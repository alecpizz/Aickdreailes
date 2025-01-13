using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine;

public static class AudioManager
{
    // All the data containers for sfx and music (possibly deprecated?)
    private static SFXClip[] _allSFX;
    private static MusicTrack[] _allMusic;
    // The accessors for music
    private static Dictionary<string, Music> _musicLibrary = new Dictionary<string, Music>();
    private static Dictionary<string, float>? _musicBaseVolLibrary;
    // The accessors for sfx
    private static Dictionary<string, Sound> _sfxLibrary = new Dictionary<string, Sound>();
    private static Dictionary<string, float>? _sfxBaseVolLibrary;
    // Check concurrent and immutable versions of dictionaries

    #region JSON Variables
    // Music
    private static string _sfxJSONFilePath = Path.Combine(AudioInfo._soundsFilePath[0], 
        AudioInfo._soundsFilePath[1], "SoundJSONData", "SFXVolCollection.json");
    private static string _sfxJSONString;
    
    private static JsonSerializerOptions audioJsonOptions = 
        new JsonSerializerOptions { WriteIndented  = true };
    
    // SFX
    private static string _musicJSONString;
    private static string _musicJSONFilePath = Path.Combine(AudioInfo._soundsFilePath[0],
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
        }

        //FileStream intoJsonStream = File.OpenWrite(_musicJSONFilePath);
        _musicJSONString = JsonSerializer.Serialize(_musicLibrary);
        
        File.WriteAllText(_musicJSONFilePath, _musicJSONString);
        
        Console.WriteLine("\n\n\n" +_musicJSONString);
        
        /*foreach (var MusicDisc in _musicLibrary)
        {
            if (_musicBaseVolLibrary.TryAdd(MusicDisc.Key, 1f))
            {
                
            }
            
        }*/
        
        #endregion
        
        #region SFX File init
        string[] allSoundFiles = Directory.GetFiles(Path.Combine
            (AudioInfo._soundsFilePath[0], AudioInfo._soundsFilePath[1], SFXClip.FolderName));

        _allSFX = new SFXClip[allSoundFiles.Length];
        
        for (int i = 0; i < allSoundFiles.Length; i++)
        {
            _allSFX[i] = new SFXClip(i, allSoundFiles[i]);
            _sfxLibrary.Add(_allSFX[i].fileName, _allSFX[i].Sound);
        }
        #endregion
        
        #region Json Volume Init

        // Chain of events:
        // 1. Try to load in base volumes from json file ~ if failed, add all sounds and default volume to dictionary
        // 2. Check if the sound dictionary and volume dictionary don't match ~ if !, remove or add mismatches
        // 3. Apply base vol
        // 4. Store base vol
        
        if (!LoadSoundBaseVol(ref _musicBaseVolLibrary, _musicJSONFilePath))
        { RefillMusicBaseVol(); }

        if (!LoadSoundBaseVol(ref _sfxBaseVolLibrary, _sfxJSONFilePath))
        { RefillSFXBaseVol(); }
        
        AssureMusicDictionaryParity();
        AssureSFXDictionaryParity();

        ApplyMusicBaseSounds();
        ApplySFXBaseSounds();

        StoreBaseVol(_musicBaseVolLibrary, _musicJSONFilePath);
        StoreBaseVol(_sfxBaseVolLibrary, _sfxJSONFilePath);
        
        #endregion
        
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
    
    #region Base Volume Functions

    // I want to use inheritance to make single functions :/
    // It's gross at this point

    private static void AssureSFXDictionaryParity()
    {
        List<string> allChangingKeys = new List<string>();

        allChangingKeys =
            (List<string>)from key in _sfxBaseVolLibrary.Keys
            where !_sfxLibrary.ContainsKey(key)
            select key;

        // Removes all keys not in sfx library
        foreach (var key in allChangingKeys)
        {
            _sfxBaseVolLibrary.Remove(key);
        }
        
        allChangingKeys =
            (List<string>)from key in _sfxLibrary.Keys
            where !_sfxBaseVolLibrary.ContainsKey(key)
            select key;

        // Adds all keys not in base vol library
        foreach (var key in allChangingKeys)
        {
            _sfxBaseVolLibrary.Add(key, defaultBaseVolume);
        }
    }

