using Raylib_cs;
using static Raylib_cs.Raylib;
using System.IO;

namespace Engine;

public static class AudioManager
{
    public static SFXClip[] _allSFX;

    public static MusicTrack[] _allMusic;

    static MusicTrack activeMusic;
    
    // Put global audio info in this class
    public static void InitializeAudio()
    {
        InitAudioDevice();
        // Probably load in the player's diff audio prefs with json files
        // But that is for later

        string[] allMusicFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], MusicTrack._folderName));

        _allMusic = new MusicTrack[allMusicFiles.Length];
        
        Console.WriteLine(allMusicFiles.Length);

        for (int i = 0; i < allMusicFiles.Length; i++)
        {
            _allMusic[i] = new MusicTrack(i, allMusicFiles[i]);
            Console.Write(_allMusic[i]._music.ToString());
        }

        string[] allSoundFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], SFXClip._folderName));

        _allSFX = new SFXClip[allSoundFiles.Length];

        Console.WriteLine(allSoundFiles.Length);
        
        for (int i = 0; i < allSoundFiles.Length; i++)
        {
            _allSFX[i] = new SFXClip(i, allSoundFiles[i]);
            Console.WriteLine(_allSFX[i]._sound.ToString());
            Console.WriteLine(allSoundFiles[i]);
        }
        
        ChangeActiveMusic(_allMusic[0]);
        
        SetMasterVolume(.1f);
    }

    public static void UpdateAudio()
    {
        if (activeMusic != null)
        {
            UpdateMusicStream(activeMusic._music);
        }
    }

    public static void ChangeActiveMusic(MusicTrack newTrack)
    {
        if (newTrack == null)
        {
            StopMusicStream(activeMusic._music);
            return;
        }
        activeMusic = newTrack;
        PlayMusicStream(activeMusic._music);
    }

    public static void ExitProgram()
    {
        if (activeMusic != null)
        {
            UnloadMusicStream(activeMusic._music);
        }
        CloseAudioDevice();
    }
}