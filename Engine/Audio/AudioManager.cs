using Raylib_cs;
using static Raylib_cs.Raylib;
using System.IO;

namespace Engine;

public static class AudioManager
{
    public static SFXClip[] _allSFX;

    public static MusicTrack[] _allMusic;

    private static MusicTrack? _activeMusic;
    
    // Put global audio info in this class
    public static void InitializeAudio()
    {
        InitAudioDevice();
        // Probably load in the player's diff audio prefs with json files
        // But that is for later

        string[] allMusicFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], MusicTrack.FolderName));

        _allMusic = new MusicTrack[allMusicFiles.Length];
        
        Console.WriteLine(allMusicFiles.Length);

        for (int i = 0; i < allMusicFiles.Length; i++)
        {
            _allMusic[i] = new MusicTrack(i, allMusicFiles[i]);
            Console.Write(_allMusic[i]._music.ToString());
        }

        string[] allSoundFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], SFXClip.FolderName));

        _allSFX = new SFXClip[allSoundFiles.Length];

        Console.WriteLine(allSoundFiles.Length);
        
        for (int i = 0; i < allSoundFiles.Length; i++)
        {
            _allSFX[i] = new SFXClip(i, allSoundFiles[i]);
            Console.WriteLine(_allSFX[i].Sound.ToString());
            Console.WriteLine(allSoundFiles[i]);
        }
        
        ChangeActiveMusic(_allMusic[0]);
        
        SetMasterVolume(.1f);
    }

    public static void UpdateAudio()
    {
        if (_activeMusic != null)
        {
            UpdateMusicStream(_activeMusic._music);
        }
    }

    public static void ChangeActiveMusic(MusicTrack? newTrack)
    {
        if (newTrack == null)
        {
            StopMusicStream(_activeMusic._music);
            return;
        }
        _activeMusic = newTrack;
        PlayMusicStream(_activeMusic._music);
    }

    #region Play SFX

    #region Play Base SFX

    /// <summary>
    /// Plays an sfx clip if the pointer's within bounds
    /// </summary>
    /// <param name="sfxPointer">Pointer for which sfx plays</param>
    public static void PlaySFXClip(int sfxPointer)
    {
        if(sfxPointer < _allSFX.Length) PlaySound(_allSFX[sfxPointer].Sound);
    }

    /// <summary>
    /// Plays an sfx clip based on name
    /// </summary>
    /// <param name="sfxName">Name of sfx to be played</param>
    public static void PlaySFXClip(string sfxName)
    {
        foreach (var sfxClip in _allSFX)
        {
            if (sfxClip.fileName == sfxName)
            {
                PlaySound(sfxClip.Sound);
                return;
            }
        }
    }

    #endregion

    #region Play Volumed SFX

    public static void PlaySFXClip(int sfxPointer, float volume)
    {
        if (sfxPointer < _allSFX.Length)
        {
            SetSoundVolume(_allSFX[sfxPointer].Sound, volume);
            PlaySound(_allSFX[sfxPointer].Sound);
            SetSoundVolume(_allSFX[sfxPointer].Sound, 1f);
        }
    }
    
    public static void PlaySFXClip(string sfxName, float volume)
    {
        foreach (var sfxClip in _allSFX)
        {
            if (sfxClip.fileName == sfxName)
            {
                SetSoundVolume(sfxClip.Sound, volume);
                PlaySound(sfxClip.Sound);
                SetSoundVolume(sfxClip.Sound, 1f);
                return;
            }
        }
    }

    #endregion

    #region Play Volumed & Pitched SFX

    public static void PlaySFXClip(int sfxPointer, float volume, float pitch)
    {
        if (sfxPointer < _allSFX.Length)
        {
            SetSoundVolume(_allSFX[sfxPointer].Sound, volume);
            SetSoundPitch(_allSFX[sfxPointer].Sound, volume);
            PlaySound(_allSFX[sfxPointer].Sound);
            SetSoundPitch(_allSFX[sfxPointer].Sound, 1f);
            SetSoundVolume(_allSFX[sfxPointer].Sound, 1f);
        }
    }
    
    public static void PlaySFXClip(string sfxName, float volume, float pitch)
    {
        foreach (var sfxClip in _allSFX)
        {
            if (sfxClip.fileName == sfxName)
            {
                SetSoundVolume(sfxClip.Sound, volume);
                SetSoundPitch(sfxClip.Sound, pitch);
                PlaySound(sfxClip.Sound);
                SetSoundPitch(sfxClip.Sound, 1f);
                SetSoundVolume(sfxClip.Sound, 1f);
                return;
            }
        }
    }

    #endregion
    
    #endregion

    

    /// <summary>
    /// Unloads all music and sfx, then closes audio device
    /// </summary>
    public static void ExitProgram()
    {
        foreach (var musicTrack in _allMusic)
        {
            UnloadMusicStream(musicTrack._music);
        }

        foreach (var sfxClip in _allSFX)
        {
            UnloadSound(sfxClip.Sound);
        }
        
        CloseAudioDevice();
    }
}