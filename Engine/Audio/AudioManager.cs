using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;
using System.IO;

namespace Engine;

public static class AudioManager
{
    static SFXInfo[] _allSFX;

    static MusicInfo[] _allMusic;
    
    // Put global audio info in this class
    public static void InitializeAudio()
    {
        InitAudioDevice();
        // Probably load in the player's diff audio stuff with json files

        string[] allMusicFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], MusicInfo._folderName));

        _allMusic = new MusicInfo[allMusicFiles.Length];

        for (int i = 0; i < allMusicFiles.Length; i++)
        {
            _allMusic[i] = new MusicInfo(i, MusicInfo._folderName);
        }
        
        
        // Just a repeat for sfx, but that isn't implemented yet
    }

    public static void UpdateAudio()
    {
        foreach (var musicInfo in _allMusic)
        {
            UpdateMusicStream(musicInfo._music);
        }
    }

    public static void ExitProgram()
    {
        CloseAudioDevice();
    }
}