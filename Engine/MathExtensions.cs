using System.Numerics;
using Jitter2.LinearMath;

namespace Engine;

public static class MathExtensions
{
    public static JQuaternion FromToRotation(JVector from, JVector to)
    {
        JQuaternion q = new();

        float dp = JVector.Dot(from, to);

        if (dp > 0.9999f)
            return JQuaternion.Identity;
        else if (dp < -0.9999f)
            return AngleAxis(MathF.PI, JVector.UnitY);

        JVector a = JVector.Cross(from, to);

        q.X = a.X;
        q.Y = a.Y;
        q.Z = a.Z;

        q.W = MathF.Sqrt((from.LengthSquared()) * (to.LengthSquared())) + JVector.Dot(from, to);
        q.Normalize();
        return q;
    }

    public static JQuaternion AngleAxis(float angle, JVector axis)
    {
        var result = new JQuaternion(
            MathF.Cos(angle * 0.5f),
            axis.X * MathF.Sin(angle * 0.5f),
            axis.Y * MathF.Sin(angle * 0.5f),
            axis.Z * MathF.Sin(angle * 0.5f)
        );
        result.Normalize();
        return result;
    }
    
    public static JVector MultiplyQuat(JQuaternion q, JVector v)
    {
        var quatVector = new JVector(q.X, q.Y, q.Z);
        var uv = JVector.Cross(quatVector, v);
        var uuv = JVector.Cross(quatVector, uv);

        return v + ((uv * q.W) + uuv) * 2.0f;
    }
}