using System.Numerics;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine;

public class PlayerRayCaster
{
    private readonly DynamicTree.RayCastFilterPre _preFilter;
    private readonly DynamicTree.RayCastFilterPost _postFilter;
    private readonly World _world;
    public List<(Vector3, Vector3)> _hitPoints = new();

    public PlayerRayCaster(World world)
    {
        _preFilter = PreFilter;
        _postFilter = PostFilter;
        _world = world;
    }

    public void Update(Camera3D camera)
    {
        if (IsMouseButtonPressed(MouseButton.Left))
        {
            var ray = GetScreenToWorldRay(new Vector2(GetScreenWidth() * 0.5f, GetScreenHeight() * 0.5f), camera);
            if (_world.DynamicTree.RayCast(ray.Position.ToJVector(),
                    ray.Direction.ToJVector(), _preFilter, _postFilter,
                    out IDynamicTreeProxy? proxy, out JVector normal, out float distance))
            {
                if (proxy != null)
                {
                    _hitPoints.Add((ray.Position + ray.Direction * distance, normal.ToVector3()));
                }
            }
        }
    }

    private bool PreFilter(IDynamicTreeProxy shape)
    {
        if (shape is RigidBodyShape rbs)
        {
            if (rbs.RigidBody.Tag is Tags.Player)
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