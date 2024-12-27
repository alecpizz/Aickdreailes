using System.Numerics;

namespace Engine;

public static class MathFX
{

    public static Vector3 XZPlane(this Vector3 value)
    {
        return new Vector3(value.X, 0f, value.Z);
    }
}