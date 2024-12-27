using System.Collections;
using System.Numerics;
using Engine.Physics;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine.Entities;

public unsafe class RagdollEntity : Entity
{
    public enum RagdollParts
    {
        Head,
        UpperLegLeft,
        UpperLegRight,
        LowerLegLeft,
        LowerLegRight,
        UpperArmLeft,
        UpperArmRight,
        LowerArmLeft,
        LowerArmRight,
        Torso
    }

    private RigidBody[] _parts;
    private Mesh _head, _arm, _leg, _torso;
    private Material _material;

    private struct MeshTag
    {
        public Mesh mesh;
        public int ID;
    }

    public RagdollEntity(string name, Vector3 spawnPt) : base(name)
    {
        _parts = new RigidBody[Enum.GetNames<RagdollParts>().Length];
        for (int i = 0; i < _parts.Length; i++)
        {
            _parts[i] = Engine.PhysicsWorld.CreateRigidBody();
        }

        var position = spawnPt.ToJVector();
        _head = GenMeshSphere(0.15f, 20, 20);
        _leg = GenMeshCylinder(0.08f, 0.3f, 20);
        _arm = GenMeshCylinder(0.06f, 0.2f, 20);
        _torso = GenMeshCube(0.35f, 0.6f, 0.2f);
        _material = LoadMaterialDefault();
        var image = GenImageChecked(16, 16, 2, 2, Color.White, Color.DarkGray);
        _material.Maps[(int)MaterialMapIndex.Diffuse].Texture = LoadTextureFromImage(image);
        UnloadImage(image);
        _parts[(int)RagdollParts.Head].AddShape(new SphereShape(0.15f));
        _parts[(int)RagdollParts.UpperLegLeft].AddShape(new CapsuleShape(0.08f, 0.3f));
        _parts[(int)RagdollParts.UpperLegRight].AddShape(new CapsuleShape(0.08f, 0.3f));
        _parts[(int)RagdollParts.LowerLegLeft].AddShape(new CapsuleShape(0.08f, 0.3f));
        _parts[(int)RagdollParts.LowerLegRight].AddShape(new CapsuleShape(0.08f, 0.3f));
        _parts[(int)RagdollParts.UpperArmLeft].AddShape(new CapsuleShape(0.07f, 0.2f));
        _parts[(int)RagdollParts.UpperArmRight].AddShape(new CapsuleShape(0.07f, 0.2f));
        _parts[(int)RagdollParts.LowerArmLeft].AddShape(new CapsuleShape(0.06f, 0.2f));
        _parts[(int)RagdollParts.LowerArmRight].AddShape(new CapsuleShape(0.06f, 0.2f));
        _parts[(int)RagdollParts.Torso].AddShape(new BoxShape(0.35f, 0.6f, 0.2f));

        _parts[(int)RagdollParts.Head].Tag = new MeshTag
        {
            mesh = _head,
            ID = 0
        };
        _parts[(int)RagdollParts.UpperLegLeft].Tag = _parts[(int)RagdollParts.UpperLegRight].Tag =
            _parts[(int)RagdollParts.LowerLegLeft].Tag = _parts[(int)RagdollParts.LowerLegRight].Tag = new MeshTag
            {
                mesh = _leg,
                ID = 1
            };


        _parts[(int)RagdollParts.UpperArmLeft].Tag = _parts[(int)RagdollParts.UpperArmRight].Tag =
            _parts[(int)RagdollParts.LowerArmLeft].Tag = _parts[(int)RagdollParts.LowerArmRight].Tag = new MeshTag
            {
                mesh = _arm,
                ID = 2
            };
        _parts[(int)RagdollParts.Torso].Tag = new MeshTag()
        {
            mesh = _torso,
            ID = 3
        };

        _parts[(int)RagdollParts.Head].Position = new JVector(0, 0, 0);
        _parts[(int)RagdollParts.Torso].Position = new JVector(0, -0.46f, 0);
        _parts[(int)RagdollParts.UpperLegLeft].Position = new JVector(0.11f, -0.85f, 0);
        _parts[(int)RagdollParts.UpperLegRight].Position = new JVector(-0.11f, -0.85f, 0);
        _parts[(int)RagdollParts.LowerLegLeft].Position = new JVector(0.11f, -1.2f, 0);
        _parts[(int)RagdollParts.LowerLegRight].Position = new JVector(-0.11f, -1.2f, 0);

        _parts[(int)RagdollParts.UpperArmLeft].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);
        _parts[(int)RagdollParts.UpperArmRight].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);
        _parts[(int)RagdollParts.LowerArmLeft].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);
        _parts[(int)RagdollParts.LowerArmRight].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);

        _parts[(int)RagdollParts.UpperArmLeft].Position = new JVector(0.30f, -0.2f, 0);
        _parts[(int)RagdollParts.UpperArmRight].Position = new JVector(-0.30f, -0.2f, 0);

        _parts[(int)RagdollParts.LowerArmLeft].Position = new JVector(0.55f, -0.2f, 0);
        _parts[(int)RagdollParts.LowerArmRight].Position = new JVector(-0.55f, -0.2f, 0);


        var spine0 =
            Engine.PhysicsWorld.CreateConstraint<BallSocket>(_parts[(int)RagdollParts.Head],
                _parts[(int)RagdollParts.Torso]);
        spine0.Initialize(new JVector(0, -0.15f, 0));

        var spine1 =
            Engine.PhysicsWorld.CreateConstraint<ConeLimit>(_parts[(int)RagdollParts.Head],
                _parts[(int)RagdollParts.Torso]);
        spine1.Initialize(-JVector.UnitZ, AngularLimit.FromDegree(0, 45));

        var hipLeft0 =
            Engine.PhysicsWorld.CreateConstraint<BallSocket>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft0.Initialize(new JVector(0.11f, -0.7f, 0));

        var hipLeft1 =
            Engine.PhysicsWorld.CreateConstraint<TwistAngle>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft1.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, +80));

        var hipLeft2 =
            Engine.PhysicsWorld.CreateConstraint<ConeLimit>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft2.Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        var hipRight0 =
            Engine.PhysicsWorld.CreateConstraint<BallSocket>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperLegRight]);
        hipRight0.Initialize(new JVector(-0.11f, -0.7f, 0));

        var hipRight1 =
            Engine.PhysicsWorld.CreateConstraint<TwistAngle>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperLegRight]);
        hipRight1.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, +80));

        var hipRight2 =
            Engine.PhysicsWorld.CreateConstraint<ConeLimit>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperLegRight]);
        hipRight2.Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        var kneeLeft = new HingeJoint(Engine.PhysicsWorld, _parts[(int)RagdollParts.UpperLegLeft],
            _parts[(int)RagdollParts.LowerLegLeft],
            new JVector(0.11f, -1.05f, 0), JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        var kneeRight = new HingeJoint(Engine.PhysicsWorld, _parts[(int)RagdollParts.UpperLegRight],
            _parts[(int)RagdollParts.LowerLegRight],
            new JVector(-0.11f, -1.05f, 0), JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        var armLeft = new HingeJoint(Engine.PhysicsWorld, _parts[(int)RagdollParts.LowerArmLeft],
            _parts[(int)RagdollParts.UpperArmLeft],
            new JVector(0.42f, -0.2f, 0), JVector.UnitY, AngularLimit.FromDegree(-160, 0));

        var armRight = new HingeJoint(Engine.PhysicsWorld, _parts[(int)RagdollParts.LowerArmRight],
            _parts[(int)RagdollParts.UpperArmRight],
            new JVector(-0.42f, -0.2f, 0), JVector.UnitY, AngularLimit.FromDegree(0, 160));

        var shoulderLeft0 =
            Engine.PhysicsWorld.CreateConstraint<BallSocket>(_parts[(int)RagdollParts.UpperArmLeft],
                _parts[(int)RagdollParts.Torso]);
        shoulderLeft0.Initialize(new JVector(0.20f, -0.2f, 0));

        var shoulderLeft1 =
            Engine.PhysicsWorld.CreateConstraint<TwistAngle>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperArmLeft]);
        shoulderLeft1.Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        var shoulderRight0 =
            Engine.PhysicsWorld.CreateConstraint<BallSocket>(_parts[(int)RagdollParts.UpperArmRight],
                _parts[(int)RagdollParts.Torso]);
        shoulderRight0.Initialize(new JVector(-0.20f, -0.2f, 0));

        var shoulderRight1 =
            Engine.PhysicsWorld.CreateConstraint<TwistAngle>(_parts[(int)RagdollParts.Torso],
                _parts[(int)RagdollParts.UpperArmRight]);
        shoulderRight1.Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        shoulderLeft1.Bias = 0.01f;
        shoulderRight1.Bias = 0.01f;
        hipLeft1.Bias = 0.01f;
        hipRight1.Bias = 0.01f;

        if (Engine.PhysicsWorld.BroadPhaseFilter is not IgnoreCollisionBetweenFilter filter)
        {
            filter = new IgnoreCollisionBetweenFilter();
            Engine.PhysicsWorld.BroadPhaseFilter = filter;
        }

        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperLegLeft].Shapes[0],
            _parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperLegRight].Shapes[0],
            _parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperLegLeft].Shapes[0],
            _parts[(int)RagdollParts.LowerLegLeft].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperLegRight].Shapes[0],
            _parts[(int)RagdollParts.LowerLegRight].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperArmLeft].Shapes[0],
            _parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperArmRight].Shapes[0],
            _parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperArmLeft].Shapes[0],
            _parts[(int)RagdollParts.LowerArmLeft].Shapes[0]);
        filter.IgnoreCollisionBetween(_parts[(int)RagdollParts.UpperArmRight].Shapes[0],
            _parts[(int)RagdollParts.LowerArmRight].Shapes[0]);

        for (int i = 0; i < _parts.Length; i++)
        {
            _parts[i].Position += position;
        }
    }

    public override void OnRender()
    {
        base.OnRender();
        foreach (var rigidBody in _parts)
        {
            var position = rigidBody.Position.ToVector3();
            var rotation = rigidBody.Orientation.ToQuaternion();
            var mat = RaylibExtensions.TRS(position, rotation, Vector3.One);
            var tag = (MeshTag)rigidBody.Tag!;
            if (tag.ID == 2)
            {
                mat *= Raymath.MatrixTranslate(0f, -0.5f * 0.2f, 0f);
            }
            else if (tag.ID == 1)
            {
                mat *= Raymath.MatrixTranslate(0f, -0.5f * 0.3f, 0f);
            }

            DrawMesh(tag.mesh, _material, mat);
        }
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        foreach (var rigidBody in _parts)
        {
            Engine.PhysicsWorld.Remove(rigidBody);
        }

        UnloadMesh(_head);
        UnloadMesh(_leg);
        UnloadMesh(_arm);
        UnloadMesh(_torso);
    }
    // private Model _model;
    // private Shader _skinningShader;
    // private static readonly string FragPath = Path.Combine("Resources", "Shaders", "skinning.frag");
    // private static readonly string VertPath = Path.Combine("Resources", "Shaders", "skinning.vert");
    // private ModelAnimation _modelAnimation;
    // private Gizmo.TransformData[] _transformDatas;
    // private Dictionary<Limb, Gizmo.TransformData> _colliderTransformDatas = new();
    // private Dictionary<string, BoneInfo> _cleanBoneNames = new();
    // private Dictionary<Limb, RigidBody> _limbRbMap = new();
    //
    // public enum Limb
    // {
    //     Hips = 0,
    //     Spine,
    //     Spine1,
    //     Spine2,
    //     Neck,
    //     LeftShoulder,
    //     LeftForeArm,
    //     LeftHand,
    //     RightShoulder,
    //     RightForeArm,
    //     RightHand,
    //     LeftUpLeg,
    //     LeftLeg,
    //     LeftFoot,
    //     RightUpLeg,
    //     RightLeg,
    //     RightFoot,
    //     None
    // }
    //
    // private Dictionary<string, Limb> _stringToLimbGroup = new()
    // {
    //     { "Hips", Limb.Hips },
    //
    //     { "Spine", Limb.Spine },
    //
    //     { "Spine1", Limb.Spine1 },
    //
    //     { "Spine2", Limb.Spine2 },
    //
    //     { "Neck", Limb.Neck },
    //     { "Head", Limb.Neck },
    //     { "HeadTop_End", Limb.Neck },
    //
    //     { "LeftShoulder", Limb.LeftShoulder },
    //     { "LeftArm", Limb.LeftShoulder },
    //
    //     { "LeftForeArm", Limb.LeftForeArm },
    //
    //     { "LeftHand", Limb.LeftHand },
    //
    //     { "LeftHandThumb1", Limb.LeftHand },
    //     { "LeftHandThumb2", Limb.LeftHand },
    //     { "LeftHandThumb3", Limb.LeftHand },
    //     { "LeftHandThumb4", Limb.LeftHand },
    //     { "LeftHandIndex1", Limb.LeftHand },
    //     { "LeftHandIndex2", Limb.LeftHand },
    //     { "LeftHandIndex3", Limb.LeftHand },
    //     { "LeftHandIndex4", Limb.LeftHand },
    //     { "LeftHandMiddle1", Limb.LeftHand },
    //     { "LeftHandMiddle2", Limb.LeftHand },
    //     { "LeftHandMiddle3", Limb.LeftHand },
    //     { "LeftHandMiddle4", Limb.LeftHand },
    //     { "LeftHandRing1", Limb.LeftHand },
    //     { "LeftHandRing2", Limb.LeftHand },
    //     { "LeftHandRing3", Limb.LeftHand },
    //     { "LeftHandRing4", Limb.LeftHand },
    //     { "LeftHandPinky1", Limb.LeftHand },
    //     { "LeftHandPinky2", Limb.LeftHand },
    //     { "LeftHandPinky3", Limb.LeftHand },
    //     { "LeftHandPinky4", Limb.LeftHand },
    //
    //     { "RightShoulder", Limb.RightShoulder },
    //     { "RightArm", Limb.RightShoulder },
    //
    //     { "RightForeArm", Limb.RightForeArm },
    //
    //     { "RightHand", Limb.RightHand },
    //     { "RightHandThumb1", Limb.RightHand },
    //     { "RightHandThumb2", Limb.RightHand },
    //     { "RightHandThumb3", Limb.RightHand },
    //     { "RightHandThumb4", Limb.RightHand },
    //     { "RightHandIndex1", Limb.RightHand },
    //     { "RightHandIndex2", Limb.RightHand },
    //     { "RightHandIndex3", Limb.RightHand },
    //     { "RightHandIndex4", Limb.RightHand },
    //     { "RightHandMiddle1", Limb.RightHand },
    //     { "RightHandMiddle2", Limb.RightHand },
    //     { "RightHandMiddle3", Limb.RightHand },
    //     { "RightHandMiddle4", Limb.RightHand },
    //     { "RightHandRing1", Limb.RightHand },
    //     { "RightHandRing2", Limb.RightHand },
    //     { "RightHandRing3", Limb.RightHand },
    //     { "RightHandRing4", Limb.RightHand },
    //     { "RightHandPinky1", Limb.RightHand },
    //     { "RightHandPinky2", Limb.RightHand },
    //     { "RightHandPinky3", Limb.RightHand },
    //     { "RightHandPinky4", Limb.RightHand },
    //
    //     { "LeftUpLeg", Limb.LeftUpLeg },
    //
    //     { "LeftLeg", Limb.LeftLeg },
    //
    //     { "LeftFoot", Limb.LeftFoot },
    //     { "LeftToeBase", Limb.LeftFoot },
    //     { "LeftToe_End", Limb.LeftFoot },
    //
    //     { "RightUpLeg", Limb.RightUpLeg },
    //
    //     { "RightLeg", Limb.RightLeg },
    //
    //     { "RightFoot", Limb.RightFoot },
    //     { "RightToeBase", Limb.RightFoot },
    //     { "RightToe_End", Limb.RightFoot }
    // };
    //
    // private Dictionary<Limb, List<int>> _limbMapping = new();
    //
    //
    // public RagdollEntity(string modelPath) : base(modelPath)
    // {
    //     _model = Raylib.LoadModel(modelPath);
    //     var tr = Transform;
    //     tr.Translation = new Vector3(0f, -1f, 0f);
    //     tr.Scale = Vector3.One * 0.01f;
    //     tr.Rotation = Raymath.QuaternionFromEuler(float.DegreesToRadians(90f), 0f, 0f);
    //     Transform = tr;
    //
    //     _skinningShader = LoadShader(VertPath, FragPath);
    //     for (int i = 0; i < _model.MaterialCount; i++)
    //     {
    //         _model.Materials[i].Shader = _skinningShader;
    //     }
    //
    //     _transformDatas = new Gizmo.TransformData[_model.BoneCount];
    //
    //     ModelAnimation* modelAnimations = LoadModelAnimations(modelPath, out var count);
    //     _modelAnimation = modelAnimations[0];
    //     var entityTransform = RaylibExtensions.TRS(Transform);
    //     for (int boneId = 0; boneId < _model.BoneCount; boneId++)
    //     {
    //         var frame = _modelAnimation.FramePoses[0];
    //         Vector3 bonePosition = Raymath.Vector3Transform(frame[boneId].Translation, entityTransform);
    //         Quaternion boneRotation = frame[boneId].Rotation;
    //         Vector3 boneScale = Raymath.Vector3Transform(frame[boneId].Scale, entityTransform);
    //
    //         _transformDatas[boneId] = new Gizmo.TransformData(bonePosition, boneRotation, boneScale);
    //         var name = GetCleanBoneName(boneId);
    //         _cleanBoneNames.Add(name, _model.Bones[boneId]);
    //         _limbMapping.TryAdd(_stringToLimbGroup[name], new());
    //         _limbMapping[_stringToLimbGroup[name]].Add(boneId);
    //     }
    //
    //     for (Limb i = Limb.Hips; i < Limb.None; i++)
    //     {
    //         //make da ragdoll yeah!
    //         var rb = Engine.PhysicsWorld.CreateRigidBody();
    //         rb.AddShape(new TransformedShape(new BoxShape(1f), JVector.Zero));
    //         rb.Position = _transformDatas[_limbMapping[i][0]].Translation.ToJVector();
    //         rb.IsStatic = true;
    //         _limbRbMap.Add(i, rb);
    //         _colliderTransformDatas.Add(i, new Gizmo.TransformData(_transformDatas[_limbMapping[i][0]])
    //         {
    //             Scale = Vector3.One * 0.1f
    //         });
    //         var shape = rb.Shapes[0] as TransformedShape;
    //         var mat = JMatrix.CreateScale(_colliderTransformDatas[i].Scale.ToJVector());
    //         mat *= JMatrix.CreateFromQuaternion(_colliderTransformDatas[i].Rotation.ToJQuaternion());
    //         shape.Transformation = mat;
    //     }
    // }
    //
    // private string GetCleanBoneName(int boneId)
    // {
    //     string name = _model.Bones[boneId].ToString();
    //     var words = name.Split(':');
    //     name = words[2];
    //     words = name.Split(' ');
    //     name = words[0];
    //     return name;
    // }
    //
    // public override void OnUpdate()
    // {
    //     base.OnUpdate();
    //
    //     var entityTransform = RaylibExtensions.TRS(Transform);
    //     for (int boneId = 0; boneId < _model.BoneCount; boneId++)
    //     {
    //         Vector3 bonePosition = Raymath.Vector3Transform(_transformDatas[boneId].Translation,
    //             Raymath.MatrixInvert(entityTransform));
    //         Quaternion boneRotation = _transformDatas[boneId].Rotation;
    //         Vector3 boneScale =
    //             Raymath.Vector3Transform(_transformDatas[boneId].Scale, Raymath.MatrixInvert(entityTransform));
    //         _modelAnimation.FramePoses[0][boneId].Translation = bonePosition;
    //         _modelAnimation.FramePoses[0][boneId].Rotation = boneRotation;
    //         _modelAnimation.FramePoses[0][boneId].Scale = boneScale;
    //     }
    //
    //     UpdateModelAnimationBones(_model, _modelAnimation, 0);
    // }
    //
    // public override void OnPostRender()
    // {
    //     base.OnPostRender();
    // }
    //
    // public override void OnImGuiWindowRender()
    // {
    //     base.OnImGuiWindowRender();
    //     foreach (var pair in _limbRbMap)
    //     {
    //         var colliderTransformData = _colliderTransformDatas[pair.Key];
    //         ImGui.Text(pair.Key.ToString());
    //         ImGui.InputFloat3("Position", ref colliderTransformData.Translation);
    //         var asVector4 = colliderTransformData.Rotation.AsVector4();
    //         ImGui.InputFloat4("Rotation", ref asVector4);
    //     }
    // }
    //
    // public override void OnRender()
    // {
    //     base.OnRender();
    //
    //     Matrix4x4 matrix = RaylibExtensions.TRS(Transform);
    //     for (int i = 0; i < _model.MeshCount; i++)
    //     {
    //         DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
    //     }
    //
    //     if (Engine.UIActive)
    //     {
    //         foreach (var pair in _limbRbMap)
    //         {
    //             var colliderTransformData = _colliderTransformDatas[pair.Key];
    //             if (Gizmo.DrawGizmo3D(
    //                     (int)(Gizmo.GizmoFlags.GIZMO_TRANSLATE | Gizmo.GizmoFlags.GIZMO_SCALE |
    //                           Gizmo.GizmoFlags.GIZMO_ROTATE),
    //                     ref colliderTransformData))
    //             {
    //                 _colliderTransformDatas[pair.Key] = colliderTransformData;
    //                 var shape = pair.Value.Shapes[0] as TransformedShape;
    //                 shape!.Translation = colliderTransformData.Translation.ToJVector();
    //                 var mat = JMatrix.CreateScale(colliderTransformData.Scale.ToJVector());
    //                 mat *= JMatrix.CreateFromQuaternion(colliderTransformData.Rotation.ToJQuaternion());
    //                 shape.Transformation = mat;
    //             }
    //
    //             pair.Value.DebugDraw(Engine.PhysDrawer);
    //         }
    //     }
    // }
    //
    // public override void OnCleanup()
    // {
    //     base.OnCleanup();
    //     UnloadModel(_model);
    //     UnloadShader(_skinningShader);
    // }
}