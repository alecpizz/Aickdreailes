using System.Numerics;
using Raylib_cs;
using System.Threading.Tasks;
using System;

namespace Engine;

public class  AudioPoint
{
    
    [SerializeField]
    private int whichAudioPointer;

    [SerializeField]
    private Vector3 currentPoint = new();

    private Transform actualCurrentPoint = new();

    public void PlaySFX()
    {
        AudioManager.PlayAutoVolSFXClip(whichAudioPointer, actualCurrentPoint.Translation);
    }

}