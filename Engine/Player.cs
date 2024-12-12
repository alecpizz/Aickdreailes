using System.Numerics;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;

namespace Engine;

public class Player
{
    public RigidBody Body { get; }
    private readonly float _capsuleHalfHeight;
    private readonly World _world;
    private bool _isGrounded = true;
    private JVector _targetVelocity = JVector.Zero;
    private float _playerTopSpeed = 0.0f;
    private PlayerConfig _playerConfig = new PlayerConfig();
    private PlayerCommand _playerCommand = new PlayerCommand();
    private float _yaw, _pitch;
    private bool _jumpQueued = false;
    private DynamicTree.RayCastFilterPre _preFilter;
    private DynamicTree.RayCastFilterPost _postFilter;
    public Player(World world, JVector pos)
    {
        Body = world.CreateRigidBody();
        var capsule = new CapsuleShape(0.5f, 1f);
        Body.AddShape(capsule);
        Body.Position = pos;
        Body.Damping = (0, 0);
        _capsuleHalfHeight = capsule.Radius + capsule.Length * 0.5f;
        _world = world;
        Body.DeactivationTime = TimeSpan.MaxValue;

        var head = new TransformedShape(new BoxShape(0.25f, 0.1f, 0.25f), new JVector(0.0f, 0.7f, 0.5f));
        Body.AddShape(head, false);

        var upright = world.CreateConstraint<HingeAngle>(Body, world.NullBody);
        upright.Initialize(JVector.UnitY, AngularLimit.Full);

        // _angularMotor = world.CreateConstraint<AngularMotor>(Body, world.NullBody);
        // _angularMotor.Initialize(JVector.UnitY, JVector.UnitY);
        // _angularMotor.MaximumForce = 1000f;
        // _angularMotor.TargetVelocity = 0.0f;
        Body.Friction = 0.0f;
        Body.SetMassInertia(JMatrix.Zero, 1e-3f, true);
        Body.AffectedByGravity = false;
        _preFilter = FilterShape;
        _postFilter = PostFilter;
    }

    

    public void Update(ref Camera3D cam)
    {
        var motion = Raylib.GetMouseDelta();
        float x = motion.X;
        float y = -motion.Y;
        x *= _playerConfig.XMouseSensitivity * Time.DeltaTime;
        y *= _playerConfig.YMouseSensitivity * Time.DeltaTime;
        _yaw += x;
        _pitch += y;
        if (_pitch < -89.0f)
        {
            _pitch = -89.0f;
        }
        if (_pitch > 89.0f)
        {
            _pitch = 89.0f;
        }

        Vector3 front;
        front.X = MathF.Cos(float.DegreesToRadians(_yaw)) * MathF.Cos(float.DegreesToRadians(_pitch));
        front.Y = MathF.Sin(float.DegreesToRadians(_pitch));
        front.Z = MathF.Sin(float.DegreesToRadians(_yaw)) * MathF.Cos(float.DegreesToRadians(_pitch));
        Body.Orientation = JQuaternion.CreateRotationY(float.DegreesToRadians(-_yaw));
        QueueJump();
        bool hit = _world.DynamicTree.RayCast(Body.Position, -JVector.UnitY, _preFilter, _postFilter,
            out IDynamicTreeProxy? proxy, out JVector normal, out float lambda);
        float delta = lambda - _capsuleHalfHeight;
        _isGrounded =  (hit && delta < 0.04f && proxy != null);
        if (_isGrounded)
        {
            GroundMove();    
        }
        else
        {
            AirMove();
        }

        Body.Velocity = _targetVelocity ;
        Vector3 targetPosition = new Vector3(Body.Position.X, Body.Position.Y + _playerConfig.PlayerViewYOffset, Body.Position.Z);
        cam.Position = targetPosition;
        cam.Target = targetPosition + Vector3.Normalize(front);
    }

    private void QueueJump()
    {
        if (_playerConfig.AutoBhop)
        {
            _jumpQueued = Raylib.IsKeyDown(KeyboardKey.Space);
            return;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Space) && !_jumpQueued)
        {
            _jumpQueued = true;
        }

