using System.ComponentModel;
using Raylib_cs;
using System.Linq;
using System.Text.Json.Serialization;

namespace Engine;

/// <summary>
/// The base class for audio informatics
/// </summary>
public abstract class AudioInfo
{
    [JsonIgnore]
    [ToolboxItem("Sound array value pointer")]
    public int audioID { get; protected set; }
    
    public float BaseVolume { get; set; } = 1f;
    
    public float BasePitch { get; set; } = 1f;

    #region File Variables

    [ToolboxItem("File path location to the sound")]
    protected string filePath;

    [JsonIgnore] [ToolboxItem("Name of the sound file")]
    public string fileName{ get; protected set; }

    [ToolboxItem("Base path that every sound must take")]
    public static string _soundsFilePath = Path.Combine("Resources", "Sounds");
    
    [ToolboxItem("File separator character")]
    protected static char fileTweenChar = Path.DirectorySeparatorChar;

    #endregion
    
    public override string ToString()
    {
        return fileName;
    }
}

/// <summary>
/// The class for music informatics
/// </summary>
public class MusicTrack : AudioInfo
{
    // This makes the JSON work, plz don't remove, I don't understand it either
    public MusicTrack()
    { }
    public MusicTrack(int audioID, string filePath)
    {
        this.audioID = audioID;
        this.filePath = filePath;
        _music = Raylib.LoadMusicStream(this.filePath);
        fileName = this.filePath[(1 + this.filePath.LastIndexOf(fileTweenChar))..];
    }
    
    public static string FolderName = "Music";
    [JsonIgnore] 
    public Music _music{ get; private set; }
}

public class SFXClip : AudioInfo
{
    // This makes the JSON work, plz don't remove, I don't understand it either
    public SFXClip()
    { }

    public SFXClip(int audioID, string filePath)
    {
        this.audioID = audioID;
        this.filePath = filePath;
        Sound = Raylib.LoadSound(this.filePath);
        fileName = filePath[(1+filePath.LastIndexOf(fileTweenChar))..];
    }

    public static string FolderName = "Sound Effects";
    [JsonIgnore] 
    public Sound Sound { get; private set; }
}

// TODO:
// Fix null pointer errors with sound and music variables
// Look into LoadSoundAlias() - it is probably necessary for multiple of the same sound