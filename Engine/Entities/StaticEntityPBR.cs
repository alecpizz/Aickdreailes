using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Engine.Rendering;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Raymath;
using ShaderType = Engine.Rendering.ShaderType;

namespace Engine.Entities;

public class StaticEntityPBR : Entity
{
    private Model _model;
    private RigidBody _rigidBody;
    private float _envLightIntensity = 1.0F;
    private bool _spin = false;

    public unsafe StaticEntityPBR(
        string path, 
        Vector3 position) : base(path)
    {
     
        try
        {
            _model = LoadModel(path);
          
            // _model = LoadModelFromMesh(
            //     GenMeshSphere(1.0F, 32, 32)
            // );

            Material testMat = LoadMaterialDefault();

            var image = GenImageColor(1, 1, Color.White);
            testMat.Maps[(int)MaterialMapIndex.Albedo].Texture = LoadTextureFromImage(image);
            UnloadImage(image);
            image = GenImageColor(1, 1, new Color(128, 128, 255));
            testMat.Maps[(int)MaterialMapIndex.Normal].Texture = LoadTextureFromImage(image);
            UnloadImage(image);
            image = GenImageColor(1, 1, new Color(255, 1, 255));
            testMat.Maps[(int)MaterialMapIndex.Roughness].Texture = LoadTextureFromImage(image);
            UnloadImage(image);
            _model.Materials[0] = testMat;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return;
        }

        var tr = Transform;
        tr.Translation = position;
        Transform = tr;

        // Create materials/pass data
        Engine.ShaderManager.SetupModelMaterials(ref _model, ShaderType.Static);

        _rigidBody = Engine.PhysicsWorld.CreateRigidBody();
        _rigidBody.Tag = this;
        List<JTriangle> tris = new List<JTriangle>();

        for (int i = 0; i < _model.MeshCount; i++)
        {
            var mesh = _model.Meshes[i];
            Vector3* vertdata = (Vector3*)mesh.Vertices;

            for (int j = 0; j < mesh.TriangleCount; j++)
            {
                JVector a, b, c;
                if (mesh.Indices != null)
                {
                    a = vertdata[mesh.Indices[j * 3 + 0]].ToJVector();
                    b = vertdata[mesh.Indices[j * 3 + 1]].ToJVector();
                    c = vertdata[mesh.Indices[j * 3 + 2]].ToJVector();
                }
                else
                {
                    a = vertdata[i*3 + 0].ToJVector();
                    b = vertdata[i*3 + 1].ToJVector();
                    c = vertdata[i*3 + 2].ToJVector();
                }

                JVector normal = (c - b) % (a - b);

                if (MathHelper.CloseToZero(normal, 1e-12f))
                {
                    continue;
                }

                tris.Add(new JTriangle(b, a, c));
            }
        }

        var jtm = new TriangleMesh(tris);
        List<RigidBodyShape> triangleShapes = new List<RigidBodyShape>();
        for (int i = 0; i < jtm.Indices.Length; i++)
        {
            TriangleShape ts = new TriangleShape(jtm, i);
            triangleShapes.Add(ts);
        }
        
        _rigidBody.AddShape(triangleShapes, false);
        _rigidBody.Position = Transform.Translation.ToJVector();
        _rigidBody.IsStatic = true;
    }

    public override unsafe void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        ImGui.Checkbox("SPEEN", ref _spin);
    }

    public override unsafe void OnRender(Shader? shader = null)
    {
        //TIL that the sys numerics matrix implementation doesn't work with raylib!
        Matrix4x4 matrix = RaylibExtensions.TRS(Transform);
        
        if (_spin)
        {
            matrix *= MatrixRotateY((float) GetTime() * 0.5F);
        }

        var oldShader = _model.Materials[0].Shader;
        if (shader != null)
        {
            for (int i = 0; i < _model.MaterialCount; i++)
            {
                _model.Materials[i].Shader = shader.Value;
            }
        }
        for (int i = 0; i < _model.MeshCount; i++)
        {
            DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
        }
        if (shader != null)
        {
            for (int i = 0; i < _model.MaterialCount; i++)
            {
                _model.Materials[i].Shader = oldShader;
            }
        }
    }

    public override void OnCleanup()
    {
        UnloadModel(_model);
        Engine.PhysicsWorld.Remove(_rigidBody);
    }
}