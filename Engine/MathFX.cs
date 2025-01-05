using System.Numerics;

namespace Engine;

public static class MathFX
{
    public static float InverseLerp(float a, float b, float v)
    {
        return (v - a) / (b - a);
    }

    public static float Lerp(float a, float b, float t)
    {
        return (1.0f - t) * a + b * t;
    }

    public static float Remap(float iMin, float iMax, float oMin, float oMax, float v)
    {
        float t = InverseLerp(iMin, iMax, v);
        return Lerp(oMin, oMax, t);
    }
    
    public static Vector3 XZPlane(this Vector3 value)
    {
        return new Vector3(value.X, 0f, value.Z);
    }
}