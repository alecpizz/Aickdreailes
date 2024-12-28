using System.Numerics;
using Engine.Animation;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine.Entities;

public class ViewModelEntity : Entity
{
    private Model _model;

    [Header("Sway Settings")] [SerializeField]
    private float _smoothing = 8f;

    [SerializeField] private readonly float _swayMultiplier = 1.25f;
    [SerializeField] private float _stepMultiplier = 0.0025f;


    [Header("Model Settings")] [SerializeField]
    private Vector3 _positionOffset = new Vector3(0.2f, -0.6f, -0.25f);

    [SerializeField] private Vector3 _eulerOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 _modelScale = new Vector3(1f);
    [SerializeField] private float _bobSpeed = 4.0f;
    [SerializeField] private float _bobIntensity = 0.005f;
    [SerializeField] private float _bobIntensityX = 0.250f;
    private float _time;
    private PlayerEntity _player;
    private ModelAnimator _animator;

    public ViewModelEntity(string modelPath, PlayerEntity player) : base("View Model")
    {
        _model = LoadModel(modelPath);
        unsafe
        {
            for (int i = 0; i < _model.MaterialCount; i++)
            {
                _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture.Mipmaps = 2;
                GenTextureMipmaps(ref _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture);
            }
        }

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
        float mouseX = mouse.X * _swayMultiplier;
        float mouseY = mouse.Y * _swayMultiplier;

        Quaternion xRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(-mouseY));
        Quaternion yRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(mouseX));

        Quaternion targetRotation = xRotation * yRotation;

        var transform = Transform;
        transform.Rotation = Quaternion.Slerp(transform.Rotation,
            targetRotation * Raymath.QuaternionFromEuler(float.DegreesToRadians(_eulerOffset.Z),
                float.DegreesToRadians(_eulerOffset.Y),float.DegreesToRadians( _eulerOffset.X)),
            Time.DeltaTime * _smoothing);

        Vector3 input = new Vector3(_player.RigidBody.Velocity.X, 0f, _player.RigidBody.Velocity.Z);
        if (input.LengthSquared() > 0f && _player.IsGrounded)
        {
            _time += Time.DeltaTime * _bobSpeed;
        }
        else
        {
            _time = 0f;
        }
        Vector3 bob = Vector3.Zero;
        float sinAmountY = -MathF.Abs(_bobIntensity * MathF.Sin(_time));
        Vector3 sinX = GetCameraRight(ref Engine.Camera) * _bobIntensity * MathF.Cos(_time) * _bobIntensityX;
        bob.Y = sinAmountY;
        bob += sinX;
        transform.Translation =
            Vector3.Lerp(transform.Translation, bob + _positionOffset, Time.DeltaTime * _smoothing);
        transform.Scale = _modelScale;
        Transform = transform;
        _animator.OnUpdate();
    }

    public override void OnRender()
    {
        base.OnRender();
        Rlgl.PushMatrix();
        //apply camera position & rotation
        var view = GetCameraViewMatrix(ref Engine.Camera);
        view = Raymath.MatrixInvert(view);
        Raymath.MatrixDecompose(view, out var translation, out var rotation, out var scale);
        rotation = Quaternion.Inverse(rotation);
        Rlgl.Translatef(translation.X, translation.Y, translation.Z);
        Rlgl.MultMatrixf(Raymath.QuaternionToMatrix(rotation));

        //apply local space transforms
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