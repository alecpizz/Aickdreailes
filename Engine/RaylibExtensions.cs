using System.Numerics;
using Raylib_cs.BleedingEdge;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;
using static StbImageSharp.ImageResultFloat;

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

    public static unsafe void LoadEquirecCubemap(string path, int cubemapSize)
    {
        // Setup framebuffer
        int captureFbo, captureRbo;
        GL.GenFramebuffers(
            1,
            &captureFbo
        );
        GL.GenRenderbuffers(
            1,
            &captureRbo
        );
        
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
            cubemapSize,
            cubemapSize
        );
        GL.FramebufferRenderbuffer(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer,
            captureRbo
        );
        
        // Load environment map
        using var stream = File.OpenRead(path);
        
        ImageResultFloat image = FromStream(stream);

        if (image.Data.Length == 0)
        {
            throw new Exception("Failed to load image from path: " +
                Path.GetFileName(path)
            );
        }
        
        // Send loaded image to the GPU
        int imageLoc;
        
        GL.GenTextures(
            1,
            &imageLoc
        );
        GL.BindTexture(
            TextureTarget.Texture2d,
            imageLoc
        );
        GL.TexImage2D(
            TextureTarget.Texture2d,
            0,
            InternalFormat.Rgb16f,
            image.Width,
            image.Height,
            0,
            OpenTK.Graphics.OpenGL.PixelFormat.Rgb,
            PixelType.Float,
            image.DataPtr
        );
        
        GL.TexParameteri(
            TextureTarget.Texture2d,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge
        );
        GL.TexParameteri(
            TextureTarget.Texture2d,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge
        );
        GL.TexParameteri(
            TextureTarget.Texture2d,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear
        );
        GL.TexParameteri(
            TextureTarget.Texture2d,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear
        );
        
        // Setup cubemap rendering
        int envCubemap;
        
        GL.GenTextures(
            1,
            &envCubemap
        );
        GL.BindTexture(
            TextureTarget.TextureCubeMap,
            envCubemap
        );

        for (int i = 0; i < 6; i++)
        {
            TextureTarget target = (TextureTarget)(i + 
                (int) TextureTarget.TextureCubeMapPositiveX);
            
            GL.TexImage2D(
                target,
                0,
                InternalFormat.Rgb16f,
                cubemapSize,
                cubemapSize,
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
            TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge
        );
        GL.TexParameteri(
            TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapT,
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
        
        // Setup capture projection/views
        Matrix4x4 captureProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(90.0F),
            1.0F,
            0.1F,
            1000.0F
        );
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

        Shader equiToCube = Raylib.LoadShader(
            Path.Combine("Resources", "Shaders", "PBRIncludes", "cubemap.vert"),
            Path.Combine("Resources", "Shaders", "PBRIncludes", "cubemap.frag")
        );
        Raylib.SetShaderValue(
            equiToCube,
            Raylib.GetShaderLocation(
                equiToCube,
                "equirectangularMap"),
            0,
            ShaderUniformDataType.Int
        );
        Raylib.SetShaderValueMatrix(
            equiToCube,
            Raylib.GetShaderLocation(
                equiToCube,
                "matProjection"),
            Matrix4x4.Transpose(
                captureProjection)
        );
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(
            TextureTarget.Texture2d,
            imageLoc
        );
        
        GL.Viewport(0, 0, cubemapSize, cubemapSize);
        GL.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            captureFbo
        );
        
        for (int i = 0; i < 6; i++)
        {
            Raylib.SetShaderValueMatrix(
                equiToCube,
                Raylib.GetShaderLocation(
                    equiToCube,
                    "matView"),
                Matrix4x4.Transpose(captureViews[i])
            );
            
            TextureTarget target = (TextureTarget)(i + 
                (int) TextureTarget.TextureCubeMapPositiveX);
            
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                target,
                envCubemap,
                0
            );
            
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            
            Raylib.BeginShaderMode(equiToCube);
                Raylib.DrawCube(
                    Vector3.Zero,
                    1.0F,
                    1.0F,
                    1.0F,
                    Color.White
                );
            Raylib.EndShaderMode();
        }
        
        GL.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            0
        );
    }
    
    public static Texture2D ApplyShaderToTexture(
        Texture2D input,
        Shader effect)
    {
        // Create a render texture to get output
        RenderTexture2D renderTex = Raylib.LoadRenderTexture(
            input.Width,
            input.Height
        );
        
        // Apply texture to shader
        // ...
        // IMPORTANT: All custom shaders must implement the
        // uniform "texInput" in order to receive texture data!
        Raylib.SetShaderValue(
            effect,
            Raylib.GetShaderLocation(
                effect,
                "texInput"),
            input.Id,
            ShaderUniformDataType.Int
        );
        
        // Apply the shader effect to the render texture
        Raylib.BeginTextureMode(renderTex);
            Raylib.ClearBackground(Color.Green);
            Raylib.BeginShaderMode(effect);
                Raylib.DrawRectangle(
                    0, 
                    0, 
                    input.Width,
                    input.Height,
                    Color.White
                );
            Raylib.EndShaderMode();
            Raylib.EndTextureMode();
        Raylib.EndTextureMode();
        
        // Return result
        return renderTex.Texture;
    }
}