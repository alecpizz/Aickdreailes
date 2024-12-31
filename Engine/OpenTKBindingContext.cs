using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;
namespace Engine;

public interface IGlContext
{
    nint GetProcAddress(string procName);
}

public class OpenTKBindingContext : IBindingsContext, IGlContext, IDisposable
{
    private IGlContext _context;
    
    public bool HasDisposed { get; private set; }

    public OpenTKBindingContext()
    {
        // TODO: Add Mac/Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _context = new WinGlContext();
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Platform is not supported!"
            );
        }
    }
    
    public IntPtr GetProcAddress(string procName)
    {
        return _context.GetProcAddress(procName);
    }

    public void Dispose() 
    {
        if (this.HasDisposed) return;

        Dispose(true);
        GC.SuppressFinalize(this);
        HasDisposed = true;
    }
    
    protected virtual void Dispose(bool disposing) 
    {
        if (disposing) 
        {
            if (RuntimeInformation.IsOSPlatform(
                    OSPlatform.Windows)) 
            {
                ((WinGlContext)_context).Dispose();
            }
        }
    }
}

public partial class WinGlContext : IGlContext
{
    private const string OpenGL32 = "opengl32";
    private const string Kernel32 = "kernel32";
    
    public bool HasDisposed { get; private set; }
    
    private nint _glHandle;

    public WinGlContext()
    {
        _glHandle = LoadLibrary(OpenGL32);
    }
    
    public IntPtr GetProcAddress(string procName)
    {
        nint wglAddress = GetWGLProcAddress(procName);

        if (wglAddress == nint.Zero) 
        {
            nint procAddress = GetProcAddress(
                _glHandle, 
                procName
            );

            if (procAddress == nint.Zero) 
            {
                throw new Exception(
                    "Failed to retrieve the Procedure Address."
                );
            }

            return procAddress;
        }

        return wglAddress;
    }
    
    [LibraryImport(OpenGL32, EntryPoint = "wglGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetWGLProcAddress(string procName);
    
    [LibraryImport(Kernel32, EntryPoint = "LoadLibraryA", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint LoadLibrary(string fileName);
    
    [LibraryImport(Kernel32, EntryPoint = "GetProcAddress", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetProcAddress(nint module, string procName);
    
    [LibraryImport(Kernel32, EntryPoint = "FreeLibrary", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeLibrary(nint module);

    public void Dispose()
    {
        if (HasDisposed)
        {
            return;
        }
        
        Dispose(true);
        GC.SuppressFinalize(this);
        HasDisposed = true;
    }
    
    protected virtual void Dispose(bool disposing) 
    {
        if (disposing) 
        {
            FreeLibrary(_glHandle);
        }
    }
}