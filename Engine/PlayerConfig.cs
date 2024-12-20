using System.Reflection;
using ImGuiNET;

namespace Engine;

public class PlayerConfig
{
    public float PlayerViewYOffset { get; set; } = 0.6f;
    public float Gravity { get; set; } = 20.0f;
    public float MoveSpeed { get; set; } = 7.0f;
    public float RunAcceleration { get; set; } = 14.0f;
    public float RunDeceleration { get; set; } = 10.0f;
    public float AirAcceleration { get; set; } = 2.0f;
    public float AirDecceleration { get; set; } = 2.0f;
    public float AirControl { get; set; } = 0.3f;
    public float SideStrafeAcceleration { get; set; } = 50.0f;
    public float SideStrafeSpeed { get; set; } = 1.0f;
    public float JumpSpeed { get; set; } = 8.0f;
    public bool AutoBhop { get; set; } = false;
    public float XMouseSensitivity { get; set; } = 30.0f;
    public float YMouseSensitivity { get; set; } = 30.0f;
    public float Friction { get; set; } = 6.0f;
    private PropertyInfo[] _properties;

    public PlayerConfig()
    {
        _properties = GetType().GetProperties();
    }

    public void HandleImGui()
    {
        foreach (var property in _properties)
        {
            var value = property.GetValue(this);
            if (value is bool b)
            {
                ImGui.Checkbox(property.Name, ref b);
                property.SetValue(this, b);
            }
            else if(value is float f)
            {
                ImGui.InputFloat(property.Name, ref f);
                property.SetValue(this, f);
            }
        }
    }
}