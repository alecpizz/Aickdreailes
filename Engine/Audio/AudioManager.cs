using System.Diagnostics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;
using System.Text.Json;

namespace Engine;

public static class AudioManager
{
    // All the data containers for sfx and music (possibly deprecated?)
    private static SFXClip[] _allSFX;
    private static MusicTrack[] _allMusic;
    
    
    // Figure out whether or not to load in the whole music thing or just the vol and pitch
    #region Dictionaries
    // The accessors for music
    private static Dictionary<string, Music> _musicLibrary = new Dictionary<string, Music>();
    private static Dictionary<string, float> _musicBaseVolLibrary = new Dictionary<string, float>();
    private static Dictionary<string, MusicTrack> _musicLibrary2 = new Dictionary<string, MusicTrack>();
    // The accessors for sfx
    private static Dictionary<string, Sound> _sfxLibrary = new Dictionary<string, Sound>();
    private static Dictionary<string, float> _sfxBaseVolLibrary = new Dictionary<string, float>();
    private static Dictionary<string, SFXClip> _sfxLibrary2 = new Dictionary<string, SFXClip>();
    #endregion

    private enum SoundType
    { Music, SFX }
    // Check concurrent and immutable versions of dictionaries

    #region JSON Variables
    private static readonly string _directoryPath = Directory.GetCurrentDirectory();
    private static string _appDirectoryPath = Process.GetCurrentProcess().ProcessName;
    private static string _appPath2 = Path.GetFullPath(_appDirectoryPath);
    private static string _directoryStartPath = _directoryPath.Remove
        (_directoryPath.LastIndexOf(Path.DirectorySeparatorChar + "bin"));
    
    // Music
    private static string _sfxJSONFilePath = Path.Combine(_directoryStartPath, AudioInfo._soundsFilePath[0], 
        AudioInfo._soundsFilePath[1], "SoundJSONData", "SFXVolCollection.json");
    private static string _sfxJSONString;
    
    private static JsonSerializerOptions audioJsonOptions = 
        new() { WriteIndented  = true };
    
    // SFX
    private static string _musicJSONString;
    private static string _musicJSONFilePath = Path.Combine(_directoryStartPath, AudioInfo._soundsFilePath[0],
        AudioInfo._soundsFilePath[1], "SoundJSONData", "MusicVolCollection.json");
    #endregion
    
    // General vol variables
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static float _userMasterVol = .1f;
    
    // Revisit MusicTrack? implementation
    private static MusicTrack? _activeMusic;
    private static float maxVolDist = 20f;
    private static float minFallOffVolDist = 5f;

    private static float defaultBaseVolume = 1f;
    
