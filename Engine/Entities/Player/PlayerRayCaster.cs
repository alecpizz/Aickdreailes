using System.Numerics;
using Engine.Entities;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine;

public class PlayerRayCaster
{
    private readonly DynamicTree.RayCastFilterPre _preFilter;
    private readonly DynamicTree.RayCastFilterPost _postFilter;
    public List<Vector3> _hitPoints = new();
    public RigidBody? CurrentHitBody { get; private set; }
    private RigidBody? _currentHeldBody = null;
    private JVector _hitNormal, _hitOrigin, _hitDirection;
    private float _hitDistance;
    private float _holdDistance;
    private DistanceLimit? _holdConstraint = null;

    public PlayerRayCaster()
    {
        _preFilter = PreFilter;
        _postFilter = PostFilter;
    }

    public void Update()
    {
        var ray = GetScreenToWorldRay(new Vector2(GetScreenWidth() * 0.5f, GetScreenHeight() * 0.5f), Engine.Camera);
        _hitOrigin = ray.Position.ToJVector();
        _hitDirection = ray.Direction.ToJVector();
        if (Engine.PhysicsWorld.DynamicTree.RayCast(_hitOrigin,
                _hitDirection, _preFilter, _postFilter,
                out IDynamicTreeProxy? proxy, out _hitNormal, out _hitDistance))
        {
            if (proxy is RigidBodyShape shape)
            {
                CurrentHitBody = shape.RigidBody;
            }
        }
        else
        {
            CurrentHitBody = null!;
        }

       

        if (IsKeyPressed(PCControlSet.PICKUPKEY))
        {
            //pick up time
            Console.WriteLine("Grabbing!");
            if (_currentHeldBody == null)
            {
                PickupObject();
            }
            else
            {
                ReleaseObject();
            }
        }
    }

    public void OnFixedUpdate()
    {
        if (_currentHeldBody != null)
        {
            _holdConstraint.Anchor2 = _hitOrigin + _holdDistance * _hitDirection;
            _currentHeldBody.Velocity *= 0.75f;
            _currentHeldBody.AngularVelocity *= 0.75f;
        }
    }

    private void PickupObject()
    {
        if (CurrentHitBody == null || CurrentHitBody.IsStatic) return;
        _currentHeldBody = CurrentHitBody;
        _holdDistance = _hitDistance;
        var anchor = _hitOrigin + _holdDistance * _hitDirection;
        _holdConstraint =
            Engine.PhysicsWorld.CreateConstraint<DistanceLimit>(_currentHeldBody, Engine.PhysicsWorld.NullBody);
        _holdConstraint.Initialize(anchor, anchor);
        _holdConstraint.Softness = 0.01f;
        _holdConstraint.Bias = 0.1f;
    }

    private void ReleaseObject()
    {
        if (_currentHeldBody == null) return;
        if (_holdConstraint == null) return;
        Engine.PhysicsWorld.Remove(_holdConstraint);
        _holdConstraint = null;
        _currentHeldBody = null;
    }

    private bool PreFilter(IDynamicTreeProxy shape)
    {
        if (shape is RigidBodyShape rbs)
        {
            if (rbs.RigidBody.Tag is PlayerEntity)
            {
                return false;
            }
        }

        return true;
    }

    private bool PostFilter(DynamicTree.RayCastResult result)
    {
        if (result.Entity is RigidBodyShape) //filter here for what we can jump on??
        {
            return true;
        }

        return false;
    }
}