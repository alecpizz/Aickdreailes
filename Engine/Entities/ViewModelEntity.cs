using System.Numerics;
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
    
    
    [Header("Model Settings")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(0.075f, -0.025f, -0.140f);
    [SerializeField] private Vector3 _eulerOffset = new Vector3(0f, 102.0f, 0f);
    [SerializeField] private Vector3 _modelScale = new Vector3(0.01f, 0.01f, 0.01f);
    private PlayerEntity _player;

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
    }


    public override void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        ImGUIUtils.DrawFields(this);
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
            targetRotation * Raymath.QuaternionFromEuler(_eulerOffset.Z, _eulerOffset.Y, _eulerOffset.X),
            Time.DeltaTime * _smoothing);

        float positionX = 0f;
        float positionY = 0f;
        if (IsKeyDown(KeyboardKey.A))
        {
            positionX += -1f;
        }
        if (IsKeyDown(KeyboardKey.D))
        {
            positionX += 1f;
        }
        if (IsKeyDown(KeyboardKey.W))
        {
            positionY += 1f;
        }
        if (IsKeyDown(KeyboardKey.S))
        {
            positionY += -1f;
        }

        Vector3 movement = new Vector3(positionX * -_stepMultiplier, 0f,
            positionY * _stepMultiplier);
        
        transform.Translation = Vector3.Lerp(transform.Translation, movement + _positionOffset, Time.DeltaTime * _smoothing);
        transform.Scale = _modelScale;
        Transform = transform;
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
        UnloadModel(_model);
    }
}