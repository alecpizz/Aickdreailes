using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs.BleedingEdge;

namespace Engine.Animation;

public unsafe class ModelAnimator : IDisposable
{
    private Model _model;
    private int _animationCount;
    private int _animationIndex = 2;
    private int _animationCurrentFrame;
    private Shader _skinningShader;
    private ModelAnimation* _animations;
    private float _frameRate = 60f;
    private float _secondsPerFrame;
    private double _lastFrameTime = 0f;

    public ModelAnimator(Model model, string animationPath)
    {
        _model = model;
        _skinningShader = Raylib.LoadShader(Path.Combine("Resources", "Shaders", "skinning.vert"),
            Path.Combine("Resources", "Shaders", "skinning.frag"));
        for (int i = 0; i < _model.MaterialCount; i++)
        {
            _model.Materials[i].Shader = _skinningShader;
        }

        _animations = Raylib.LoadModelAnimations(animationPath, out _animationCount);
        _secondsPerFrame = 1f / _frameRate;
        _lastFrameTime = (float)Raylib.GetTime();
    }

    public void OnUpdate()
    {
        if (Raylib.GetTime() > _lastFrameTime)
        {
            ModelAnimation anim = _animations[_animationIndex];
            _animationCurrentFrame = (_animationCurrentFrame + 1) % anim.FrameCount;
            Raylib.UpdateModelAnimationBones(_model, anim, _animationCurrentFrame);
            _lastFrameTime = Raylib.GetTime() + _secondsPerFrame;
        }
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
        Raylib.UnloadShader(_skinningShader);
    }
}