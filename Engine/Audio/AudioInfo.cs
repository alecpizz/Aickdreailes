using System.ComponentModel;
using Raylib_cs;

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
    public MusicTrack(int musicID, string fileName)
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

public class SFXClip : AudioInfo
{
    public SFXClip(int soundID, string fileName)
    {
        audioID = soundID;
        this.fileName = fileName;
        filePath = [_soundsFilePath[0], _soundsFilePath[1], _folderName, this.fileName];
        _sound = Raylib.LoadSound(Path.Combine
            (filePath[0], filePath[1], filePath[2], filePath[3]));
    }

    public static string _folderName = "Sound Effects";
    public Sound _sound { get; private set; }
}