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
    private Texture2D _equirecMap;
    private Model _cube;

    private const int CubemapSize = 512;

    private static readonly string CubePathVert = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "cubemap.vert"
    );
    private static readonly string CubePathFrag = Path.Combine(
        "Resources", "Shaders", "PBRIncludes", "cubemap.frag"
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
        
        // Capture cubemap
        SetupCaptureCube();
        int cubemapUnit = CaptureCubemap();

        // Setup skybox shader
        _skyboxShader = LoadShader(SkyboxVert, SkyboxFrag);
        SetShaderValue(
            _skyboxShader,
            GetShaderLocation(
                _skyboxShader,
                "environmentMap"),
            cubemapUnit,
            ShaderUniformDataType.Int
        );
        
        // Setup/use skybox material
        Material mat = LoadMaterialDefault();
        mat.Shader = _skyboxShader;
        
        for (int i = 0; i < _cube.MaterialCount; i++)
        {
            _cube.Materials[i] = mat;
        }
    }

    private void SetupCaptureCube()
    {
        // Load shader
        _cubeShader = LoadShader(CubePathVert, CubePathFrag);

        // Setup uniform locations
        _cubeShader.Locs[(int)ShaderLocationIndex.MapAlbedo] = GetShaderLocation(
            _cubeShader,
            "equirectangularMap"
        );
        
        // Create sky material
        Material mat = LoadMaterialDefault();
        mat.Shader = _cubeShader;
        
        // Load texture
        _equirecMap = LoadTexture("Resources/Textures/petit_port_2k.png");
        
        // Set texture in material
        mat.Maps[(int)MaterialMapIndex.Albedo].Texture = _equirecMap;
        
        // Set material on model
        for (int i = 0; i < _cube.MaterialCount; i++)
        {
            _cube.Materials[i] = mat;
        }
    }

    private int CaptureCubemap()
    {
        // Create custom render/framebuffers
        int captureFbo, captureRbo;
        GL.GenFramebuffers(1, &captureFbo);
        GL.GenRenderbuffers(1, &captureRbo);
        
        GL.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            captureFbo
        );
        GL.BindRenderbuffer(
            RenderbufferTarget.Renderbuffer,
            captureRbo
        );
        GL.RenderbufferStorage(
            RenderbufferTarget.Renderbuffer,
            InternalFormat.DepthComponent24,
            512,
            512
        );
        GL.FramebufferRenderbuffer(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer,
            captureRbo
        );
        
        // Allocate cubemap texture
        int envCubemap;
        GL.GenTextures(1, &envCubemap);
        GL.BindTexture(
            TextureTarget.TextureCubeMap,
            envCubemap
        );

        for (int i = 0; i < 6; i++)
        {
            TextureTarget texTarget = (TextureTarget)(i +
                (int)TextureTarget.TextureCubeMapPositiveX);
            
            GL.TexImage2D(
                texTarget,
                0,
                InternalFormat.Rgb16f,
                512,
                512,
                0,
                OpenTK.Graphics.OpenGL.PixelFormat.Rgb,
                PixelType.Float,
                (void*)0
            );
        }
        
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge
        );
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge
        );
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge
        );
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear
        );
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear
        );

        // Projection details for the capture
        Matrix4x4 captureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            90.0F * Deg2Rad,
            1.0F,
            0.1F,
            1000.0F
        );
        
        // Views to capture (= sides of the cubemap)
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
        
        // Send view projection to shader
        SetCubeProjectionMatrix(captureProjection);
        
        // Set viewport to proper dimensions (temporarily)
        GL.Viewport(0, 0, 512, 512);
        GL.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            captureFbo
        );
        
        // Render cubemap sides
        for (int i = 0; i < captureViews.Length; i++)
        {
            TextureTarget texTarget = (TextureTarget)(i +
                (int)TextureTarget.TextureCubeMapPositiveX);
            
            SetCubeViewMatrix(captureViews[i]);
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                texTarget,
                envCubemap,
                0
            );
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // Draw the cube!
            RenderCube();
        }
        
        // Re-bind original framebuffer
        GL.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            0
        );
        
        // Reset dimensions of viewport
        GL.Viewport(
            0, 
            0,
            GetScreenWidth(),
            GetScreenHeight()
        );
        
        // Return texture unit
        return envCubemap;
    }

    private void SetCubeProjectionMatrix(Matrix4x4 value)
    {
        SetShaderValueMatrix(
            _cubeShader,
            GetShaderLocation(
                _cubeShader,
                "matProjection"),
            value
        );
    }
    
    private void SetCubeViewMatrix(Matrix4x4 value)
    {
        SetShaderValueMatrix(
            _cubeShader,
            GetShaderLocation(
                _cubeShader,
                "matView"),
            value
        );
    }

    private void RenderCube()
    {
        DrawModel(_cube, Vector3.Zero, 1.0F, Color.White);
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
        UnloadTexture(_equirecMap);
        UnloadShader(_cubeShader);
        UnloadModel(_cube);
    }
}