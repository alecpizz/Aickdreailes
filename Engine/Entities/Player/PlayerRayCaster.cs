using System.Numerics;
using Engine.Entities;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine;

public class PlayerRayCaster
{
    private readonly DynamicTree.RayCastFilterPre _preFilter;
    private readonly DynamicTree.RayCastFilterPost _postFilter;
    public List<Vector3> _hitPoints = new();

    public PlayerRayCaster()
    {
        _preFilter = PreFilter;
        _postFilter = PostFilter;
    }

    public void Update()
    {
        if (IsMouseButtonPressed(PCControlSet.SHOOTCLICK))
        {
            var ray = GetScreenToWorldRay(new Vector2(GetScreenWidth() * 0.5f, GetScreenHeight() * 0.5f), Engine.Camera);
            if (Engine.PhysicsWorld.DynamicTree.RayCast(ray.Position.ToJVector(),
                    ray.Direction.ToJVector(), _preFilter, _postFilter,
                    out IDynamicTreeProxy? proxy, out JVector normal, out float distance))
            {
                if (proxy != null)
                {
                    _hitPoints.Add(ray.Position + ray.Direction * distance);
                }
            }
        }
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