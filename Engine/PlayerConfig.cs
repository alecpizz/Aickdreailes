using System.Reflection;
using ImGuiNET;

namespace Engine;

public class PlayerConfig
{
    [SerializeField] public float PlayerViewYOffset = 0.6f;
    [SerializeField] public float Gravity = 20.0f;
    [SerializeField] public float MoveSpeed = 7.0f;
    [SerializeField] public float RunAcceleration = 14.0f;
    [SerializeField] public float RunDeceleration = 10.0f;
    [SerializeField] public float AirAcceleration = 2.0f;
    [SerializeField] public float AirDecceleration = 2.0f;
    [SerializeField] public float AirControl = 0.3f;
    [SerializeField] public float SideStrafeAcceleration = 50.0f;
    [SerializeField] public float SideStrafeSpeed = 1.0f;
    [SerializeField] public float JumpSpeed = 8.0f;
    [SerializeField] public bool AutoBhop = false;
    [SerializeField] public float XMouseSensitivity = 30.0f;
    [SerializeField] public float YMouseSensitivity = 30.0f;
    [SerializeField] public float Friction = 6.0f;

    public PlayerConfig()
    {
    }

    public void HandleImGui()
    {
        ImGUIUtils.DrawFields(this);
    }
}