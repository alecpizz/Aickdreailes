using System.Collections;
using System.Numerics;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine.Entities;

public unsafe class RagdollEntity : Entity
{
    private Model _model;
    private Shader _skinningShader;
    private static readonly string FragPath = Path.Combine("Resources", "Shaders", "skinning.frag");
    private static readonly string VertPath = Path.Combine("Resources", "Shaders", "skinning.vert");
    private ModelAnimation _modelAnimation;
    private Gizmo.TransformData[] _transformDatas;
    private Dictionary<Limb, Gizmo.TransformData> _colliderTransformDatas = new();
    private Dictionary<string, BoneInfo> _cleanBoneNames = new();
    private Dictionary<Limb, RigidBody> _limbRbMap = new();

    public enum Limb
    {
        Hips = 0,
        Spine,
        Spine1,
        Spine2,
        Neck,
        LeftShoulder,
        LeftForeArm,
        LeftHand,
        RightShoulder,
        RightForeArm,
        RightHand,
        LeftUpLeg,
        LeftLeg,
        LeftFoot,
        RightUpLeg,
        RightLeg,
        RightFoot,
        None
    }

    private Dictionary<string, Limb> _stringToLimbGroup = new()
    {
        { "Hips", Limb.Hips },

        { "Spine", Limb.Spine },

        { "Spine1", Limb.Spine1 },

        { "Spine2", Limb.Spine2 },

        { "Neck", Limb.Neck },
        { "Head", Limb.Neck },
        { "HeadTop_End", Limb.Neck },

        { "LeftShoulder", Limb.LeftShoulder },
        { "LeftArm", Limb.LeftShoulder },

        { "LeftForeArm", Limb.LeftForeArm },

        { "LeftHand", Limb.LeftHand },

        { "LeftHandThumb1", Limb.LeftHand },
        { "LeftHandThumb2", Limb.LeftHand },
        { "LeftHandThumb3", Limb.LeftHand },
        { "LeftHandThumb4", Limb.LeftHand },
        { "LeftHandIndex1", Limb.LeftHand },
        { "LeftHandIndex2", Limb.LeftHand },
        { "LeftHandIndex3", Limb.LeftHand },
        { "LeftHandIndex4", Limb.LeftHand },
        { "LeftHandMiddle1", Limb.LeftHand },
        { "LeftHandMiddle2", Limb.LeftHand },
        { "LeftHandMiddle3", Limb.LeftHand },
        { "LeftHandMiddle4", Limb.LeftHand },
        { "LeftHandRing1", Limb.LeftHand },
        { "LeftHandRing2", Limb.LeftHand },
        { "LeftHandRing3", Limb.LeftHand },
        { "LeftHandRing4", Limb.LeftHand },
        { "LeftHandPinky1", Limb.LeftHand },
        { "LeftHandPinky2", Limb.LeftHand },
        { "LeftHandPinky3", Limb.LeftHand },
        { "LeftHandPinky4", Limb.LeftHand },

        { "RightShoulder", Limb.RightShoulder },
        { "RightArm", Limb.RightShoulder },

        { "RightForeArm", Limb.RightForeArm },

        { "RightHand", Limb.RightHand },
        { "RightHandThumb1", Limb.RightHand },
        { "RightHandThumb2", Limb.RightHand },
        { "RightHandThumb3", Limb.RightHand },
        { "RightHandThumb4", Limb.RightHand },
        { "RightHandIndex1", Limb.RightHand },
        { "RightHandIndex2", Limb.RightHand },
        { "RightHandIndex3", Limb.RightHand },
        { "RightHandIndex4", Limb.RightHand },
        { "RightHandMiddle1", Limb.RightHand },
        { "RightHandMiddle2", Limb.RightHand },
        { "RightHandMiddle3", Limb.RightHand },
        { "RightHandMiddle4", Limb.RightHand },
        { "RightHandRing1", Limb.RightHand },
        { "RightHandRing2", Limb.RightHand },
        { "RightHandRing3", Limb.RightHand },
        { "RightHandRing4", Limb.RightHand },
        { "RightHandPinky1", Limb.RightHand },
        { "RightHandPinky2", Limb.RightHand },
        { "RightHandPinky3", Limb.RightHand },
        { "RightHandPinky4", Limb.RightHand },

        { "LeftUpLeg", Limb.LeftUpLeg },

        { "LeftLeg", Limb.LeftLeg },

        { "LeftFoot", Limb.LeftFoot },
        { "LeftToeBase", Limb.LeftFoot },
        { "LeftToe_End", Limb.LeftFoot },

        { "RightUpLeg", Limb.RightUpLeg },

        { "RightLeg", Limb.RightLeg },

        { "RightFoot", Limb.RightFoot },
        { "RightToeBase", Limb.RightFoot },
        { "RightToe_End", Limb.RightFoot }
    };

    private Dictionary<Limb, List<int>> _limbMapping = new();


