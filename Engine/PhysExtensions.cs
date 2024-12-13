using System.Numerics;
using Jitter2.LinearMath;

namespace Engine;

public static class PhysExtensions
{
    public static JVector ToJVector(this Vector3 v)
    {
        return new JVector(v.X, v.Y, v.Z);
    }

    public static Vector3 ToVector3(this JVector v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
}