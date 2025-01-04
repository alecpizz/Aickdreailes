using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;

namespace Engine.Animation;

public unsafe class ModelAnimator : IDisposable
{
    private Model _model;
    private int _animationCount;
    private int _animationIndex = 0;
    private int _animationCurrentFrame;
    private ModelAnimation* _animations;
    private float _frameRate = 60f;
    private float _secondsPerFrame;
    private double _lastFrameTime = 0f;
    private int _idleIndex;
    private bool _playingNonIdle = false;
    private ModelAnimation _blendAnimation;

    public ModelAnimator(Model model, string animationPath)
    {
        _model = model;
        _animations = Raylib.LoadModelAnimations(animationPath, ref _animationCount);
        _secondsPerFrame = 1f / _frameRate;
        _lastFrameTime = (float)Raylib.GetTime();
        _blendAnimation = _animations[0];
        for (int i = 0; i < _animationCount; i++)
        {
            ModelAnimation anim = _animations[i];
            var name = Marshal.PtrToStringUTF8(new IntPtr(_animations[i].Name));
            if (name.ToLower().Contains("idle"))
            {
                _animationIndex = i;
                _idleIndex = i;
                break;
            }
        }
    }

    public void SetAnimationIndex(int i)
    {
        _animationIndex = i;
        _animationCurrentFrame = 0;
        _playingNonIdle = true;
    }
    
    public void OnUpdate()
    {
        if (!(Raylib.GetTime() > _lastFrameTime)) return;
        ModelAnimation anim = _animations[_animationIndex];
        // var prevFrame = anim.FramePoses[_animationCurrentFrame];
        _animationCurrentFrame = (_animationCurrentFrame + 1);
        // var nextFrame = anim.FramePoses[_animationCurrentFrame];
        // for (int i = 0; i < anim.BoneCount; i++)
        // {
        //     _blendAnimation.FramePoses
        // }
        if (_animationCurrentFrame >= anim.FrameCount)
        {
            _animationCurrentFrame = 0;
            if (_playingNonIdle)
            {
                _animationIndex = _idleIndex;
                anim = _animations[_animationIndex];
            }
        }
        Raylib.UpdateModelAnimationBones(_model, anim, _animationCurrentFrame);
        _lastFrameTime = Raylib.GetTime() + _secondsPerFrame;
    }

    public void OnImGui()
    {
        for (int i = 0; i < _animationCount; i++)
        {
            var name = Marshal.PtrToStringUTF8(new IntPtr(_animations[i].Name));

            if (ImGui.Button($"{name}"))
            {
                _animationIndex = i;
            }
        }
    }

    public void Dispose()
    {
        Raylib.UnloadModelAnimations(_animations, _animationCount);
    }
}