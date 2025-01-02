using System.Numerics;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;
namespace Engine;

public enum LightType
{
    Directional,
    Point
}

public struct Light
{
    public LightType Type;
    public bool Enabled;
    public Vector3 Position;
    public Vector3 Target;
    public Color Color;
    public float Attenuation;
    public int EnabledLoc;
    public int TypeLoc;
    public int PositionLoc;
    public int TargetLoc;
    public int ColorLoc;
    public int AttenuationLoc;

    public static Light CreateLight(LightType type, Vector3 position, Vector3 target, Color color, Shader shader)
    {
        Light light = new();
        if (_lightsCount < MaxLights)
        {
            light.Enabled = true;
            light.Type = type;
            light.Position = position;
            light.Target = target;
            light.Color = color;

            light.EnabledLoc = GetShaderLocation(shader, $"lights[{_lightsCount}].enabled");
            light.TypeLoc = GetShaderLocation(shader, $"lights[{_lightsCount}].type");
            light.PositionLoc = GetShaderLocation(shader, $"lights[{_lightsCount}].position");
            light.TargetLoc = GetShaderLocation(shader, $"lights[{_lightsCount}].target");
            light.ColorLoc = GetShaderLocation(shader, $"lights[{_lightsCount}].color");
            UpdateLightValues(shader, light);
            _lightsCount++;
        }

        return light;
    }

    public static void UpdateLightValues(Shader shader, Light light)
    {
        SetShaderValue(shader, light.EnabledLoc, light.Enabled ? 1 : 0, ShaderUniformDataType.Int);
        SetShaderValue(shader, light.TypeLoc, (int)light.Type, ShaderUniformDataType.Int);
        SetShaderValue(shader, light.PositionLoc, light.Position, ShaderUniformDataType.Vec3);
        SetShaderValue(shader, light.TargetLoc, light.Target, ShaderUniformDataType.Vec3);
        SetShaderValue(shader, light.ColorLoc, new Vector4(light.Color.R / 255f, light.Color.G / 255f, 
            light.Color.B / 255f, light.Color.A / 255f), ShaderUniformDataType.Vec4);
    }

    public static int _lightsCount = 0;
    private const int MaxLights = 4;
}