    // Put global audio info in this class
    public static void InitializeAudio()
    {
        InitAudioDevice();
        
        #region Music File Init
        
        string[] allMusicFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], MusicTrack.FolderName));

        _allMusic = new MusicTrack[allMusicFiles.Length];

        for (int i = 0; i < allMusicFiles.Length; i++)
        {
            _allMusic[i] = new MusicTrack(i, allMusicFiles[i]);
            _musicLibrary.Add(_allMusic[i].fileName, _allMusic[i]._music);
            //_musicLibrary2.Add(_allMusic[i].fileName, _allMusic[i]);
        }
        
        Console.WriteLine("\n\n\n" +_musicJSONString);
        
        Console.WriteLine(_appPath2);
        
        #endregion
        
        #region SFX File init
        string[] allSoundFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], SFXClip.FolderName));

        _allSFX = new SFXClip[allSoundFiles.Length];
        
        for (int i = 0; i < allSoundFiles.Length; i++)
        {
            _allSFX[i] = new SFXClip(i, allSoundFiles[i]);
            _sfxLibrary.Add(_allSFX[i].fileName, _allSFX[i].Sound);
            
            //_sfxLibrary2.Add(_allSFX[i].fileName, _allSFX[i]);
        }
        #endregion
        
        #region Json Volume Init

        // Chain of events:
        // 1. Try to load in base volumes from json file ~ if failed, add all sounds and default volume to dictionary
        // 2. Check if the sound dictionary and volume dictionary don't match ~ if !, remove or add mismatches
        // 3. Apply base vol
        // 4. Store base vol

        LoadMusicBaseVol();
        LoadSfxBaseVol();
        
        AssureMusicDictionaryParity();
        AssureSFXDictionaryParity();

        ApplyMusicBaseSounds();
        ApplySFXBaseSounds();

        StoreMusicData();
        StoreSFXData();
        
        #endregion
        
        ChangeSFXBaseVolume("tada.mp3", 1f);
        
        ChangeActiveMusic(_allMusic[1]);
        
        // Load in the player's diff audio prefs with json files
        // Master, sfx, and music
        // Look into audiostreams for diff volumes???
        SetMasterVolume(_userMasterVol);
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
            _activeMusic = newTrack;
            return;
        }
        if (_activeMusic == null)
        {
            _activeMusic = newTrack;
            PlayMusicStream(_activeMusic._music);
        }
    }

    #region Play SFX

    #region Play Auto Volumed SFX
    
    public static void PlayAutoVolSFXClip(string sfxName, Vector3 audioPoint)
    {
        if (!_sfxLibrary2.TryGetValue(sfxName, out var sfxClip))
        {
            return;
        }
        float soundDistance = CalculateSoundDistance(audioPoint);
        float volume = CalculateVolume(soundDistance) * sfxClip.BaseSoundVolume;
                
        SetSoundVolume(sfxClip.Sound, volume);
        PlaySound(sfxClip.Sound);
        SetSoundVolume(sfxClip.Sound,sfxClip.BaseSoundVolume);
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
    
    #region Base Volume Functions
    
    private static void AssureSFXDictionaryParity()
    {
        foreach (var sfxData in _sfxLibrary2)
        {
            if (!_allSFX.Contains(sfxData.Value))
            {
                _sfxLibrary2.Remove(sfxData.Key);
            }
        }
        
        foreach (var sfx in _allSFX)
        {
            _sfxLibrary2.TryAdd(sfx.fileName, sfx);
        }
    }

    private static void AssureMusicDictionaryParity()
    {
        foreach (var musicData in _musicLibrary2)
        {
            if (!_allMusic.Contains(musicData.Value))
            {
                _musicLibrary2.Remove(musicData.Key);
            }
        }
        
        foreach (var musicTrack in _allMusic)
        {
            _musicLibrary2.TryAdd(musicTrack.fileName, musicTrack);
        }
    }
    
    public static void ChangeMusicBaseVolume(string musicName, float newBaseVol)
    {
        if (!_musicLibrary2.TryGetValue(musicName, out var musicTrack))
        { return;}

        musicTrack.BaseSoundVolume = newBaseVol;
        StoreMusicData();
    }
    
    public static void ChangeSFXBaseVolume(string sfxName, float newBaseVol)
    {
        if (!_sfxLibrary2.TryGetValue(sfxName, out var sfxClip))
        { return; }

        sfxClip.BaseSoundVolume = newBaseVol;
        StoreSFXData();
    }

    /// <summary>
    /// Applies the respective base volume and pitch to each sfx
    /// </summary>
    private static void ApplySFXBaseSounds()
    {
        foreach (var sfxSound in _sfxLibrary2)
        {
            SetSoundVolume(sfxSound.Value.Sound, sfxSound.Value.BaseSoundVolume);
            SetSoundPitch(sfxSound.Value.Sound,sfxSound.Value.BasePitch);
        }
    }
    
    /// <summary>
    /// Applies the respective base volume and pitch to each song
    /// </summary>
    private static void ApplyMusicBaseSounds()
    {
        foreach (var musicDisc in _musicLibrary2)
        {
            SetMusicVolume(musicDisc.Value._music, musicDisc.Value.BaseSoundVolume);
            SetMusicPitch(musicDisc.Value._music,musicDisc.Value.BasePitch);
        }
    }

    /// <summary>
    /// All the songs and their filenames get added to a dictionary
    /// </summary>
    /// <returns>Music track dictionary with all the songs</returns>
    private static Dictionary<string, MusicTrack> refillMusicTracks()
    {
        Dictionary<string, MusicTrack> refilledDictionary = new Dictionary<string, MusicTrack>();
        foreach (var musicTrack in _allMusic)
        {
            refilledDictionary.Add(musicTrack.fileName,musicTrack);
        }
        return refilledDictionary;
    }

    /// <summary>
    /// All the sfx and their filenames get added to a dictionary
    /// </summary>
    /// <returns>Sfx clip dictionary with all sfx</returns>
    private static Dictionary<string, SFXClip> refillSfxClips()
    {
        Dictionary<string, SFXClip> refilledDictionary = new Dictionary<string, SFXClip>();
        foreach (var sfx in _allSFX)
        {
            refilledDictionary.Add(sfx.fileName,sfx);
        }
        return refilledDictionary;
    }

    #endregion
    
    #region JSON Functions

    #region Store and Load in Json files

    /// <summary>
    /// Streams in sfx dictionary data
    /// </summary>
    private static void StoreSFXData()
    {
        using FileStream jsonStream = File.Create(_sfxJSONFilePath);
        JsonSerializer.Serialize(jsonStream, _sfxLibrary2, audioJsonOptions);
    }

    /// <summary>
    /// Streams in music dictionary data
    /// </summary>
    private static void StoreMusicData()
    {
        using FileStream jsonStream = File.Create(_musicJSONFilePath);
        JsonSerializer.Serialize(jsonStream, _musicLibrary2, audioJsonOptions);
    }

    /// <summary>
    /// Uses a json file to load in saved sfx data
    /// </summary>
    private static void LoadSfxBaseVol()
    {
        using FileStream jsonOutput = File.OpenRead(_sfxJSONFilePath);
        _sfxLibrary2 = JsonSerializer.Deserialize<Dictionary<string, SFXClip>>(jsonOutput)
            ?? refillSfxClips();
    }
    
    /// <summary>
    /// Uses a json file to load in saved music data
    /// </summary>
    private static void LoadMusicBaseVol()
    {
        using FileStream jsonOutput = File.OpenRead(_musicJSONFilePath);
        _musicLibrary2 = JsonSerializer.Deserialize<Dictionary<string, MusicTrack>>(jsonOutput)
            ?? refillMusicTracks();
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