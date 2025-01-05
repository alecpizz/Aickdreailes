using System.Numerics;
using Raylib_cs;
using System.Text.Json;

namespace Engine;

public static class InputExtensions
{
    public static Vector2 PlayerMovementInput()
    {
        Vector2 result = Vector2.Zero;

        #region Controller

        if (Raylib.IsGamepadAvailable(0))
        {
            if (Raylib.GetGamepadAxisMovement(0, GamepadControlSet.MOVEVERTICALSTICK) != 0)
            {
                result.Y += Raylib.GetGamepadAxisMovement(0, GamepadControlSet.MOVEVERTICALSTICK);
            }

            if (Raylib.GetGamepadAxisMovement(0, GamepadControlSet.MOVEHORIZONTALSTICK) != 0)
            {
                result.X += Raylib.GetGamepadAxisMovement(0, GamepadControlSet.MOVEHORIZONTALSTICK);
            }
        }

        #endregion

        #region Mouse & Keyboard

        if (Raylib.IsKeyDown(PCControlSet.MOVELEFTKEY))
        {
            result.X += 1.0f;
        }

        if (Raylib.IsKeyDown(PCControlSet.MOVERIGHTKEY))
        {
            result.X += -1.0f;
        }

        if (Raylib.IsKeyDown(PCControlSet.MOVEUPKEY))
        {
            result.Y += 1.0f;
        }

        if (Raylib.IsKeyDown(PCControlSet.MOVEDOWNKEY))
        {
            result.Y += -1.0f;
        }

        #endregion

        return result;
    }
}

/// <summary>
/// The controls for using mouse and keyboard
/// </summary>
public static class PCControlSet
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

    public static KeyboardKey PICKUPKEY = KeyboardKey.E;

    // Zoom
    public static MouseButton ZOOMCLICK = MouseButton.Right;

    // Menu
    public static KeyboardKey MENUKEY = KeyboardKey.Escape;

    public static KeyboardKey[] ReMappableKeys =
    {
        JUMPKEY, MOVERIGHTKEY, MOVELEFTKEY, MOVEDOWNKEY, MOVEUPKEY, RELOADKEY, MENUKEY
    };

    public enum KeyMappingPointer : byte
    {
        Jump,
        MoveRight,
        MoveLeft,
        MoveDown,
        MoveUp,
        Reload,
        Menu
    };
}

/// <summary>
/// The controls for using a gamepad
/// </summary>
public static class GamepadControlSet
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

    // Menu
    public static GamepadButton MENUBUTTON = GamepadButton.MiddleRight;

    public static GamepadButton[] ReMappableButtons =
    {
        JUMPBUTTON, SHOOTBUTTON, RELOADBUTTON, ZOOMBUTTON, MENUBUTTON
    };

    public enum ButtonMappingPointer : byte
    {
        Jump,
        Shoot,
        Reload,
        Zoom,
        Menu
    };
}