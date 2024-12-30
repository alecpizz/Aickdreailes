using System.Numerics;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Raymath;

namespace Engine.Entities;

public class StaticEntity : Entity
{
    private Model _model;
    private RigidBody _rigidBody;
    private Shader _shader;
    private float _fogDensity = 0.026f;
    public unsafe StaticEntity(string path, Vector3 position) : base(path)
    {
        try
        {
            _model = LoadModel(path);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return;
        }

        var tr = Transform;
        tr.Translation = position;
        Transform = tr;
        
        string frag = Path.Combine("Resources", "Shaders", "fog.frag");
        string vert = Path.Combine("Resources", "Shaders", "fog.vert");
        _shader = LoadShader(vert, frag);
        _shader.Locs[(int)ShaderLocationIndex.VectorView] = GetShaderLocation(_shader, "viewPos");
        _shader.Locs[(int)ShaderLocationIndex.MatrixView] = GetShaderLocation(_shader, "matModel");
        SetShaderValue(_shader, GetShaderLocation(_shader, "fogDensity"), _fogDensity, ShaderUniformDataType.Float);
        for (int i = 0; i < _model.MaterialCount; i++)
        {
            if (_model.Materials[i].Maps != null)
            {
                _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture.Mipmaps = 4;
                GenTextureMipmaps(ref _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture);
                SetTextureFilter(_model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture,
                    TextureFilter.Trilinear);
                _model.Materials[i].Shader = _shader;
            }
        }

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

    public override void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        if (ImGui.SliderFloat("Fog Density", ref _fogDensity, 0f, 1f))
        {
            SetShaderValue(_shader, GetShaderLocation(_shader, "fogDensity"), _fogDensity, ShaderUniformDataType.Float);
        }
    }

    public override unsafe void OnRender()
    {
        //TIL that the sys numerics matrix implementation doesn't work with raylib!
        SetShaderValue(_shader, _shader.Locs[(int)ShaderLocationIndex.VectorView], Engine.Camera.Position, ShaderUniformDataType.Vec3);
        Matrix4x4 matrix = RaylibExtensions.TRS(Transform);
        for (int i = 0; i < _model.MeshCount; i++)
        {
            DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
        }
        // DrawModel(_model, Vector3.Zero, 1f, Color.White);
    }

    public override void OnCleanup()
    {
        UnloadShader(_shader);
        UnloadModel(_model);
        Engine.PhysicsWorld.Remove(_rigidBody);
    }
}