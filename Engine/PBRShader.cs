using System.Numerics;
using Raylib_cs.BleedingEdge;
using rlImGui_cs;
using static Raylib_cs.BleedingEdge.Raylib;
using OpenTK.Graphics.OpenGL;
using InternalFormat = OpenTK.Graphics.OpenGLES2.InternalFormat;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Engine;

public unsafe class PBRShader
{
    private readonly int CUBEMAP_SIZE = 1024;
    private readonly int IRRADIANCE_SIZE = 32; 
    
    public static Material InitPBRMaterial(
        Texture2D albedo,
        Texture2D normal,
        Texture2D roughness,
        TextureFilter filterMode,
        Entities.SkyboxEntityPBR skybox)
    {
        // Init shader
        Shader shader = LoadShader(
            "Resources/Shaders/pbr.vert",
            "Resources/Shaders/pbr.frag"
        );
        
        // Setup lighting
        Light.CreateLight(
            LightType.Directional,
            Vector3.Zero,
            new Vector3(1.0F, 1.0F, -2.0F),
            Color.White,
            shader
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
            "roughnessMap"
        );
        shader.Locs[(int)ShaderLocationIndex.VectorView] = GetShaderLocation(
            shader,
            "viewPos"
        );
        shader.Locs[(int)ShaderLocationIndex.MapCubemap] = GetShaderLocation(
            shader,
            "environmentMap"
        );
        shader.Locs[(int)ShaderLocationIndex.MapIrradiance] = GetShaderLocation(
            shader,
            "irradianceMap"
        );
        
        // Init material
        Material mat = LoadMaterialDefault();
        mat.Shader = shader;

        mat.Maps[(int)MaterialMapIndex.Albedo].Texture = albedo;
        mat.Maps[(int)MaterialMapIndex.Normal].Texture = normal;
        mat.Maps[(int)MaterialMapIndex.Roughness].Texture = roughness;

        Texture2D environmentMap = skybox.GetSkyboxTexture();
        Texture2D irradianceMap = environmentMap;
        
        irradianceMap.Width = 32;
        irradianceMap.Height = 32;
        
        mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = environmentMap;
        mat.Maps[(int)MaterialMapIndex.Irradiance].Texture = irradianceMap;
        
        SetTextureFilter(mat.Maps[(int)MaterialMapIndex.Albedo].Texture, filterMode);
        SetTextureFilter(mat.Maps[(int)MaterialMapIndex.Normal].Texture, filterMode);
        SetTextureFilter(mat.Maps[(int)MaterialMapIndex.Roughness].Texture, filterMode);
        
        GenTextureMipmaps(&mat.Maps[(int)MaterialMapIndex.Albedo].Texture);
        GenTextureMipmaps(&mat.Maps[(int)MaterialMapIndex.Normal].Texture);
        GenTextureMipmaps(&mat.Maps[(int)MaterialMapIndex.Roughness].Texture);
        
        //LoadEnvironmentMap(ref mat);

        return mat;
    }

    private static void LoadEnvironmentMap(int cubemapSize)
    {
        Shader cubeShader = LoadShader(
            "Resources/Shaders/PBRIncludes/cubemap.vert",
            "Resources/Shaders/PBRIncludes/cubemap.frag"
        );

        

        int hdrTexture;

        
        
        // Render test cube
        
        
        // Create projection
        Matrix4x4 captureProj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            90.0F,
            1.0F,
            0.01F,
            1000.0F
        );
        captureProj = Matrix4x4.Transpose(captureProj);
        Matrix4x4[] captureViews =
        [
            Matrix4x4.CreateLookAt(
                new Vector3(0.0F, 0.0F, 0.0F),
                new Vector3(1.0F, 0.0F, 0.0F),
                new Vector3(0.0F, -1.0F, 0.0F)),
            Matrix4x4.CreateLookAt(
                new Vector3(0.0F, 0.0F, 0.0F),
                new Vector3(-1.0F, 0.0F, 0.0F),
                new Vector3(0.0F, -1.0F, 0.0F)),
            Matrix4x4.CreateLookAt(
                new Vector3(0.0F, 0.0F, 0.0F),
                new Vector3(0.0F, 1.0F, 0.0F),
                new Vector3(0.0F, 0.0F, 1.0F)),
            Matrix4x4.CreateLookAt(
                new Vector3(0.0F, 0.0F, 0.0F),
                new Vector3(0.0F, -1.0F, 0.0F),
                new Vector3(0.0F, 0.0F, -1.0F)),
            Matrix4x4.CreateLookAt(
                new Vector3(0.0F, 0.0F, 0.0F),
                new Vector3(0.0F, 0.0F, 1.0F),
                new Vector3(0.0F, -1.0F, 0.0F)),
            Matrix4x4.CreateLookAt(
                new Vector3(0.0F, 0.0F, 0.0F),
                new Vector3(0.0F, 0.0F, -1.0F),
                new Vector3(0.0F, -1.0F, 0.0F))
        ];
    }
}