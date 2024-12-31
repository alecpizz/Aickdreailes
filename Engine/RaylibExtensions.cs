using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs.BleedingEdge;
using Raylib_cs.BleedingEdge.Interop;
using OpenTK.Graphics.OpenGL;

using RL = Raylib_cs.BleedingEdge;

namespace Engine;

public static class RaylibExtensions
{
    public static Matrix4x4 Rotate(Quaternion q)
    {
        return Raymath.QuaternionToMatrix(q);
    }

    public static Matrix4x4 Translate(Vector3 v)
    {
        return Raymath.MatrixTranslate(v.X, v.Y, v.Z);
    }

    public static Matrix4x4 Scale(Vector3 s)
    {
        return Raymath.MatrixScale(s.X, s.Y, s.Z);
    }

    public static Matrix4x4 TRS(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        var result = Raymath.MatrixIdentity();
        result *= Translate(translation);
        result *= Rotate(rotation);
        result *= Scale(scale);
        return result;
    }

    public static Matrix4x4 TRS(Transform transform)
    {
        return TRS(transform.Translation, transform.Rotation, transform.Scale);
    }

    public static Matrix4x4 LocalToWorld(Matrix4x4 parent, Matrix4x4 local)
    {
        return parent * local;
    }

    public static Matrix4x4 WorldToLocal(Matrix4x4 parent, Matrix4x4 local)
    {
        return Raymath.MatrixInvert(parent) * local;
    }

    public static unsafe Texture2D GenTextureCubemap(
        Shader shader,
        string panoramaPath,
        int size,
        RL.PixelFormat format)
    {
        // Flip panorama texture
        Image panoramaImage = Raylib.LoadImage(panoramaPath);
        Raylib.ImageFlipVertical(&panoramaImage);
        
        // Load flipped panorama texture
        Texture2D panorama = Raylib.LoadTextureFromImage(
            panoramaImage
        );
        
        // Gen blank cubemap
        Image blankCubemap = Raylib.GenImageColor(size, size * 6, Color.Pink);
        Texture2D cubemap = Raylib.LoadTextureCubemap(
            blankCubemap,
            CubemapLayout.LineVertical
        );
        cubemap.Format = format;
        
        Rlgl.DisableBackfaceCulling();
        
        // Setup framebuffer
        uint rbo = Rlgl.LoadTextureDepth(size, size, true);
        
        uint fbo = Rlgl.LoadFramebuffer();
        Rlgl.FramebufferAttach(
            fbo,
            rbo,
            FramebufferAttachType.Depth,
            FramebufferAttachTextureType.RenderBuffer,
            0
        );
        Rlgl.FramebufferAttach(
            fbo,
            cubemap.Id,
            FramebufferAttachType.ColorChannel0,
            FramebufferAttachTextureType.CubemapPositiveX,
            0
        );
        
        // Verify framebuffer attachment
        Rlgl.FramebufferComplete(fbo);
        
        // Draw to framebuffer
        Rlgl.EnableShader(shader.Id);

        // Define/setup projection matrix
        Matrix4x4 matFboProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(90.0F),
            1.0F,
            0.1F,
            1000.0F
        );
        Rlgl.SetUniformMatrix(
            Raylib.GetShaderLocation(
                shader,
                "matProjection"),
            Matrix4x4.Transpose(matFboProjection)
        );
        
        // Define/setup view matrix list
        Matrix4x4[] fboViews =
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
        
        Rlgl.Viewport(0, 0, size, size);
        
        // Render cubemap to texture
        Rlgl.ActiveTextureSlot(0);
        Rlgl.EnableTexture(panorama.Id);

        for (int i = 0; i < 6; i++)
        {
            Rlgl.SetUniformMatrix(
                Raylib.GetShaderLocation(
                    shader,
                    "matView"),
                Matrix4x4.Transpose(fboViews[i])
            );

            var attachLoc = (FramebufferAttachTextureType) (
                (int)FramebufferAttachTextureType.CubemapPositiveX + i
            );
            
            Rlgl.FramebufferAttach(
                fbo,
                cubemap.Id,
                FramebufferAttachType.ColorChannel0,
                attachLoc,
                0
            );
            Rlgl.EnableFramebuffer(fbo);
            
            Rlgl.ClearScreenBuffers();
            Rlgl.LoadDrawCube();
        }

        // Clean up resources
        Rlgl.DisableShader();
        Rlgl.DisableTexture();
        Rlgl.DisableFramebuffer();
        Rlgl.UnloadFramebuffer(fbo);
        
        Raylib.UnloadImage(panoramaImage);
        
        // Reset viewport state
        Rlgl.Viewport(
            0, 
            0,
            Rlgl.GetFramebufferWidth(),
            Rlgl.GetFramebufferHeight()
        );
        Rlgl.EnableBackfaceCulling();

        cubemap.Width = size;
        cubemap.Height = size;
        cubemap.Mipmaps = 1;
        cubemap.Format = format;

