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
    [ToolboxItem("Sound array value pointer")]
    public int audioID { get; protected set; }

    public float BaseVolume { get; set; }

    public float BasePitch { get; set; }

    #region File Variables

    [ToolboxItem("File path location to the sound")]
    protected string filePath;

    [ToolboxItem("Name of the sound file")]
    public string fileName { get; protected set; }

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
    public MusicTrack(int musicID, string filePath)
    {
        audioID = musicID;
        this.filePath = filePath;
        _music = Raylib.LoadMusicStream(this.filePath);
        fileName = this.filePath[(1 + this.filePath.LastIndexOf(fileTweenChar))..];
    }
    
    public static string FolderName = "Music";
    [JsonIgnore]
    public Music _music { get; private set; }

    private int timer;
}

public class SFXClip : AudioInfo
{
    public SFXClip(int soundID, string filePath)
    {
        audioID = soundID;
        this.filePath = filePath;
        Sound = Raylib.LoadSound(this.filePath);
        fileName = filePath[(1+filePath.LastIndexOf(fileTweenChar))..];
    }

    public static string FolderName = "Sound Effects";
    [JsonIgnore]
    public Sound Sound { get; private set; }
}

// TODO LIST:
// Variable that saves dev deemed volume for each sound
// Variable that saves dev deemed pitch for each sound
// Look into LoadSoundAlias() - it is probably necessary for multiple of the same sound