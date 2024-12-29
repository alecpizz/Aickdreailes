using System.ComponentModel;
using Raylib_cs.BleedingEdge;

namespace Engine;

/// <summary>
/// The base class for audio informatics
/// </summary>
public abstract class AudioInfo
{
    [ToolboxItem("This int is the sound array value pointer")]
    protected int audioID;

    [ToolboxItem("This string is the file path location to the sound")]
    protected string[] filePath;

    [ToolboxItem("This is the name of the sound file")]
    protected string fileName;

    [ToolboxItem("This is the base path that every sound must take")]
    public static string[] _soundsFilePath = new [] {"Resources", "Sounds"};
}

/// <summary>
/// The class for music informatics
/// </summary>
public class MusicInfo : AudioInfo
{
    public MusicInfo(int musicID, string fileName)
    {
        audioID = musicID;
        this.fileName = fileName;
        filePath = [_soundsFilePath[0], _soundsFilePath[1], _folderName, this.fileName];
        _music = Raylib.LoadMusicStream(Path.Combine
            (filePath[0], filePath[1], filePath[2], filePath[3]));
    }
    
    public static string _folderName = "Music";
    public Music _music { get; private set; }
}

public class SFXInfo : AudioInfo
{
    public SFXInfo(Sound sound, int soundID, string fileName)
    {
        audioID = soundID;
        this.fileName = fileName;
        filePath = [_soundsFilePath[1], _soundsFilePath[2], _folderName, this.fileName];
        _sound = Raylib.LoadSound(Path.Combine
            (filePath[0], filePath[1], filePath[2], filePath[3]));
    }

    public static string _folderName = "Sound Effects";
    private Sound _sound;
}