        return cubemap;
    }
    
    public static unsafe Texture2D GenTextureIrradiance(
        Shader shader,
        Texture2D environmentMap,
        int size,
        RL.PixelFormat format)
    {
        // Gen blank cubemap
        Image blankCubemap = Raylib.GenImageColor(size, size * 6, Color.Pink);
        Texture2D cubemap = Raylib.LoadTextureCubemap(
            blankCubemap,
            CubemapLayout.LineVertical
        );
        cubemap.Format = format;
        
        Rlgl.DisableBackfaceCulling();
        
        // Setup framebuffer
        uint rbo = Rlgl.LoadTextureDepth(size, size, true);
        
        uint fbo = Rlgl.LoadFramebuffer();
        Rlgl.FramebufferAttach(
            fbo,
            rbo,
            FramebufferAttachType.Depth,
            FramebufferAttachTextureType.RenderBuffer,
            0
        );
        Rlgl.FramebufferAttach(
            fbo,
            cubemap.Id,
            FramebufferAttachType.ColorChannel0,
            FramebufferAttachTextureType.CubemapPositiveX,
            0
        );
        
        // Verify framebuffer attachment
        Rlgl.FramebufferComplete(fbo);
        
        // Draw to framebuffer
        Rlgl.EnableShader(shader.Id);

        // Define/setup projection matrix
        Matrix4x4 matFboProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(90.0F),
            1.0F,
            0.1F,
            1000.0F
        );
        Rlgl.SetUniformMatrix(
            Raylib.GetShaderLocation(
                shader,
                "matProjection"),
            Matrix4x4.Transpose(matFboProjection)
        );
        
        // Define/setup view matrix list
        Matrix4x4[] fboViews =
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
        
        Rlgl.Viewport(0, 0, size, size);
        
        // Render cubemap to texture
        Rlgl.ActiveTextureSlot(0);
        Rlgl.EnableTextureCubemap(environmentMap.Id);

        for (int i = 0; i < 6; i++)
        {
            Rlgl.SetUniformMatrix(
                Raylib.GetShaderLocation(
                    shader,
                    "matView"),
                Matrix4x4.Transpose(fboViews[i])
            );

            var attachLoc = (FramebufferAttachTextureType) (
                (int)FramebufferAttachTextureType.CubemapPositiveX + i
            );
            
            Rlgl.FramebufferAttach(
                fbo,
                cubemap.Id,
                FramebufferAttachType.ColorChannel0,
                attachLoc,
                0
            );
            Rlgl.EnableFramebuffer(fbo);
            
            Rlgl.ClearScreenBuffers();
            Rlgl.LoadDrawCube();
        }

        // Clean up resources
        Rlgl.DisableShader();
        Rlgl.DisableTexture();
        Rlgl.DisableFramebuffer();
        Rlgl.UnloadFramebuffer(fbo);
        
        // Reset viewport state
        Rlgl.Viewport(
            0, 
            0,
            Rlgl.GetFramebufferWidth(),
            Rlgl.GetFramebufferHeight()
        );
        Rlgl.EnableBackfaceCulling();

        cubemap.Width = size;
        cubemap.Height = size;
        cubemap.Mipmaps = 1;
        cubemap.Format = format;

        return cubemap;
    }
    
    private static readonly int PrefilterMipLevels = 5; 
    
    public static unsafe Texture2D GenTexturePrefilter(
        Shader shader,
        Texture2D environmentMap,
        int size,
        RL.PixelFormat format)
    {
        // Gen blank cubemap
        Image blankCubemap = Raylib.GenImageColor(size, size * 6, Color.Pink);
        Texture2D cubemap = Raylib.LoadTextureCubemap(
            blankCubemap,
            CubemapLayout.LineVertical
        );
        cubemap.Mipmaps = PrefilterMipLevels;
        cubemap.Format = format;
        
        GL.BindTexture(TextureTarget.TextureCubeMap, (int)cubemap.Id);
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
            (int)TextureMinFilter.LinearMipmapLinear
        );
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear
        );
        GL.GenerateMipmap(TextureTarget.TextureCubeMap);
        
        Rlgl.DisableBackfaceCulling();
        
        // Save current renderbuffer params
        int fbWidth = Rlgl.GetFramebufferWidth();
        int fbHeight = Rlgl.GetFramebufferHeight();
        
        // Setup framebuffer
        uint rbo = Rlgl.LoadTextureDepth(size, size, true);
        uint fbo = Rlgl.LoadFramebuffer();
        Rlgl.FramebufferAttach(
            fbo,
            rbo,
            FramebufferAttachType.Depth,
            FramebufferAttachTextureType.RenderBuffer,
            0
        );
        Rlgl.FramebufferAttach(
            fbo,
            cubemap.Id,
            FramebufferAttachType.ColorChannel0,
            FramebufferAttachTextureType.CubemapPositiveX,
            0
        );
        
        // Verify framebuffer attachment
        Rlgl.FramebufferComplete(fbo);
        
        // Draw to framebuffer
        Rlgl.EnableShader(shader.Id);

        // Define/setup projection matrix
        Matrix4x4 matFboProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(90.0F),
            1.0F,
            0.1F,
            1000.0F
        );
        Rlgl.SetUniformMatrix(
            Raylib.GetShaderLocation(
                shader,
                "matProjection"),
            Matrix4x4.Transpose(matFboProjection)
        );
        
        // Define/setup view matrix list
        Matrix4x4[] fboViews =
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
        
        Rlgl.Viewport(0, 0, size, size);
        
        // Render cubemap to texture
        Rlgl.ActiveTextureSlot(0);
        Rlgl.EnableTextureCubemap(environmentMap.Id);

        for (int mip = 0; mip < PrefilterMipLevels; mip++)
        {
            int mipSize = (int) (size * float.Pow(0.5F, mip));
            
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, (int)rbo);
            GL.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.DepthComponent24,
                mipSize,
                mipSize
            );
            Rlgl.Viewport(0, 0, mipSize, mipSize);

            float roughness = (float)mip / (float)(PrefilterMipLevels - 1);
            Rlgl.SetUniform(
                Raylib.GetShaderLocation(
                    shader,
                    "roughness"),
                roughness,
                ShaderUniformDataType.Float,
                1
            );
            
            for (int i = 0; i < 6; i++)
            {
                Rlgl.SetUniformMatrix(
                    Raylib.GetShaderLocation(
                        shader,
                        "matView"),
                    Matrix4x4.Transpose(fboViews[i])
                );

                var attachLoc = (FramebufferAttachTextureType) (
                    (int) FramebufferAttachTextureType.CubemapPositiveX + i
                );

                Rlgl.FramebufferAttach(
                    fbo,
                    cubemap.Id,
                    FramebufferAttachType.ColorChannel0,
                    attachLoc,
                    mip
                );
                Rlgl.EnableFramebuffer(fbo);

                Rlgl.ClearScreenBuffers();
                Rlgl.LoadDrawCube();
            }
        }

        // Clean up resources
        Rlgl.DisableShader();
        Rlgl.DisableTexture();
        Rlgl.DisableFramebuffer();
        Rlgl.UnloadFramebuffer(fbo);
        
        // Reset viewport state
        Rlgl.Viewport(
            0, 
            0,
            Rlgl.GetFramebufferWidth(),
            Rlgl.GetFramebufferHeight()
        );
        Rlgl.EnableBackfaceCulling();

        cubemap.Width = size;
        cubemap.Height = size;
        cubemap.Mipmaps = PrefilterMipLevels;
        cubemap.Format = format;

        return cubemap;
    }

    private static readonly RL.PixelFormat BrdfPixFormat = RL.PixelFormat.UncompressedR32G32B32A32;

    public static Texture2D GenTextureBRDF(
        Shader shader,
        int size)
    {
        Image blankBRDF = Raylib.GenImageColor(size, size, Color.White);
        Texture2D brdf = Raylib.LoadTextureFromImage(blankBRDF);
        brdf.Format = BrdfPixFormat;

        // Setup framebuffer
        uint rbo = Rlgl.LoadTextureDepth(size, size, true);
        uint fbo = Rlgl.LoadFramebuffer();
        Rlgl.FramebufferAttach(
            fbo,
            rbo,
            FramebufferAttachType.Depth,
            FramebufferAttachTextureType.RenderBuffer,
            0
        );
        Rlgl.FramebufferAttach(
            fbo,
            brdf.Id,
            FramebufferAttachType.ColorChannel0,
            FramebufferAttachTextureType.Texture2D,
            0
        );
        
        // Verify framebuffer attachment
        Rlgl.FramebufferComplete(fbo);
        
        // Draw to framebuffer
        Rlgl.EnableShader(shader.Id);
        
        // Set viewport
        Rlgl.Viewport(0, 0, size, size);
        
        // Render cubemap to texture
        Rlgl.ActiveTextureSlot(0);
        Rlgl.EnableTexture(brdf.Id);
        
        // Render
        Rlgl.EnableFramebuffer(fbo);
        Rlgl.ClearScreenBuffers();
        Rlgl.LoadDrawQuad();
        
        // Clean up resources
        Rlgl.DisableShader();
        Rlgl.DisableTexture();
        Rlgl.DisableFramebuffer();
        Rlgl.UnloadFramebuffer(fbo);
        
        // Reset viewport state
        Rlgl.Viewport(
            0, 
            0,
            Rlgl.GetFramebufferWidth(),
            Rlgl.GetFramebufferHeight()
        );
        Rlgl.EnableBackfaceCulling();
        
        brdf.Width = size;
        brdf.Height = size;
        brdf.Mipmaps = 1;
        brdf.Format = BrdfPixFormat;

        return brdf;
    }
}