    private static void AssureMusicDictionaryParity()
    {
        List<string> allChangingKeys = new List<string>();

        allChangingKeys =
            (List<string>)from key in _musicBaseVolLibrary.Keys
            where !_musicLibrary.ContainsKey(key)
            select key;

        // Removes all keys not in sfx library
        foreach (var key in allChangingKeys)
        {
            _musicBaseVolLibrary.Remove(key);
        }
        
        allChangingKeys =
            (List<string>)from key in _musicLibrary.Keys
            where !_musicBaseVolLibrary.ContainsKey(key)
            select key;

        // Adds all keys not in base vol library
        foreach (var key in allChangingKeys)
        {
            _musicBaseVolLibrary.Add(key, defaultBaseVolume);
        }
    }
    
    public static void ChangeBaseMusicLibraryVolume(string musicName, float newBaseVol)
    {
        if (!_musicBaseVolLibrary.ContainsKey(musicName))
        { return;}

        _musicBaseVolLibrary[musicName] = newBaseVol;
        StoreBaseVol(_musicBaseVolLibrary, _musicJSONFilePath);
        // May put above function into exit program, not sure
    }
    
    public static void ChangeBaseSFXLibraryVolume(string sfxName, float newBaseVol)
    {
        if (!_sfxBaseVolLibrary.ContainsKey(sfxName))
        { return; }

        _sfxBaseVolLibrary[sfxName] = newBaseVol;
        StoreBaseVol(_sfxBaseVolLibrary, _sfxJSONFilePath);
    }

    private static void ApplySFXBaseSounds()
    {
        foreach (var sfxSound in _sfxLibrary)
        {
                SetSoundVolume(sfxSound.Value, _sfxBaseVolLibrary[sfxSound.Key]);
        }
    }

    private static void ApplyMusicBaseSounds()
    {
        bool dictionariesMatch = true;
        foreach (var musicDisc in _musicLibrary)
        {
            SetMusicVolume(musicDisc.Value, _musicBaseVolLibrary[musicDisc.Key]);
        }
    }
    
    private static void RefillSFXBaseVol()
    {
        _sfxBaseVolLibrary = new Dictionary<string, float>();
        foreach (var soundDictionary in _sfxLibrary)
        {
            _sfxBaseVolLibrary.Add(soundDictionary.Key, defaultBaseVolume);
        }
    }
    
    private static void RefillMusicBaseVol()
    {
        _musicBaseVolLibrary = new Dictionary<string, float>();
        foreach (var musicDictionary in _musicLibrary)
        {
            _musicBaseVolLibrary.Add(musicDictionary.Key, defaultBaseVolume);
        }
    }

    #endregion
    
    #region JSON Functions

    // TODO:
    // Make a function that checks if the music library doesn't match up with the music base vol

    /// <summary>
    /// Erases the json sound file
    /// </summary>
    public static void EraseSoundJsonFile(string filePath)
    {
        StoreBaseVol(null, filePath);
    }

    #region Store and Load in Json files

    /// <summary>
    /// Sends base vol library to json file
    /// </summary>
    /// <param name="baseVolLibrary">The sfx or music base volume dictionary</param>
    /// <param name="filePath">The file path</param>
    private static void StoreBaseVol(Dictionary<string, float>? baseVolLibrary, string filePath)
    {
        FileStream jsonStream = File.OpenRead(filePath);
        JsonSerializer.Serialize(jsonStream, baseVolLibrary);
    }

    /// <summary>
    /// Puts json base vol info into a dictionary
    /// </summary>
    /// <param name="baseVolLibrary">The sfx or music base volume dictionary</param>
    /// <param name="filePath">The file path</param>
    /// <returns>If what loaded in was not null</returns>
    private static bool LoadSoundBaseVol(ref Dictionary<string, float>? baseVolLibrary, string filePath)
    {
        FileStream jsonOutput = File.OpenRead(filePath);
        baseVolLibrary = JsonSerializer.Deserialize<Dictionary<string, float>>(jsonOutput);
        return baseVolLibrary != null;
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