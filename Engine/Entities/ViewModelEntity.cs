using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Engine.Animation;
using ShaderType = Engine.Rendering.ShaderType;

namespace Engine.Entities;

public class ViewModelEntity : Entity
{
    private Model _model;

    [Header("Sway Settings")] [SerializeField]
    private float _smoothing = 8f;

    [SerializeField] private readonly float _swayMultiplier = 20f;
    [SerializeField] private float _stepMultiplier = 10f;


    [Header("Model Settings")] [SerializeField]
    private Vector3 _positionOffset = new Vector3(0.145f, -0.320f, 0.070f);

    [SerializeField] private Vector3 _eulerOffset = new Vector3(0.155f, 185.095f, -2.62f);
    [SerializeField] private Vector3 _modelScale = new Vector3(1f);
    [SerializeField] private float _amplitude = 0.01f;
    [SerializeField] private float _frequency = 10.0f;
    [SerializeField] private float _verticalVelocityMultiplier = 0.01f;
    [SerializeField] private float _maxVerticalOffset = 0.05f;
    private float _toggleSpeed = 3.0f;
    private Vector3 _startPos = Vector3.Zero;
    private float _time;
    private PlayerEntity _player;
    private ModelAnimator _animator;

    public ViewModelEntity(string modelPath, PlayerEntity player) : base("View Model")
    {
        _model = LoadModel(modelPath);
        for (int i = 0; i < _model.MeshCount; i++)
        {
            unsafe
            {
                if (_model.Meshes[i].Tangents == null)
                {
                    var prevColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"WARNING: NO TANGENTS ON MESH {modelPath}");
                    _model.Meshes[i].AllocTangents();
                    GenMeshTangents(ref _model.Meshes[i]);
                    Console.ForegroundColor = prevColor;
                }
            }
        }
        Engine.ShaderManager.SetupModelMaterials(ref _model, ShaderType.Skinned);

        _player = player;
        _animator = new ModelAnimator(_model, modelPath);
    }


    public override void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        _animator.OnImGui();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        var mouse = GetMouseDelta();
        float mouseX = mouse.X * _swayMultiplier * Time.DeltaTime;
        float mouseY = mouse.Y * _swayMultiplier * Time.DeltaTime;

        Quaternion xRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(-mouseY));
        Quaternion yRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(-mouseX));

        Quaternion targetRotation = xRotation * yRotation;

        var transform = Transform;
        transform.Rotation = Quaternion.Slerp(transform.Rotation,
            targetRotation * Raymath.QuaternionFromEuler(float.DegreesToRadians(_eulerOffset.Z),
                float.DegreesToRadians(_eulerOffset.Y),float.DegreesToRadians( _eulerOffset.X)),
            Time.DeltaTime * _smoothing);

        Vector3 input = new Vector3(_player.RigidBody.Velocity.X, 0f, _player.RigidBody.Velocity.Z);
        if (input.LengthSquared() > 0f )
        {
            _time += Time.DeltaTime;
        }
        else
        {
            _time = 0f;
        }
        Vector3 bob = Vector3.Zero;
        bob.Y += MathF.Sin(_time * _frequency) * _amplitude;
        bob.X += MathF.Cos(_time * _frequency / 2) * _amplitude * 2;
        bob.Y += Math.Clamp(-_player.RigidBody.Velocity.Y * _verticalVelocityMultiplier, -_maxVerticalOffset, _maxVerticalOffset);
        transform.Translation =
            Vector3.Lerp(transform.Translation, bob + _positionOffset, Time.DeltaTime * _smoothing);
        transform.Scale = _modelScale;
        Transform = transform;
        //TODO PUT THIS SOMEWHERE ELSE
        if (IsMouseButtonPressed(PCControlSet.SHOOTCLICK))
        {
            _animator.SetAnimationIndex(4);
        }

        if (IsKeyPressed(PCControlSet.RELOADKEY))
        {
            _animator.SetAnimationIndex(3);
        }
        _animator.OnUpdate();
    }

    public override void OnRender()
    {
        base.OnRender();
        Rlgl.PushMatrix();
        //apply camera position & rotation
        var view = Matrix4x4.CreateLookAt(Engine.Camera.Position, Engine.Camera.Target, Engine.Camera.Up);
        Matrix4x4.Invert(view, out view);
        Matrix4x4.Decompose(view, out var scale, out var rotation, out var translation);
        
        Rlgl.Translatef(translation.X, translation.Y, translation.Z);
        Rlgl.MultMatrixf(Raymath.QuaternionToMatrix(rotation));

        // //apply local space transforms
        Rlgl.Translatef(Transform.Translation.X, Transform.Translation.Y, Transform.Translation.Z);
        Rlgl.MultMatrixf(Raymath.QuaternionToMatrix(Transform.Rotation));
        Rlgl.Scalef(Transform.Scale.X, Transform.Scale.Y, Transform.Scale.Z);
        DrawModel(_model, Vector3.Zero, 1.0f, Color.White);
        Rlgl.PopMatrix();
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        _animator.Dispose();
        UnloadModel(_model);
    }
}