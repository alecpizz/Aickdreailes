using System.Numerics;
using Engine.Entities;
using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine.Rendering;

public enum ShaderType
{
    Skinned,
    Static,
    None
}

/// <summary>
/// General class for global shaders and access
/// </summary>
public unsafe class ShaderManager : IDisposable
{
    private static readonly string PbrVert = Path.Combine(
        "Resources", "Shaders", "pbr.vert"
    );

    private static readonly string PbrFrag = Path.Combine(
        "Resources", "Shaders", "pbr.frag"
    );

    private static readonly string SkinnedVert = Path.Combine("Resources", "Shaders", "skinning.vert");
    public SkyboxEntityPBR Skybox { get; private set; }
    private int _lightCount = 0;
    public Dictionary<ShaderType, Shader> Shaders { get; private set; } = new Dictionary<ShaderType, Shader>();
    private Dictionary<Shader, Dictionary<Light, LightLocations>> _shaderLocMap = new();
    public const int MaxLights = 4;
    private float _envLightIntensity = 1.0f;
    private float _fogDensity = 0.0f;
    private Vector3 _fogColor = new Vector3(0.5f, 0.5f, 0.5f);
    public Light DirectionalLight { get; private set; }

    public ShaderManager(SkyboxEntityPBR skybox)
    {
        Skybox = skybox;
        var staticPbr = Raylib.LoadShader(PbrVert, PbrFrag);
        Shaders.Add(ShaderType.Static, staticPbr);
        var skinnedPbr = Raylib.LoadShader(SkinnedVert, PbrFrag);
        Shaders.Add(ShaderType.Skinned, skinnedPbr);
        _shaderLocMap.Add(staticPbr, new());
        _shaderLocMap.Add(skinnedPbr, new());
        foreach (var pair in Shaders)
        {
            var shader = pair.Value;
            shader.Locs[(int)ShaderLocationIndex.MapCubemap] = GetShaderLocation(
                shader,
                "environmentMap"
            );
            shader.Locs[(int)ShaderLocationIndex.MapIrradiance] = GetShaderLocation(
                shader,
                "irradianceMap"
            );
            shader.Locs[(int)ShaderLocationIndex.MapPrefilter] = GetShaderLocation(
                shader,
                "prefilterMap"
            );
            shader.Locs[(int)ShaderLocationIndex.MapBrdf] = GetShaderLocation(
                shader,
                "brdfLUT"
            );
            shader.Locs[(int)ShaderLocationIndex.MapAlbedo] = GetShaderLocation(
                shader,
                "albedoMap"
            );
            shader.Locs[(int)ShaderLocationIndex.MapNormal] = GetShaderLocation(
                shader,
                "normalMap"
            );
            shader.Locs[(int)ShaderLocationIndex.MapRoughness] = GetShaderLocation(
                shader,
                "ormMap"
            );

            // Uniform locs
            shader.Locs[(int)ShaderLocationIndex.VectorView] = GetShaderLocation(
                shader,
                "viewPos"
            );
            shader.Locs[(int)ShaderLocationIndex.MatrixMvp] = GetShaderLocation(
                shader,
                "mvp"
            );
            shader.Locs[(int)ShaderLocationIndex.MatrixModel] = GetShaderLocation(
                shader,
                "matModel"
            );
        }
    }

