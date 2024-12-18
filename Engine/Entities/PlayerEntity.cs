using System.Numerics;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;

namespace Engine.Entities;

public class PlayerEntity : Entity
{
    private readonly RigidBody _rigidBody;
    private readonly float _capsuleHalfHeight;
    private bool _isGrounded = true;
    private JVector _targetVelocity = JVector.Zero;
    private PlayerConfig _playerConfig = new PlayerConfig();
    private PlayerCommand _playerCommand = new PlayerCommand();
    private float _yaw, _pitch;
    private bool _jumpQueued = false;
    private DynamicTree.RayCastFilterPre _preFilter;
    private DynamicTree.RayCastFilterPost _postFilter;
    private PlayerRayCaster _rayCaster;

    public PlayerEntity(Vector3 spawnPt)
    {
        _rigidBody = Engine.PhysicsWorld.CreateRigidBody();
        _rigidBody.Tag = this;
        var capsule = new CapsuleShape(0.5f, 1f);
        _rigidBody.AddShape(capsule);
        _rigidBody.Position = spawnPt.ToJVector();
        _rigidBody.Damping = (0, 0);
        _capsuleHalfHeight = capsule.Radius + capsule.Length * 0.5f;

        _rigidBody.DeactivationTime = TimeSpan.MaxValue;

        var upright = Engine.PhysicsWorld.CreateConstraint<HingeAngle>(_rigidBody, Engine.PhysicsWorld.NullBody);
        upright.Initialize(JVector.UnitY, AngularLimit.Full);

        _rigidBody.Friction = 0.0f;
        _rigidBody.SetMassInertia(JMatrix.Zero, 1e-3f, true);
        _rigidBody.AffectedByGravity = false;
        _preFilter = FilterShape;
        _postFilter = PostFilter;
        _rayCaster = new PlayerRayCaster();
    }

    public override void OnUpdate()
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
        _rigidBody.Orientation = JQuaternion.CreateRotationY(float.DegreesToRadians(-_yaw));
        QueueJump();
        bool hit = Engine.PhysicsWorld.DynamicTree.RayCast(_rigidBody.Position, -JVector.UnitY, _preFilter, 
            _postFilter,
            out IDynamicTreeProxy? proxy, out JVector normal, out float lambda);
        float delta = lambda - _capsuleHalfHeight;
        _isGrounded = (hit && delta < 0.04f && proxy != null);
        if (_isGrounded)
        {
            GroundMove();
        }
        else
        {
            AirMove();
        }

        _rigidBody.Velocity = _targetVelocity;
        Vector3 targetPosition = new Vector3(_rigidBody.Position.X,
            _rigidBody.Position.Y + _playerConfig.PlayerViewYOffset, _rigidBody.Position.Z);
        Engine.Camera.Position = targetPosition;
        Engine.Camera.Target = targetPosition + Vector3.Normalize(front);
        _rayCaster.Update();
    }

    public override void OnCleanup()
    {
        Engine.PhysicsWorld.Remove(_rigidBody);
    }

    public override void OnUIRender()
    {
        Raylib.DrawText($"Player Velocity {_rigidBody.Velocity.ToString()}", 10, 20, 20, Color.White);
        Raylib.DrawText($"Player Position {_rigidBody.Position.ToString()}", 10, 60, 20, Color.White);
        Raylib.DrawText($"Player is Grounded {_isGrounded.ToString()}", 10, 90, 20, Color.White);
    }

    public override void OnRender()
    {
        foreach (var hitPoint in _rayCaster._hitPoints)
        {
            Raylib.DrawSphere(hitPoint, 0.2f, Color.Red);
        }
    }

    public void Teleport(Vector3 pos)
    {
        _rigidBody.Position = pos.ToJVector();
        Engine.Camera.Position = pos + new Vector3(0f, _playerConfig.PlayerViewYOffset, 0f);
        _rigidBody.Velocity = JVector.Zero;
        _targetVelocity = JVector.Zero;
        _rigidBody.AngularVelocity = JVector.Zero;
    }

    private bool PostFilter(DynamicTree.RayCastResult result)
    {
        if (result.Entity is RigidBodyShape) //filter here for what we can jump on??
        {
            return true;
        }

        return false;
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


    private void GroundMove()
    {
        ApplyFriction(!_jumpQueued ? 1.0f : 0.0f);
        UpdateInput();
        var goalDirection = new JVector(_playerCommand.Forward, 0f, -_playerCommand.Right);
        goalDirection =
            JVector.Transform(goalDirection, _rigidBody.Orientation); //this probably needs an offset or something.
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

    private bool FilterShape(IDynamicTreeProxy shape)
    {
        if (shape is RigidBodyShape rbs)
        {
            if (rbs.RigidBody == _rigidBody) return false;
        }

        return true;
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

    private void AirMove()
    {
        float accel;
        UpdateInput();
        var goalDir = new JVector(_playerCommand.Forward, 0f, -_playerCommand.Right);
        goalDir = JVector.Transform(goalDir, _rigidBody.Orientation);

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
}