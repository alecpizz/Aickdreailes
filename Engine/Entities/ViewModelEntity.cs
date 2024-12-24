using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine.Entities;

public class ViewModelEntity : Entity
{
    private Model _model;
    [Header("Sway")] [SerializeField] private float _step = 0.01f;
    [SerializeField] private float _maxStepDistance = 0.06f;
    private Vector3 _swayPos;

    [Header("Sway Rotation")] [SerializeField]
    private float _rotationStep = 4f;

    [SerializeField] private float _maxRotationStep = 5f;
    private Vector3 _swayEulerRot;
    [SerializeField] private float _smooth = 10f;
    private float _smoothRot = 12f;
    [Header("Bobbing")] [SerializeField] private float _speedCurve;
    private float _curveSin => MathF.Sin(_speedCurve);
    private float _curveCos => MathF.Cos(_speedCurve);
    [SerializeField] private Vector3 _travelLimit = Vector3.One * 0.025f;
    [SerializeField] private Vector3 _bobLimit = Vector3.One * 0.01f;
    private Vector3 _bobPosition;
    [SerializeField] private float _bobExaggeration;

    [Header("Bob Rotation")] [SerializeField]
    private Vector3 _multiplier;

    [Header("Model Offset")] [SerializeField]
    private Vector3 _positionOffset = new Vector3(0.030f, 0.000f, -0.100f);

    private Vector3 _eulerOffset;

    private Dictionary<FieldInfo, string> _fields = new Dictionary<FieldInfo, string>();
    private Vector3 _walkInput;
    private Vector3 _bobEulerRotation;
    private Vector3 _lookInput;
    private PlayerEntity _player;

    public ViewModelEntity(string modelPath, PlayerEntity player) : base("View Model")
    {
        _model = LoadModelFromMesh(GenMeshCube(0.25f, 0.25f, 1f));
        var transform = Transform;
        transform.Translation = new Vector3(0.25f, 0.0f, -0.5f);
        Transform = transform;
        _player = player;
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var fieldInfo in fields)
        {
            var attribute = fieldInfo.GetCustomAttributes<Attribute>();
            if (attribute != null!)
            {
                _fields.Add(fieldInfo, GetPretty(fieldInfo.Name));
            }
        }
    }

    private static string GetPretty(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;

        string result = str;
        result = result.TrimStart('_').TrimEnd('_');
        string[] words = Regex.Split(result, @"(?<=[a-z])(?=[A-Z])");
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
        }

        return string.Join(" ", words);
    }

    public override void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        //TODO: find a way for this to be used globally
        ImGUIUtils.DrawFields(this,ref  _fields);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        GetInput();
        SwayPosition();
        SwayRotation();
        BobPosition();
        BobRotation();
        var transform = Transform;
        transform.Translation = Vector3.Lerp(transform.Translation, _swayPos + _bobPosition, Time.DeltaTime * _smooth) +
                                _positionOffset;
        transform.Rotation = Quaternion.Slerp(transform.Rotation,
            Raymath.QuaternionFromEuler(_swayEulerRot.X, _swayEulerRot.Y, _swayEulerRot.Z) *
            Raymath.QuaternionFromEuler(_bobEulerRotation.X, _bobEulerRotation.Y, _bobEulerRotation.Z),
            Time.DeltaTime * _smoothRot);
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
        DrawModel(_model, Vector3.Zero, 1.0f, Color.Red);
        Rlgl.PopMatrix();
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        UnloadModel(_model);
    }

    private void GetInput()
    {
        _walkInput = Vector3.Zero;
        if (IsKeyDown(KeyboardKey.W))
        {
            _walkInput.Y += 1f;
        }

        if (IsKeyDown(KeyboardKey.S))
        {
            _walkInput.Y += -1f;
        }

        if (IsKeyDown(KeyboardKey.A))
        {
            _walkInput.X += 1f;
        }

        if (IsKeyDown(KeyboardKey.D))
        {
            _walkInput.X -= 1f;
        }

        var mouse = GetMouseDelta();
        _lookInput = Vector3.Zero;
        _lookInput.X = mouse.X;
        _lookInput.Y = mouse.Y;
    }

    private void SwayPosition()
    {
        Vector3 invertLook = _lookInput * -_step;
        invertLook.X = Raymath.Clamp(invertLook.X, -_maxStepDistance, _maxStepDistance);
        invertLook.Y = Raymath.Clamp(invertLook.Y, -_maxStepDistance, _maxStepDistance);

        _swayPos = invertLook;
    }

    private void SwayRotation()
    {
        var invertLook = _lookInput * -_rotationStep;
        invertLook.X = Raymath.Clamp(invertLook.X, -_maxRotationStep, _maxRotationStep);
        invertLook.Y = Raymath.Clamp(invertLook.Y, -_maxRotationStep, _maxRotationStep);
        _swayEulerRot = new Vector3(invertLook.Y, invertLook.X, invertLook.X);
    }

    private void BobPosition()
    {
        _speedCurve += Time.DeltaTime * (_player.IsGrounded ? (_walkInput.X + _walkInput.Y) * _bobExaggeration : 1f) +
                       0.01f;
        _bobPosition.X = (_curveCos * _bobLimit.X * (_player.IsGrounded ? 1 : 0)) - (_walkInput.X * _travelLimit.X);

        _bobPosition.Y = (_curveSin * _bobLimit.Y) - (_walkInput.Y * _travelLimit.Y);

        _bobPosition.Z = -(_walkInput.Y * _travelLimit.Z);
    }

    private void BobRotation()
    {
        _bobEulerRotation.X = (_walkInput != Vector3.Zero
            ? _multiplier.X * (MathF.Sin(2 * _speedCurve))
            : _multiplier.X * (MathF.Sin(2 * _speedCurve) / 2));

        _bobEulerRotation.Y = (_walkInput != Vector3.Zero ? _multiplier.Y * _curveCos : 0);

        _bobEulerRotation.Z = (_walkInput != Vector3.Zero ? _multiplier.Z * _curveCos * _walkInput.X : 0);
    }
}