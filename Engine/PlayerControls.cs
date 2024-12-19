using Raylib_cs.BleedingEdge;

namespace Engine;

public static class PlayerControls
{
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