    public void AddLight(Light light)
    {
        if (_lightCount >= MaxLights) return;
        int count = _lightCount;
        if (light.Type == LightType.Directional)
        {
            DirectionalLight = light;
        }
        foreach (var pair in Shaders)
        {
            var shader = pair.Value;
            var lightLocs = new LightLocations
            {
                EnabledLoc = GetShaderLocation(shader, $"lights[{count}].enabled"),
                TypeLoc = GetShaderLocation(shader, $"lights[{count}].type"),
                PositionLoc = GetShaderLocation(shader, $"lights[{count}].position"),
                TargetLoc = GetShaderLocation(shader, $"lights[{count}].target"),
                ColorLoc = GetShaderLocation(shader, $"lights[{count}].color")
            };
            _shaderLocMap[shader].Add(light, lightLocs);
            SetShaderValue(shader, lightLocs.EnabledLoc, light.Enabled ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(shader, lightLocs.TypeLoc, (int)light.Type, ShaderUniformDataType.Int);
            SetShaderValue(shader, lightLocs.PositionLoc, light.Position, ShaderUniformDataType.Vec3);
            SetShaderValue(shader, lightLocs.TargetLoc, light.Target, ShaderUniformDataType.Vec3);
            SetShaderValue(shader, lightLocs.ColorLoc, new Vector4(light.Color.R / 255f, light.Color.G / 255f,
                light.Color.B / 255f, light.Color.A / 255f), ShaderUniformDataType.Vec4);
        }

        _lightCount++;
    }

    public void RemoveLight(Light light)
    {
        foreach (var pair in Shaders)
        {
            var shader = pair.Value;
            SetShaderValue(shader, _shaderLocMap[shader][light].EnabledLoc, 0, ShaderUniformDataType.Int);
            _shaderLocMap[shader].Remove(light);
        }

        _lightCount--;
    }

    public void Dispose()
    {
        //cleanup
        foreach (var keyValuePair in Shaders)
        {
            UnloadShader(keyValuePair.Value);
        }
    }

    public void OnUpdate()
    {
        foreach (var pair in Shaders)
        {
            var shader = pair.Value;
            SetShaderValue(shader, shader.Locs[(int)ShaderLocationIndex.VectorView], Engine.Camera.Position,
                ShaderUniformDataType.Vec3);
        }
    }

    public void OnImGui()
    {
        if (ImGui.CollapsingHeader("Shader Settings"))
        {
            if (ImGui.SliderFloat("Environ. Light Intensity", ref _envLightIntensity, 0.0F, 20.0F))
            {
                foreach (var keyValuePair in Shaders)
                {
                    var shader = keyValuePair.Value;
                    SetShaderValue(
                        shader,
                        GetShaderLocation(shader, "envLightIntensity"),
                        _envLightIntensity,
                        ShaderUniformDataType.Float
                    );
                }
            }
            
            if (ImGui.SliderFloat("Fog Density", ref _fogDensity, 0.0F, 0.25f))
            {
                foreach (var keyValuePair in Shaders)
                {
                    var shader = keyValuePair.Value;
                    SetShaderValue(
                        shader,
                        GetShaderLocation(shader, "fogDensity"),
                        _fogDensity,
                        ShaderUniformDataType.Float
                    );
                }
            }

            if (ImGui.ColorPicker3("Fog Color", ref _fogColor))
            {
                foreach (var keyValuePair in Shaders)
                {
                    var shader = keyValuePair.Value;
                    SetShaderValue(
                        shader,
                        GetShaderLocation(shader, "fogColor"),
                        _fogColor,
                        ShaderUniformDataType.Vec3
                    );
                }
            }

            var light = DirectionalLight;
            if (ImGui.DragFloat3("Light Position", ref light.Position, 0.05f))
            {
                RemoveLight(DirectionalLight);
                AddLight(light);
            }
            if (ImGui.DragFloat3("Light Target", ref light.Target, 0.05f))
            {
                RemoveLight(DirectionalLight);
                AddLight(light);
            }
        }
    }

    public void SetupModelMaterials(ref Model model, ShaderType type)
    {
        for (int i = 0; i < model.MeshCount; i++)
        {
            if (model.Meshes[i].Tangents == null)
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"WARNING: NO TANGENTS ON MESH {model}");
                model.Meshes[i].AllocTangents();
                GenMeshTangents(ref model.Meshes[i]);
                Console.ForegroundColor = prevColor;
            }
        }
        for (int i = 0; i < model.MaterialCount; i++)
        {
            Material mat = LoadMaterialDefault();
            mat.Shader = Engine.ShaderManager[type];

            mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = Skybox.GetEnvironment();
            mat.Maps[(int)MaterialMapIndex.Irradiance].Texture = Skybox.GetIrradiance();
            mat.Maps[(int)MaterialMapIndex.Prefilter].Texture = Skybox.GetPrefilter();
            mat.Maps[(int)MaterialMapIndex.Brdf].Texture = Skybox.GetBrdf();
            
            // TODO: Account for when no texture is loaded in slot
            mat.Maps[(int)MaterialMapIndex.Albedo].Texture =
                model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture;
            mat.Maps[(int)MaterialMapIndex.Albedo].Texture.Mipmaps = 4;
            GenTextureMipmaps(ref mat.Maps[(int)MaterialMapIndex.Albedo].Texture);
            SetTextureFilter(
                mat.Maps[(int)MaterialMapIndex.Albedo].Texture,
                TextureFilter.Bilinear
            );

            if (model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture.Id != 0)
            {
                mat.Maps[(int)MaterialMapIndex.Normal].Texture =
                    model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture;
            }
            else
            {
                Image blankNormal = GenImageColor(1, 1, new Color(128, 128, 255));
                mat.Maps[(int)MaterialMapIndex.Normal].Texture = 
                    LoadTextureFromImage(blankNormal);
                UnloadImage(blankNormal);
            }

            if (model.Materials[i].Maps[(int) MaterialMapIndex.Roughness].Texture.Id != 0)
            {
                mat.Maps[(int)MaterialMapIndex.Roughness].Texture =
                    model.Materials[i].Maps[(int) MaterialMapIndex.Roughness].Texture;
            }
            else
            {
                Image blankRoughness = GenImageColor(1, 1, new Color(255, 216, 0));
                mat.Maps[(int)MaterialMapIndex.Roughness].Texture =
                    LoadTextureFromImage(blankRoughness);
                UnloadImage(blankRoughness);
            }


            model.Materials[i] = mat;
        }
    }

    public Shader this[ShaderType type] => Shaders[type];
}