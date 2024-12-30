using System.ComponentModel;
using Raylib_cs;
using System.Linq;

namespace Engine;

/// <summary>
/// The base class for audio informatics
/// </summary>
public abstract class AudioInfo
{
    [ToolboxItem("Sound array value pointer")]
    protected int audioID;

    [ToolboxItem("File path location to the sound")]
    //protected string[] filePath;
    protected string filePath;

    [ToolboxItem("Name of the sound file")]
    protected string fileName;

    [ToolboxItem("Base path that every sound must take")]
    public static string[] _soundsFilePath = new [] {"Resources", "Sounds"};
    
    [ToolboxItem("File separator character")]
    protected static char fileTweenChar = Path.PathSeparator;
    
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
    
    public static string _folderName = "Music";
    public Music _music { get; private set; }
}

public class SFXClip : AudioInfo
{
    public SFXClip(int soundID, string filePath)
    {
        audioID = soundID;
        this.filePath = filePath;
        _sound = Raylib.LoadSound(this.filePath);
        fileName = filePath[(1+filePath.LastIndexOf(fileTweenChar))..];
    }

    public static string _folderName = "Sound Effects";
    public Sound _sound { get; private set; }
}