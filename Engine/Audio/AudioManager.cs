using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine;

public static class AudioManager
{
    // Put global audio info in this class
    public static void InitializeAudio()
    {
        InitAudioDevice();
        // Probably load in the player's diff audio stuff with json files
        
    }

    public static void ExitProgram()
    {
        CloseAudioDevice();
    }
}