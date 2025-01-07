﻿using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine.Entities;

public class SkyboxEntity : Entity
{
    private Shader _skyShader;
    private Texture2D _cubeMap;
    private Model _cube;
    private static readonly string VertexPath = Path.Combine("Resources","Shaders","skybox.vert");
    private static readonly string FragmentPath = Path.Combine("Resources","Shaders","skybox.frag");

    public SkyboxEntity(string cubeMap) : base(cubeMap)
    {
        _skyShader = LoadShader(VertexPath, FragmentPath);
        Mesh cube = GenMeshCube(1.0f, 1.0f, 1.0f);
        _cube = LoadModelFromMesh(cube);
        SetShaderValue(_skyShader, GetShaderLocation(_skyShader, "environmentMap"),
            (int)MaterialMapIndex.Cubemap, ShaderUniformDataType.Int);
        SetMaterialShader(ref _cube, 0, ref _skyShader);
        var texture = LoadImage(cubeMap);
        _cubeMap = LoadTextureCubemap(texture, CubemapLayout.AutoDetect);
        UnloadImage(texture);
        unsafe
        {
            SetMaterialTexture(_cube.Materials, MaterialMapIndex.Cubemap, _cubeMap);
        }
    }

    public override void OnRender(Shader? shader = null)
    {
        Rlgl.DisableBackfaceCulling();
        Rlgl.DisableDepthMask();
        DrawModel(_cube, Vector3.Zero, 1.0f, Color.White);
        Rlgl.EnableBackfaceCulling();
        Rlgl.EnableDepthMask();
    }

    public override void OnCleanup()
    {
        UnloadTexture(_cubeMap);
        UnloadShader(_skyShader);
    }
}