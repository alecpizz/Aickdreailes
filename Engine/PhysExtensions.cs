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

    public static Quaternion ToQuaternion(this JQuaternion q)
    {
        return new Quaternion(q.X, q.Y, q.Z, q.W);
    }

    public static JQuaternion ToJQuaternion(this Quaternion q)
    {
        return new JQuaternion(q.X, q.Y, q.Z, q.W);
    }
}