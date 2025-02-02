using System.Numerics;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using ShaderType = Engine.Rendering.ShaderType;

namespace Engine.Entities;

public unsafe class PhysicsEntity : Entity
{
    private Model _model;
    private RigidBody _rb;
    private Transform _lastTransform;
    private Transform _currTransform;
    [SerializeField] private Vector3 _modelOffset;
    [SerializeField] private bool _drawBounds = false;
    public PhysicsEntity(string modelPath, Transform transform, Vector3 offset, string name) : base(name)
    {
        _model = Raylib.LoadModel(modelPath);
        _modelOffset = offset;
        _model.Transform = RaylibExtensions.TRS(transform);
        Transform = transform;
        _currTransform = Transform;
        var bounds = Raylib.GetModelBoundingBox(_model);
        Vector3 size = Vector3.Abs(bounds.Min - bounds.Max);
        _rb = Engine.PhysicsWorld.CreateRigidBody();
        _rb.Position = transform.Translation.ToJVector();
        _rb.Orientation = transform.Rotation.ToJQuaternion();
        _rb.AddShape(new BoxShape(size.X, size.Y, size.Z));
        _rb.Tag = this;
        Engine.ShaderManager.SetupModelMaterials(ref _model, ShaderType.Static);
    }
    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        _lastTransform = _currTransform;
        _currTransform.Translation = _rb.Position.ToVector3();
        _currTransform.Rotation = _rb.Orientation.ToQuaternion();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        var tr = Transform;
        tr.Translation = Vector3.Lerp(_lastTransform.Translation, _currTransform.Translation, Time.InterpolationTime);
        tr.Rotation = Quaternion.Slerp(_lastTransform.Rotation, _currTransform.Rotation, Time.InterpolationTime);
        Transform = tr;
    }

    public override void OnRender()
    {
        base.OnRender();
        var matrix = RaylibExtensions.TRS(Transform);
        matrix *= RaylibExtensions.Translate(_modelOffset);
        for (int i = 0; i < _model.MeshCount; i++)
        {
            Raylib.DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
        }

        if (_drawBounds)
        {
            _rb.DebugDraw(Engine.PhysDrawer);
        }
    }

}