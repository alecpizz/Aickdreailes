using System.Numerics;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;

namespace Engine.Entities;

public unsafe class PhysicsEntity : Entity
{
    private Model _model;
    private RigidBody _rb;
    [SerializeField] private Vector3 _modelOffset;
    public PhysicsEntity(string modelPath, Vector3 scale, Vector3 offset, string name) : base(name)
    {
        _model = Raylib.LoadModel(modelPath);
        _modelOffset = offset;
        _model.Transform = Raymath.MatrixScale(scale.X, scale.Y, scale.Z);
        var tr = Transform;
        tr.Scale = scale;
        Transform = tr;
        var bounds = Raylib.GetModelBoundingBox(_model);
        Vector3 size = Vector3.Abs(bounds.Min - bounds.Max);
        _rb = Engine.PhysicsWorld.CreateRigidBody();
        _rb.AddShape(new BoxShape(size.X, size.Y, size.Z));
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        var tr = Transform;
        tr.Translation = _rb.Position.ToVector3();
        tr.Rotation = _rb.Orientation.ToQuaternion();
        Transform = tr;
    }

    public override void OnRender()
    {
        base.OnRender();
        _rb.DebugDraw(Engine.PhysDrawer);
        var matrix = RaylibExtensions.TRS(Transform);
        matrix *= RaylibExtensions.Translate(_modelOffset);
        for (int i = 0; i < _model.MeshCount; i++)
        {
            Raylib.DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
        }
    }
}