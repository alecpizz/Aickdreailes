using System.Numerics;
using Jitter2;
using Jitter2.LinearMath;
using Raylib_cs;

namespace Engine;

public class PhysDrawer : IDebugDrawer
{
    
    public void DrawSegment(in JVector pA, in JVector pB)
    {
        Raylib.DrawLine3D(new Vector3(pA.X, pA.Y, pA.Z), new Vector3(pB.X, pB.Y, pB.Z), Color.White);
    }

    public void DrawTriangle(in JVector pA, in JVector pB, in JVector pC)
    {
        Vector3 a = new Vector3(pA.X, pA.Y, pA.Z);
        Vector3 b = new Vector3(pB.X, pB.Y, pB.Z);
        Vector3 c = new Vector3(pC.X, pC.Y, pC.Z);
        Raylib.DrawLine3D(a, b, Color.White);
        Raylib.DrawLine3D(b, c, Color.White);
        Raylib.DrawLine3D(c, a, Color.White);
    }

    public void DrawPoint(in JVector p)
    {
        Vector3 pV = new Vector3(p.X, p.Y, p.Z);
        Raylib.DrawPoint3D(pV, Color.White);
    }
}