using Raylib_cs.BleedingEdge;

namespace Engine;

public class PlayerConfig
{
    public float PlayerViewYOffset = 0.6f;
    public float Gravity = 20.0f;
    public float MoveSpeed = 7.0f;
    public float RunAcceleration = 14.0f;
    public float RunDeceleration = 10.0f;
    public float AirAcceleration = 2.0f;
    public float AirDecceleration = 2.0f;
    public float AirControl = 0.3f;
    public float SideStrafeAcceleration = 50.0f;
    public float SideStrafeSpeed = 1.0f;
    public float JumpSpeed = 8.0f;
    public bool AutoBhop = false;
    public float XMouseSensitivity = 30.0f;
    public float YMouseSensitivity = 30.0f;
    public float Friction = 6.0f;
}