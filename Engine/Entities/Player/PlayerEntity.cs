using System.Numerics;
using ImGuiNET;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Raylib_cs;

namespace Engine.Entities;

public class PlayerEntity : Entity
{
    private readonly RigidBody _rigidBody;
    private readonly float _capsuleHalfHeight;
    private bool _isGrounded = true;
    private float _groundDistance = 0f;
    private JVector _groundNormal = JVector.Zero;
    private JVector _targetVelocity = JVector.Zero;
    private PlayerConfig _playerConfig = new PlayerConfig();
    private PlayerCommand _playerCommand = new PlayerCommand();
    private bool _jumpQueued = false;
    private DynamicTree.RayCastFilterPre _preFilter;
    private DynamicTree.RayCastFilterPost _postFilter;
    private PlayerRayCaster _rayCaster;
    public bool IsGrounded => _isGrounded;
    public RigidBody RigidBody => _rigidBody;
    private Vector2 _rotation = Vector2.Zero;
    [SerializeField] private float _cameraTiltAmount = 2.5f;
    [SerializeField] private float _cameraTiltSpeed = 8.5f;
    [SerializeField] private float _fovBoostEaseTime = 35f;
    [SerializeField] private float _fovBoostAmount = 15f;
    [SerializeField] private float _baseFov = 60f;
    private float _currentTiltAmount = 0f;
    private float _currentFovTarget = 0f;
    private Transform _lastTransform, _currTransform;
    private Sound _shootSound;
    private Sound[] _footStepSounds;
    private float _lastStepTime;

    public PlayerEntity(Vector3 spawnPt) : base("Player")
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
        Engine.Camera.FovY = _baseFov;
        var tr = Transform;
        tr.Translation = spawnPt;
        Transform = tr;
        _lastTransform = tr;
        _currTransform = tr;
        _shootSound = Raylib.LoadSound(Path.Combine("Resources", "Sounds", "Sound Effects", "gun.mp3"));
        Raylib.SetSoundVolume(_shootSound, 0.25f);
        _footStepSounds = new Sound[4];
        for (int i = 0; i < 4; i++)
        {
            _footStepSounds[i] =
                Raylib.LoadSound(Path.Combine("Resources", "Sounds", "Sound Effects", $"concrete{i + 1}.wav"));
        }