        if (Raylib.IsKeyUp(KeyboardKey.Space))
        {
            _jumpQueued = false;
        }
    }

    private void UpdateInput()
    {
        float forward = 0.0f;
        float right = 0.0f;
        if (Raylib.IsKeyDown(KeyboardKey.W))
        {
            forward += 1.0f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.S))
        {
            forward += -1.0f;
        }
        
        if (Raylib.IsKeyDown(KeyboardKey.A))
        {
            right += 1.0f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.D))
        {
            right += -1.0f;
        }

        _playerCommand.Forward = forward;
        _playerCommand.Right = right;
    }
    
    private void GroundMove()
    {
        ApplyFriction(!_jumpQueued ? 1.0f : 0.0f);
        UpdateInput();
        var goalDirection = new JVector(_playerCommand.Forward, 0f, -_playerCommand.Right);
        goalDirection = JVector.Transform(goalDirection, Body.Orientation); //this probably needs an offset or something.
        if (goalDirection.Length() != 0.0f)
        {
            goalDirection.Normalize();
        }
        
        var goalSpeed = goalDirection.Length() * _playerConfig.MoveSpeed;
        Acceleration(goalDirection, goalSpeed, _playerConfig.RunAcceleration);
        _targetVelocity.Y = -_playerConfig.Gravity * Time.DeltaTime;
        if (_jumpQueued)
        {
            _targetVelocity.Y = _playerConfig.JumpSpeed;
            _jumpQueued = false;
        }
    }

    private void ApplyFriction(float t)
    {
        JVector v = _targetVelocity;
        v.Y = 0.0f;
        float speed = v.Length();
        float drop = 0.0f;
        if (_isGrounded)
        {
            float control = speed < _playerConfig.RunDeceleration ? _playerConfig.RunDeceleration : speed;
            drop = control * _playerConfig.Friction * Time.DeltaTime * t;
        }

        float newSpeed = speed - drop;
        if (newSpeed < 0f)
        {
            newSpeed = 0f;
        }

        if (newSpeed > 0.0f)
        {
            newSpeed /= speed;
        }

        _targetVelocity.X *= newSpeed;
        _targetVelocity.Z *= newSpeed;
    }
    
    private void Acceleration(JVector goalDir, float goalSpeed, float accel)
    {
        float currentSpeed = JVector.Dot(_targetVelocity, goalDir);
        float addSpeed = goalSpeed - currentSpeed;
        if (addSpeed <= 0)
        {
            return;
        }

        float accelspeed = accel * Time.DeltaTime * goalSpeed;
        if (accelspeed > addSpeed)
        {
            accelspeed = addSpeed;
        }

        _targetVelocity.X += accelspeed * goalDir.X;
        _targetVelocity.Z += accelspeed * goalDir.Z;
    }

    private void AirMove()
    {
        float accel;
        UpdateInput();
        var goalDir = new JVector(_playerCommand.Forward, 0f, -_playerCommand.Right);
        goalDir = JVector.Transform(goalDir, Body.Orientation);

        float wishspeed = goalDir.Length();
        wishspeed *= _playerConfig.MoveSpeed;
        if (goalDir.Length() != 0.0f)
        {
            goalDir.Normalize();
        }

        // CPM Air control.
        float wishspeed2 = wishspeed;
        if (JVector.Dot(_targetVelocity, goalDir) < 0)
        {
            accel = _playerConfig.AirDecceleration;
        }
        else
        {
            accel = _playerConfig.AirAcceleration;
        }

        // If the player is ONLY strafing left or right
        if (_playerCommand.Forward == 0 && _playerCommand.Right != 0)
        {
            if (wishspeed > _playerConfig.SideStrafeSpeed)
            {
                wishspeed = _playerConfig.SideStrafeSpeed;
            }

            accel = _playerConfig.SideStrafeAcceleration;
        }

        Acceleration(goalDir, wishspeed, accel);
        if (_playerConfig.AirControl > 0)
        {
            AirControl(goalDir, wishspeed2);
        }

        // Apply gravity
        _targetVelocity.Y -= _playerConfig.Gravity * Time.DeltaTime;
    }

    private void AirControl(JVector targetDir, float targetSpeed)
    {
        // Only control air movement when moving forward or backward.
        if (MathF.Abs(_playerCommand.Right) < 0.001 || MathF.Abs(targetSpeed) < 0.001)
        {
            return;
        }

        float zSpeed = _targetVelocity.Y;
        _targetVelocity.Y = 0;
        /* Next two lines are equivalent to idTech's VectorNormalize() */
        float speed = _targetVelocity.Length();
        if (speed != 0f)
        {
             _targetVelocity.Normalize();
        }

        float dot = JVector.Dot(_targetVelocity, targetDir);
        float k = 32;
        k *= _playerConfig.AirControl * dot * dot * Time.DeltaTime;

        // Change direction while slowing down.
        if (dot > 0)
        {
            _targetVelocity.X *= speed + targetDir.X * k;
            _targetVelocity.Y *= speed + targetDir.Y * k;
            _targetVelocity.Z *= speed + targetDir.Z * k;

            _targetVelocity.Normalize();
        }

        _targetVelocity.X *= speed;
        _targetVelocity.Y = zSpeed; // Note this line
        _targetVelocity.Z *= speed;
    }

    private bool FilterShape(IDynamicTreeProxy shape)
    {
        if (shape is RigidBodyShape rbs)
        {
            if (rbs.RigidBody == Body) return false;
        }

        return true;
    }

    private bool PostFilter(DynamicTree.RayCastResult result)
    {
        if (result.Entity is RigidBodyShape rbs) //filter here for what we can jump on??
        {
            return true;
        }

        return false;
    }

}