    public RagdollEntity(string modelPath) : base(modelPath)
    {
        _model = Raylib.LoadModel(modelPath);
        var tr = Transform;
        tr.Translation = new Vector3(0f, -1f, 0f);
        tr.Scale = Vector3.One * 0.01f;
        tr.Rotation = Raymath.QuaternionFromEuler(float.DegreesToRadians(90f), 0f, 0f);
        Transform = tr;

        _skinningShader = LoadShader(VertPath, FragPath);
        for (int i = 0; i < _model.MaterialCount; i++)
        {
            _model.Materials[i].Shader = _skinningShader;
        }

        _transformDatas = new Gizmo.TransformData[_model.BoneCount];
        int count = 0;
        ModelAnimation* modelAnimations = LoadModelAnimations(modelPath, ref count);
        _modelAnimation = modelAnimations[0];
        var entityTransform = RaylibExtensions.TRS(Transform);
        for (int boneId = 0; boneId < _model.BoneCount; boneId++)
        {
            var frame = _modelAnimation.FramePoses[0];
            Vector3 bonePosition = Raymath.Vector3Transform(frame[boneId].Translation, entityTransform);
            Quaternion boneRotation = frame[boneId].Rotation;
            Vector3 boneScale = Raymath.Vector3Transform(frame[boneId].Scale, entityTransform);

            _transformDatas[boneId] = new Gizmo.TransformData(bonePosition, boneRotation, boneScale);
            var name = GetCleanBoneName(boneId);
            _cleanBoneNames.Add(name, _model.Bones[boneId]);
            _limbMapping.TryAdd(_stringToLimbGroup[name], new());
            _limbMapping[_stringToLimbGroup[name]].Add(boneId);
        }

        for (Limb i = Limb.Hips; i < Limb.None; i++)
        {
            //make da ragdoll yeah!
            var rb = Engine.PhysicsWorld.CreateRigidBody();
            rb.AddShape(new TransformedShape(new BoxShape(1f), JVector.Zero));
            rb.Position = _transformDatas[_limbMapping[i][0]].Translation.ToJVector();
            rb.IsStatic = true;
            _limbRbMap.Add(i, rb);
            _colliderTransformDatas.Add(i, new Gizmo.TransformData(_transformDatas[_limbMapping[i][0]])
            {
                Scale = Vector3.One * 0.1f
            });
            var shape = rb.Shapes[0] as TransformedShape;
            var mat = JMatrix.CreateScale(_colliderTransformDatas[i].Scale.ToJVector());
            mat *= JMatrix.CreateFromQuaternion(_colliderTransformDatas[i].Rotation.ToJQuaternion());
            shape.Transformation = mat;
        }
    }

    private string GetCleanBoneName(int boneId)
    {
        string name = _model.Bones[boneId].ToString();
        var words = name.Split(':');
        name = words[2];
        words = name.Split(' ');
        name = words[0];
        return name;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        var entityTransform = RaylibExtensions.TRS(Transform);
        for (int boneId = 0; boneId < _model.BoneCount; boneId++)
        {
            Vector3 bonePosition = Raymath.Vector3Transform(_transformDatas[boneId].Translation,
                Raymath.MatrixInvert(entityTransform));
            Quaternion boneRotation = _transformDatas[boneId].Rotation;
            Vector3 boneScale =
                Raymath.Vector3Transform(_transformDatas[boneId].Scale, Raymath.MatrixInvert(entityTransform));
            _modelAnimation.FramePoses[0][boneId].Translation = bonePosition;
            _modelAnimation.FramePoses[0][boneId].Rotation = boneRotation;
            _modelAnimation.FramePoses[0][boneId].Scale = boneScale;
        }

        UpdateModelAnimationBones(_model, _modelAnimation, 0);
    }

    public override void OnPostRender()
    {
        base.OnPostRender();
    }

    public override void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        foreach (var pair in _limbRbMap)
        {
            var colliderTransformData = _colliderTransformDatas[pair.Key];
            ImGui.Text(pair.Key.ToString());
            ImGui.InputFloat3("Position", ref colliderTransformData.Translation);
            var asVector4 = colliderTransformData.Rotation.AsVector4();
            ImGui.InputFloat4("Rotation", ref asVector4);
        }
    }

    public override void OnRender()
    {
        base.OnRender();

        Matrix4x4 matrix = RaylibExtensions.TRS(Transform);
        for (int i = 0; i < _model.MeshCount; i++)
        {
            DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
        }

        if (Engine.UIActive)
        {
            foreach (var pair in _limbRbMap)
            {
                var colliderTransformData = _colliderTransformDatas[pair.Key];
                if (Gizmo.DrawGizmo3D(
                        (int)(Gizmo.GizmoFlags.GIZMO_TRANSLATE | Gizmo.GizmoFlags.GIZMO_SCALE |
                              Gizmo.GizmoFlags.GIZMO_ROTATE),
                        ref colliderTransformData))
                {
                    _colliderTransformDatas[pair.Key] = colliderTransformData;
                    var shape = pair.Value.Shapes[0] as TransformedShape;
                    shape!.Translation = colliderTransformData.Translation.ToJVector();
                    var mat = JMatrix.CreateScale(colliderTransformData.Scale.ToJVector());
                    mat *= JMatrix.CreateFromQuaternion(colliderTransformData.Rotation.ToJQuaternion());
                    shape.Transformation = mat;
                }

                pair.Value.DebugDraw(Engine.PhysDrawer);
            }
        }
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        UnloadModel(_model);
        UnloadShader(_skinningShader);
    }
}