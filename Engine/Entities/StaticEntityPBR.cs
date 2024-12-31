using System.Numerics;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;
using static Raylib_cs.BleedingEdge.Raymath;

namespace Engine.Entities;

public class StaticEntityPBR : Entity
{
    private Model _model;
    private RigidBody _rigidBody;
    private Shader _shader;
    private float _fogDensity = 0.026f;
    
    private static readonly string PbrVert = Path.Combine(
        "Resources", "Shaders", "pbr.vert"
    );
    private static readonly string PbrFrag = Path.Combine(
        "Resources", "Shaders", "pbr.frag"
    );
    
    public unsafe StaticEntityPBR(
        string path, 
        Vector3 position,
        SkyboxEntityPBR skybox,
        Light[] lights) : base(path)
    {
        try
        {
            //_model = LoadModel(path);
            _model = LoadModelFromMesh(
                GenMeshSphere(1.0F, 32, 32)
            );

            Material testMat = LoadMaterialDefault();
            
            testMat.Maps[(int)MaterialMapIndex.Albedo].Texture = LoadTextureFromImage(
                GenImageColor(1, 1, Color.Red)
            );
            testMat.Maps[(int)MaterialMapIndex.Normal].Texture = LoadTextureFromImage(
                GenImageColor(1, 1, new Color(128, 128, 255))
            );
            testMat.Maps[(int)MaterialMapIndex.Roughness].Texture = LoadTextureFromImage(
                GenImageColor(1, 1,  new Color(255, 15, 0))
            );

            _model.Materials[0] = testMat;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return;
        }

        MatrixDecompose(_model.Transform, out var translation,
            out var rotation, out var scale);

        var tr = Transform;
        tr.Translation = position;
        tr.Rotation = rotation;
        tr.Scale = scale;
        Transform = tr;

        _shader = LoadShader(PbrVert, PbrFrag);

        // Setup data locations
        _shader.Locs[(int)ShaderLocationIndex.MapCubemap] = GetShaderLocation(
            _shader,
            "environmentMap"
        );
        _shader.Locs[(int)ShaderLocationIndex.MapIrradiance] = GetShaderLocation(
            _shader,
            "irradianceMap"
        );
        _shader.Locs[(int)ShaderLocationIndex.MapAlbedo] = GetShaderLocation(
            _shader,
            "albedoMap"
        );
        _shader.Locs[(int)ShaderLocationIndex.MapNormal] = GetShaderLocation(
            _shader,
            "normalMap"
        );
        _shader.Locs[(int)ShaderLocationIndex.MapRoughness] = GetShaderLocation(
            _shader,
            "ormMap"
        );
        
        // Uniform locs
        _shader.Locs[(int)ShaderLocationIndex.VectorView] = GetShaderLocation(
            _shader,
            "viewPos"
        );
        
        // Create materials/pass data
        for (int i = 0; i < _model.MaterialCount; i++)
        {
            Material mat = LoadMaterialDefault();
            mat.Shader = _shader;

            mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = skybox.GetEnvironment();
            mat.Maps[(int)MaterialMapIndex.Irradiance].Texture = skybox.GetIrradiance();
            
            // TODO: Account for when no texture is loaded in slot
            mat.Maps[(int)MaterialMapIndex.Albedo].Texture =
                _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture;
            mat.Maps[(int)MaterialMapIndex.Normal].Texture =
                _model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture;
            mat.Maps[(int)MaterialMapIndex.Roughness].Texture =
                _model.Materials[i].Maps[(int)MaterialMapIndex.Roughness].Texture;
            
            // Send lighting data
            foreach (Light light in lights)
            {
                Light.CreateLight(
                    light.Type,
                    light.Position,
                    light.Target,
                    light.Color,
                    mat.Shader
                );
            }

            _model.Materials[i] = mat;
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
    }

    public override void OnCleanup()
    {
        UnloadShader(_shader);
        UnloadModel(_model);
        Engine.PhysicsWorld.Remove(_rigidBody);
    }
}