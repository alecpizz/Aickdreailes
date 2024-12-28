using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs.BleedingEdge;

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
}