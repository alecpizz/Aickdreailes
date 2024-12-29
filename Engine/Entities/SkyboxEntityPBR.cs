using System.Numerics;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;
using PixelFormat = Raylib_cs.BleedingEdge.PixelFormat;

namespace Engine.Entities;

public unsafe class SkyboxEntityPBR : Entity
{
    private Shader _cubeShader;
    private Shader _skyboxShader;
    private Texture2D _cubeMap;
    private Texture2D _convolvedCubeMap;
    private Model _cube;

    private static readonly string IrradiancePathVert = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "cubemap.vert"
    );
    private static readonly string IrradiancePathFrag = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "irradiance.frag"
    );
    private static readonly string SkyboxVert = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "skybox.vert"
    );
    private static readonly string SkyboxFrag = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "skybox.frag"
    );

    public SkyboxEntityPBR(string cubeMap) : base(cubeMap)
    {
        // Gen cubemap capture mesh
        Mesh cube = GenMeshCube(1.0F, 1.0F, 1.0F);
        _cube = LoadModelFromMesh(cube);
        
        // Load environment map texture
        Image skyImage = LoadImageRaw(cubeMap);
        _cubeMap = LoadTextureCubemap(skyImage, CubemapLayout.AutoDetect);
        
        // Cubemap convolution for irradiance
        ImageResize(
            &skyImage,
            skyImage.Width  / 4,
            skyImage.Height / 4
        );
        ImageBlurGaussian(&skyImage, 2);
        _convolvedCubeMap = LoadTextureCubemap(
            skyImage, 
            CubemapLayout.AutoDetect
        );

        // Setup skybox shader
        _skyboxShader = LoadShader(SkyboxVert, SkyboxFrag);
        _skyboxShader.Locs[(int)ShaderLocationIndex.MapCubemap] = GetShaderLocation(
            _skyboxShader,
            "environmentMap"
        );
        
        // Setup/use skybox material
        Material mat = LoadMaterialDefault();
        mat.Shader = _skyboxShader;
        mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = _convolvedCubeMap;
        
        SetTextureFilter(
            mat.Maps[(int)MaterialMapIndex.Cubemap].Texture,
            TextureFilter.Trilinear
        );

        mat.Maps[(int)MaterialMapIndex.Cubemap].Texture.Mipmaps = 4;
        GenTextureMipmaps(&mat.Maps[(int)MaterialMapIndex.Cubemap].Texture);
        
        for (int i = 0; i < _cube.MaterialCount; i++)
        {
            _cube.Materials[i] = mat;
        }
        
        // Test rendering cubemap
        
    }

    public Texture2D GetSkyboxTexture()
    {
        return _cubeMap;
    }

    public override void OnRender()
    {
        Rlgl.DisableBackfaceCulling();
        Rlgl.DisableDepthMask();
        DrawModel(_cube, Vector3.Zero, 100.0F, Color.White);
        Rlgl.EnableBackfaceCulling();
        Rlgl.EnableDepthMask();
    }

    public override void OnCleanup()
    {
        UnloadTexture(_cubeMap);
        UnloadShader(_cubeShader);
        UnloadModel(_cube);
    }
}