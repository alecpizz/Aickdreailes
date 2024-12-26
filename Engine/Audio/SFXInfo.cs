using Raylib_cs.BleedingEdge;

namespace Engine;

public class SFXInfo : AudioInfo
{
    // Prolly need to figure everything out; whether or not to make things 
    // All loaded or not;
    // Or to unload and load as necessary
    // Probably just load all of the sounds in
    // Yeah
    
    private static Sound[] _sfxLibrary = 
    {
        new Sound(), new Sound(),
    };
}