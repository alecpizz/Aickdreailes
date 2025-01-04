using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
namespace Engine;

public record struct Light
{
    public LightType Type;
    public bool Enabled;
    public Vector3 Position;
    public Vector3 Target;
    public Color Color;

    public Light(LightType type, bool enabled, Vector3 position, Vector3 target, Color color)
    {
        Type = type;
        Enabled = enabled;
        Position = position;
        Target = target;
        Color = color;
    }
}