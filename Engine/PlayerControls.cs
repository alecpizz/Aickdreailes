using Raylib_cs.BleedingEdge;
using System.Text.Json;

namespace Engine;

/// <summary>
/// Deprecated section that will eventually be reused to make a button/key swap feature
/// At least that's what I plan to do eventually
/// </summary>
public static class PlayerControls
{
    // This method isn't smaller than raylib
    /*public static bool IsControlDown(KeyboardKey checkingForDownKey)
    {
        return Raylib.IsKeyDown(checkingForDownKey);
    }*/
}

/// <summary>
/// The controls for using mouse and keyboard
/// </summary>
public struct PCControlSet
{
    // Jump
    public static KeyboardKey JUMPKEY = KeyboardKey.Space;
    // Move Horizontally
    public static KeyboardKey MOVERIGHTKEY = KeyboardKey.D;
    public static KeyboardKey MOVELEFTKEY = KeyboardKey.A;
    // Move Vertically
    public static KeyboardKey MOVEDOWNKEY = KeyboardKey.S;
    public static KeyboardKey MOVEUPKEY = KeyboardKey.W;
    // Look
    public static MouseCursor LOOK = MouseCursor.Default;
    // Shoot
    public static MouseButton SHOOTCLICK = MouseButton.Left;
    // Reload
    public static KeyboardKey RELOADKEY = KeyboardKey.R;
    // Zoom
    public static MouseButton ZOOMCLICK = MouseButton.Right;
}

/// <summary>
/// The controls for using a gamepad
/// </summary>
public struct GamepadControlSet
{ 
    // Jump
    public static GamepadButton JUMPBUTTON = GamepadButton.RightFaceDown;
    // Move Horizontally
    public static GamepadAxis MOVEHORIZONTALSTICK = GamepadAxis.RightX;
    // Move Vertically
    public static GamepadAxis MOVEVERTICALSTICK = GamepadAxis.RightY;
    // Shoot
    public static GamepadButton SHOOTBUTTON = GamepadButton.RightTrigger2;
    //Reload
    public static GamepadButton RELOADBUTTON = GamepadButton.RightFaceLeft;
    // Zoom
    public static GamepadButton ZOOMBUTTON = GamepadButton.LeftTrigger2;
}