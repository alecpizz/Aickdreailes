using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Engine;

public static class AudioManager
{
    public static SFXClip[] _allSFX;
    public static MusicTrack[] _allMusic;
    private static Dictionary<string, Sound> _sfxLibrary;

    private static MusicTrack? _activeMusic;
    private static float maxVolDist = 20f;
    private static float minFallOffVolDist = 5f;
    
    
    // Put global audio info in this class
    public static void InitializeAudio()
    {
        InitAudioDevice();
        
        // Load in the player's diff audio prefs with json files
        // For future

        string[] allMusicFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], MusicTrack.FolderName));

        _allMusic = new MusicTrack[allMusicFiles.Length];

        for (int i = 0; i < allMusicFiles.Length; i++)
        {
            _allMusic[i] = new MusicTrack(i, allMusicFiles[i]);
            Console.Write(_allMusic[i]._music.ToString());
        }

        string[] allSoundFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], SFXClip.FolderName));

        _allSFX = new SFXClip[allSoundFiles.Length];
        
        for (int i = 0; i < allSoundFiles.Length; i++)
        {
            _allSFX[i] = new SFXClip(i, allSoundFiles[i]);
            _sfxLibrary.Add(_allSFX[i].fileName, _allSFX[i].Sound);
        }
        
        ChangeActiveMusic(_allMusic[1]);
        
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

    #region Play Auto Volumed SFX

    public static void PlayAutoVolSFXClip(int sfxPointer, Vector3 audioPoint)
    {
        if (sfxPointer >= _allSFX.Length)
        {
            return;
        }

        float soundDistance = CalculateSoundDistance(audioPoint);
            
        // This will keep the sounds volume if it is within the min fall off distance
        // It will check if the sound is within the max fall off ditance, if not it will play at 0 volume
        // If it is within the max distance, it will 
        float volume = CalculateVolume(soundDistance);
        
        SetSoundVolume(_allSFX[sfxPointer].Sound, volume); 
        PlaySound(_allSFX[sfxPointer].Sound);
    }
    
    public static void PlayAutoVolSFXClip(string sfxName, Vector3 audioPoint)
    {
        if (!_sfxLibrary.TryGetValue(sfxName, out var sfxClip))
        {
            return;
        }
        float soundDistance = CalculateSoundDistance(audioPoint);
        float volume = CalculateVolume(soundDistance);
                
        SetSoundVolume(sfxClip, volume);
        PlaySound(sfxClip);
    }

    #endregion

    #region Play Custom Volumed & Pitched SFX

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
    
    #region Calculations for SFX
    
    

    private static float CalculateSoundDistance(Vector3 soundPosition)
    {
        return Vector3.Distance(soundPosition, Engine.Camera.Position);
    }

    private static float CalculateSoundPan(Vector3 soundPosition)
    {
        //float relativeXZ = Engine.Camera.Up;
        
        float newPan = 1f;
        
        return 1f;
    }

    private static float CalculateVolume(float soundDistance)
    {
        return MathFX.InverseLerp(maxVolDist,minFallOffVolDist, soundDistance);
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