using System.ComponentModel;
using Raylib_cs;
using System.Linq;

namespace Engine;

/// <summary>
/// The base class for audio informatics
/// </summary>
public abstract class AudioInfo
{
    [ToolboxItem("This int is the sound array value pointer")]
    protected int audioID;

    [ToolboxItem("This string is the file path location to the sound")]
    //protected string[] filePath;
    protected string filePath;

    [ToolboxItem("This is the name of the sound file")]
    protected string fileName;

    [ToolboxItem("This is the base path that every sound must take")]
    public static string[] _soundsFilePath = new [] {"Resources", "Sounds"};
    
    // This is a disgusting hack method, plz don't look at this...
    protected static char fileTweenChar = 
            Path.Combine(_soundsFilePath[0], _soundsFilePath[1]).
                Remove(0, _soundsFilePath[0].Length).
                Remove(1, _soundsFilePath[1].Length)[0];
    
    public override string ToString()
    {
        return fileName;
    }

    protected int FindStartOfFileName(string totalFilePath)
    {
        return totalFilePath.LastIndexOf(fileTweenChar);
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
        //base.filePath = [_soundsFilePath[0], _soundsFilePath[1], _folderName, this.fileName];
        _music = Raylib.LoadMusicStream(this.filePath/*Path.Combine
            (base.filePath[0], base.filePath[1], base.filePath[2], base.filePath[3])*/);
        fileName = filePath[(1 + filePath.LastIndexOf(fileTweenChar))..];
    }
    
    public static string _folderName = "Music";
    public Music _music { get; private set; }
}

public class SFXClip : AudioInfo
{
    public SFXClip(int soundID, string filePath)
    {
        audioID = soundID;
        //this.fileName = filePath;
        this.filePath = filePath;
        //base.filePath = [_soundsFilePath[0], _soundsFilePath[1], _folderName, this.fileName];
        _sound = Raylib.LoadSound(this.filePath/*Path.Combine
            (base.filePath[0], base.filePath[1], base.filePath[2], base.filePath[3])*/);
        fileName = filePath[(1+filePath.LastIndexOf(fileTweenChar))..];
    }

    public static string _folderName = "Sound Effects";
    public Sound _sound { get; private set; }
}