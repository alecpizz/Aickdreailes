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
    
    // These values should stay static unless there are ploans to use this in more than a single player game
    #region PLAYER INPUT
    // Jump
    public static KeyboardKey JUMPKEY = KeyboardKey.Space;
    public static GamepadButton JumpButton = GamepadButton.RightFaceDown;
    // Move Horizontally
    public static KeyboardKey MOVERIGHTKEY = KeyboardKey.D;
    public static KeyboardKey MOVELEFTKEY = KeyboardKey.A;
    public static GamepadAxis MoveHorizontalStick = GamepadAxis.RightX;
    // Move Vertically
    public static KeyboardKey MOVEDOWNKEY = KeyboardKey.S;
    public static KeyboardKey MOVEUPKEY = KeyboardKey.W;
    public static GamepadAxis MoveVerticalStick = GamepadAxis.RightY;
    // Look
    public static MouseCursor LOOKMOUSE = MouseCursor.Default;
    //public static MouseCursor HOWMOUSE = MouseCursor.;
    // Shoot
    public static MouseButton SHOOTCLICK = MouseButton.Left;
    public static GamepadButton ShootButton = GamepadButton.RightTrigger2;
    // Zoom
    public static MouseButton ZOOMCLICK = MouseButton.Right;
    public static GamepadButton ZoomButton = GamepadButton.LeftTrigger2;


    #endregion
}