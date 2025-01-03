using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Raymath;

namespace Engine.Entities;

public class StaticEntityPBR : Entity
{
    private Model _model;
    private RigidBody _rigidBody;
    private Shader _shader;
    private float _envLightIntensity = 1.0F;
    private bool _spin = false;
    
    private static readonly string PbrVert = Path.Combine(
        "Resources", "Shaders", "pbr.vert"
    );
    private static readonly string PbrFrag = Path.Combine(
        "Resources", "Shaders", "pbr.frag"
    );

    private SkyboxEntityPBR _skybox;
    private Light[] _lights;
    public unsafe StaticEntityPBR(
        string path, 
        Vector3 position,
        SkyboxEntityPBR skybox,
        Light[] lights) : base(path)
    {
        _skybox = skybox;
        _lights = lights;
        Light._lightsCount = 0;
        try
        {
            _model = LoadModel(path);
            for (int i = 0; i < _model.MeshCount; i++)
            {
                if (_model.Meshes[i].Tangents == null)
                {
                    var prevColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"WARNING: NO TANGENTS ON MESH {path}");
                    _model.Meshes[i].AllocTangents();
                    GenMeshTangents(ref _model.Meshes[i]);
                    Console.ForegroundColor = prevColor;
                }
            }
            // _model = LoadModelFromMesh(
            //     GenMeshSphere(1.0F, 32, 32)
            // );

            Material testMat = LoadMaterialDefault();
            
            testMat.Maps[(int)MaterialMapIndex.Albedo].Texture = LoadTextureFromImage(
                GenImageColor(1, 1, Color.White)
            );
            testMat.Maps[(int)MaterialMapIndex.Normal].Texture = LoadTextureFromImage(
                GenImageColor(1, 1, new Color(128, 128, 255))
            );
            testMat.Maps[(int)MaterialMapIndex.Roughness].Texture = LoadTextureFromImage(
                GenImageColor(1, 1,  new Color(255, 1, 255))
            );

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
        _shader.Locs[(int)ShaderLocationIndex.MapPrefilter] = GetShaderLocation(
            _shader,
            "prefilterMap"
        );
        _shader.Locs[(int)ShaderLocationIndex.MapBrdf] = GetShaderLocation(
            _shader,
            "brdfLUT"
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
        _shader.Locs[(int)ShaderLocationIndex.MatrixMvp] = GetShaderLocation(
            _shader,
            "mvp"
        );
        _shader.Locs[(int)ShaderLocationIndex.MatrixModel] = GetShaderLocation(
            _shader,
            "matModel"
        );
        
        // Create materials/pass data
        for (int i = 0; i < _model.MaterialCount; i++)
        {
            Material mat = LoadMaterialDefault();
            mat.Shader = _shader;

            mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = skybox.GetEnvironment();
            mat.Maps[(int)MaterialMapIndex.Irradiance].Texture = skybox.GetIrradiance();
            mat.Maps[(int)MaterialMapIndex.Prefilter].Texture = skybox.GetPrefilter();
            mat.Maps[(int)MaterialMapIndex.Brdf].Texture = skybox.GetBrdf();
            
            // TODO: Account for when no texture is loaded in slot
            mat.Maps[(int)MaterialMapIndex.Albedo].Texture =
                _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture;
            mat.Maps[(int)MaterialMapIndex.Albedo].Texture.Mipmaps = 4;
            GenTextureMipmaps(ref mat.Maps[(int)MaterialMapIndex.Albedo].Texture);
            SetTextureFilter(
                mat.Maps[(int)MaterialMapIndex.Albedo].Texture,
                TextureFilter.Bilinear
            );

            if (_model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture.Id != 0)
            {
                mat.Maps[(int)MaterialMapIndex.Normal].Texture =
                    _model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture;
            }
            else
            {
                Image blankNormal = GenImageColor(1, 1, new Color(128, 128, 255));
                mat.Maps[(int)MaterialMapIndex.Normal].Texture = 
                    LoadTextureFromImage(blankNormal);
            }

            if (_model.Materials[i].Maps[(int) MaterialMapIndex.Roughness].Texture.Id != 0)
            {
                mat.Maps[(int)MaterialMapIndex.Roughness].Texture =
                    _model.Materials[i].Maps[(int) MaterialMapIndex.Roughness].Texture;
            }
            else
            {
                Image blankRoughness = GenImageColor(1, 1, new Color(255, 216, 0));
                mat.Maps[(int)MaterialMapIndex.Roughness].Texture =
                    LoadTextureFromImage(blankRoughness);
            }

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

  


    public override unsafe void OnImGuiWindowRender()
    {
        base.OnImGuiWindowRender();
        if (ImGui.SliderFloat("Environ. Light Intensity", ref _envLightIntensity, 0.0F, 20.0F))
        {
            SetShaderValue(
                _shader, 
                GetShaderLocation(_shader, "envLightIntensity"), 
                _envLightIntensity, 
                ShaderUniformDataType.Float
            );
        }

        if (ImGui.Button("Reload PBR Shader"))
        {
            UnloadShader(_shader);
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
            _shader.Locs[(int)ShaderLocationIndex.MapPrefilter] = GetShaderLocation(
                _shader,
                "prefilterMap"
            );
            _shader.Locs[(int)ShaderLocationIndex.MapBrdf] = GetShaderLocation(
                _shader,
                "brdfLUT"
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
            _shader.Locs[(int)ShaderLocationIndex.MatrixMvp] = GetShaderLocation(
                _shader,
                "mvp"
            );
            _shader.Locs[(int)ShaderLocationIndex.MatrixModel] = GetShaderLocation(
                _shader,
                "matModel"
            );
            
            for (int i = 0; i < _model.MaterialCount; i++)
            {
                Material mat = LoadMaterialDefault();
                mat.Shader = _shader;

                mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = _skybox.GetEnvironment();
                mat.Maps[(int)MaterialMapIndex.Irradiance].Texture = _skybox.GetIrradiance();
                mat.Maps[(int)MaterialMapIndex.Prefilter].Texture = _skybox.GetPrefilter();
                mat.Maps[(int)MaterialMapIndex.Brdf].Texture = _skybox.GetBrdf();
            
                // TODO: Account for when no texture is loaded in slot
                mat.Maps[(int)MaterialMapIndex.Albedo].Texture =
                    _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture;
                mat.Maps[(int)MaterialMapIndex.Normal].Texture =
                    _model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture;
                mat.Maps[(int)MaterialMapIndex.Roughness].Texture =
                    _model.Materials[i].Maps[(int)MaterialMapIndex.Roughness].Texture;
                Light._lightsCount = 0;
                // Send lighting data
                foreach (Light light in _lights)
                {
                    Light.CreateLight(
                        light.Type,
                        light.Position,
                        light.Target,
                        light.Color,
                        _shader
                    );
                }

                _model.Materials[i] = mat;
            }
        }

        ImGui.Checkbox("SPEEN", ref _spin);
    }

    public override unsafe void OnRender()
    {
        //TIL that the sys numerics matrix implementation doesn't work with raylib!
        SetShaderValue(_shader, _shader.Locs[(int)ShaderLocationIndex.VectorView], Engine.Camera.Position, ShaderUniformDataType.Vec3);
        Matrix4x4 matrix = RaylibExtensions.TRS(Transform);
        
        if (_spin)
        {
            matrix *= MatrixRotateY((float) GetTime() * 0.5F);
        }
        
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