        _lastStepTime = (float)Time.TimeSinceStartup;
    }

    public override void OnUpdate()
    {
        if (Engine.InEditor)
        {
            _rigidBody.Velocity = JVector.Zero;
            _rigidBody.AngularVelocity = JVector.Zero;
            return;
        }

        var motion = Raylib.GetMouseDelta();
        motion.X = Math.Clamp(motion.X, -25f, 25f);
        motion.Y = Math.Clamp(motion.Y, -25f, 25f);
        _rotation.X += motion.X * _playerConfig.XMouseSensitivity * Time.DeltaTime;
        _rotation.Y += motion.Y * _playerConfig.YMouseSensitivity * Time.DeltaTime;
        _rotation.Y = Raymath.Clamp(_rotation.Y, -89.0f, 89.0f);
        var xQuat = Raymath.QuaternionFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(-_rotation.X));
        var yQuat = Raymath.QuaternionFromAxisAngle(-Vector3.UnitX, float.DegreesToRadians(_rotation.Y));
        _rigidBody.Orientation = xQuat.ToJQuaternion();
        QueueJump();
        bool hit = Engine.PhysicsWorld.DynamicTree.RayCast(_rigidBody.Position, -JVector.UnitY, _preFilter,
            _postFilter,
            out IDynamicTreeProxy? proxy, out _groundNormal, out _groundDistance);
        float delta = _groundDistance - _capsuleHalfHeight;
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
        Vector3 targetPosition =
            Vector3.Lerp(_lastTransform.Translation, _currTransform.Translation, Time.InterpolationTime) +
            new Vector3(0f, _playerConfig.PlayerViewYOffset, 0f);
        Quaternion targetRotation = xQuat * yQuat;

        var horizontal = _playerCommand.Right;
        float tilt = Raymath.Lerp(_currentTiltAmount, horizontal * _cameraTiltAmount,
            Time.DeltaTime * _cameraTiltSpeed);
        _currentTiltAmount = tilt;

        var fwd = Raymath.Vector3RotateByQuaternion(-Vector3.UnitZ, targetRotation);
        var right = Vector3.Cross(Vector3.UnitY, fwd);
        right = Vector3.Normalize(right);
        var up = Vector3.Cross(fwd, right);

        var tiltQuat = Raymath.QuaternionFromAxisAngle(fwd, float.DegreesToRadians(-tilt));
        up = Raymath.Vector3RotateByQuaternion(up, tiltQuat);
        float fovTarget =
            Math.Clamp(Vector3.Dot(Vector3.Normalize(fwd), Vector3.Normalize(_targetVelocity.ToVector3().XZPlane())),
                0f, 1f);
        if (float.IsNaN(fovTarget))
        {
            fovTarget = 0.0f;
        }

        fovTarget = Raymath.Lerp(_currentFovTarget, fovTarget, Easing.InQuart(Time.DeltaTime * _fovBoostEaseTime));
        _currentFovTarget = fovTarget;
        fovTarget = _baseFov + fovTarget * _fovBoostAmount;
        fovTarget = Math.Clamp(fovTarget, _baseFov, _baseFov + _fovBoostAmount);
        Engine.Camera.FovY = fovTarget;
        Engine.Camera.Up = up;
        Engine.Camera.Position = targetPosition;
        Engine.Camera.Target = targetPosition + fwd;
        _rayCaster.Update();
        if (Raylib.IsMouseButtonPressed(PCControlSet.SHOOTCLICK))
        {
            Raylib.PlaySound(_shootSound);
            _rayCaster.OnClick();
        }
        HandleFootStepSounds();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        _lastTransform = _currTransform;
        _currTransform.Translation = _rigidBody.Position.ToVector3();
        _currTransform.Rotation = _rigidBody.Orientation.ToQuaternion();
        _rayCaster.OnFixedUpdate();
    }

    public override void OnCleanup()
    {
        Engine.PhysicsWorld.Remove(_rigidBody);
        Raylib.UnloadSound(_shootSound);
        for (int i = 0; i < 4; i++)
        {
            Raylib.UnloadSound(_footStepSounds[i]);
        }
    }

    public override void OnUIRender()
    {
        Raylib.DrawText($"Player Velocity {_rigidBody.Velocity.ToString()}", 10, 20, 20, Color.White);
        Raylib.DrawText($"Player Position {_rigidBody.Position.ToString()}", 10, 60, 20, Color.White);
        Raylib.DrawText($"Player is Grounded {_isGrounded.ToString()}", 10, 90, 20, Color.White);
        Raylib.DrawText($"Current Hit ID: {_rayCaster.CurrentHitBody?.RigidBodyId}", 10, 120, 20, Color.White);
        Raylib.DrawText($"Current FOV: {Engine.Camera.FovY}", 10, 150, 20, Color.White);
        Raylib.DrawCircle(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2, 5f, Color.Black);
    }

    public override void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        _playerConfig.HandleImGui();
    }

    public override void OnRender()
    {
        foreach (var hitPoint in _rayCaster._hitPoints)
        {
            Raylib.DrawSphereWires(hitPoint, 0.05f, 10, 10, Color.Red);
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
            _jumpQueued = Raylib.IsKeyDown(PCControlSet.JUMPKEY)
                          || Raylib.IsGamepadButtonDown(0, GamepadControlSet.JUMPBUTTON);
            return;
        }

        if ((Raylib.IsKeyDown(PCControlSet.JUMPKEY)
             || Raylib.IsGamepadButtonDown(0, GamepadControlSet.JUMPBUTTON)) && !_jumpQueued)
        {
            _jumpQueued = true;
        }

        if (Raylib.IsKeyUp(PCControlSet.JUMPKEY) || Raylib.IsGamepadButtonUp(0, GamepadControlSet.JUMPBUTTON))
        {
            _jumpQueued = false;
        }
    }


    private void GroundMove()
    {
        ApplyFriction(!_jumpQueued ? 1.0f : 0.0f);
        UpdateInput();
        var goalDirection = new JVector(-_playerCommand.Right, 0f, -_playerCommand.Forward);
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
        else
        {
            if (_groundNormal.Y > 0)
            {
                _targetVelocity.Y -= _groundDistance;
            }
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
        Vector2 move = InputExtensions.PlayerMovementInput();

        _playerCommand.Forward = move.Y;
        _playerCommand.Right = move.X;
    }

    private void AirMove()
    {
        float accel;
        UpdateInput();
        var goalDir = new JVector(-_playerCommand.Right, 0f, -_playerCommand.Forward);
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

    [SerializeField] private float _maxStepVelocity = 7.5f;
    [SerializeField] private float _minStepVelocity = 1.5f;
    [SerializeField] private float _minStepInterval = 0.5f;
    [SerializeField] private float _maxStepInterval = 0.75f;
    private void HandleFootStepSounds()
    {
        if (!_isGrounded)
        {
            return;
        }
        var speed = _targetVelocity.ToVector3().XZPlane().Length();
        if (speed < _minStepVelocity)
        {
            return;
        }
        var nextStepTime = MathFX.Remap(0f, _maxStepVelocity, _maxStepInterval, _minStepInterval, speed);
        if (Time.TimeSinceStartup > _lastStepTime)
        {
            _lastStepTime = (float)(Time.TimeSinceStartup + nextStepTime);
            //play the sound
            var rand = Raylib.GetRandomValue(0, 3);
            Raylib.SetSoundPitch(_footStepSounds[rand], Random.Shared.NextSingle() * (0.9f - 0.8f) + 0.8f);
            Raylib.PlaySound(_footStepSounds[rand]);
        }
        
    }
}