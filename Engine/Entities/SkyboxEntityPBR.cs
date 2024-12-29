﻿using System.Numerics;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;
using PixelFormat = Raylib_cs.BleedingEdge.PixelFormat;

namespace Engine.Entities;

public unsafe class SkyboxEntityPBR : Entity
{
    private Shader _cubeShader;
    private Shader _skyboxShader;
    private Texture2D _cubeMap;
    private Model _cube;

    private static readonly string CubemapPathVert = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "cubemap.vert"
    );
    private static readonly string CubemapPathFrag = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "cubemap.frag"
    );
    private static readonly string SkyboxVert = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "skybox.vert"
    );
    private static readonly string SkyboxFrag = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "skybox.frag"
    );

    public SkyboxEntityPBR(string panorama) : base(panorama)
    {
        // Gen cubemap capture mesh
        Mesh cube = GenMeshCube(1.0F, 1.0F, 1.0F);
        _cube = LoadModelFromMesh(cube);
        
        // Init cubemap skybox shader
        Shader cubemapShader = LoadShader(CubemapPathVert, CubemapPathFrag);
        cubemapShader.Locs[(int)ShaderLocationIndex.MapCubemap] = GetShaderLocation(
            cubemapShader,
            "equirectangularMap"
        );
        
        // Load environment map texture
        _cubeMap = RaylibExtensions.GenTextureCubemap(
            cubemapShader,
            panorama,
            512,
            PixelFormat.UncompressedR32G32B32A32
        );
        
        // Load skybox shader
        Shader skyboxShader = LoadShader(SkyboxVert, SkyboxFrag);
        skyboxShader.Locs[(int)ShaderLocationIndex.MapCubemap] = GetShaderLocation(
            skyboxShader,
            "environmentMap"
        );

        // Setup cube to draw with the skybox shader/texture
        Material mat = LoadMaterialDefault();
        mat.Shader = skyboxShader;
        mat.Maps[(int)MaterialMapIndex.Cubemap].Texture = _cubeMap;

        for (int i = 0; i < _cube.MaterialCount; i++)
        {
            _cube.Materials[i] = mat;
        }
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