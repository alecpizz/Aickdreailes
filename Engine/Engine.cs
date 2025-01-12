using System.Numerics;
using Engine.Entities;
using Engine.Rendering;
using ImGuiNET;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using rlImGui_cs;
using static Raylib_cs.Raylib;
using ShaderType = Engine.Rendering.ShaderType;

namespace Engine;

public class Engine
{
    private bool _exitWindow;
    private float _currentTime;
    private Sound _sound;
    private float _t;
    private static bool _inEditor;
    private List<Entity> _entities = new List<Entity>();
    public static World PhysicsWorld = new World();
    public static PhysDrawer PhysDrawer = new PhysDrawer();
    public static Camera3D Camera;
    public static ShaderManager ShaderManager;
    public static bool InEditor => _inEditor;
    private bool _cursorActive = false;
    private bool _firstCursor = false;
    private Shader _shadowMapShader;
    private RenderTexture2D _shadowMap;

    public Engine()
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "My Window!");
        AudioManager.InitializeAudio();
        int fps = GetMonitorRefreshRate(GetCurrentMonitor());
        SetTargetFPS(fps);

        _sound = LoadSound(Path.Combine("Resources", "Sounds", "Sound Effects", "tada.mp3"));


        Camera = new()
        {
            Position = new Vector3(2.0f, 4.0f, 6.0f),
            Target = new Vector3(0.0f, 0.5f, 0.0f),
            Up = new Vector3(0.0f, 1.0f, 0.0f),
            FovY = 45.0f,
            Projection = CameraProjection.Perspective
        };

        PhysicsWorld.SubstepCount = 4;
        PhysicsWorld.SolverIterations = (20, 20);


        OpenTK.Graphics.GLLoader.LoadBindings(new OpenTKBindingContext());

        SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();
        ImGUIUtils.SetupSteamTheme();


        float t = 0.0f;
        float dt = 1.0f / fps;

        _currentTime = (float)GetTime();
        //skybox
        var skybox = new SkyboxEntityPBR(Path.Combine("Resources", "Textures", "petit_port_2k.hdr"));
        _entities.Add(skybox);

        ShaderManager = new ShaderManager(skybox);
        ShaderManager.AddLight(new Light(type: LightType.Directional, enabled: true, position: new Vector3(0f, 25f, 0f),
            target: new Vector3(-54f, -11f, -86f), color: Color.White));
        _entities.Add(new RagdollEntity("Ragdoll", new Vector3(10f, 4f, 0f)));

        _entities.Add(new PhysicsEntity(
            Path.Combine("Resources", "Models", "ConeTest", "ConeTestModel.gltf"),
            new Transform()
            {
                Rotation = Quaternion.Identity,
                Scale = Vector3.One * 3,
                Translation = new Vector3(11f, 0.0f, 0.0f)
            },
            new Vector3(0F, -.140f, 0.0F),
            "Cone"
        ));

        _entities.Add(new PhysicsEntity(
            Path.Combine("Resources", "Models", "AlarmClockTest", "alarm_clock.gltf"),
            new Transform()
            {
                Rotation = Quaternion.Identity,
                Scale = Vector3.One * 3,
                Translation = new Vector3(10f, 0.0f, 0.0f)
            }, Vector3.UnitY * -.15f, "Clock"
        ));
        //gm big city
        _entities.Add(new StaticEntityPBR(
            Path.Combine("Resources", "Models", "dust2.glb"),
            Vector3.UnitY * -5f
        ));
        // _entities.Add(new RagdollEntity(Path.Combine("Resources", "Models", "motorman.glb")));
        //player
        _entities.Add(new PlayerEntity(new Vector3(12.0f, -4.0f, 6.0f)));
        _entities.Add(new ViewModelEntity(Path.Combine("Resources", "Models", "rifle.glb"),
            (PlayerEntity)_entities[^1]));
        _entities.Add(new PhysicsEntity(Path.Combine("Resources", "Models", "USP", "scene.gltf"),
            new Transform()
            {
                Rotation = Quaternion.Identity,
                Scale = Vector3.One * 0.1f,
                Translation = new Vector3(0.5f, 0f, -2.1f)
            },
            new Vector3(0, 0f, -1.9f), "USP"));
        Image image = LoadImage(Path.Combine("Resources", "Textures", "icon.png"));
        SetWindowIcon(image);
        UnloadImage(image);
        Time.FixedDeltaTime = 1.0f / 60f;
        _shadowMapShader = LoadShader(Path.Combine("Resources", "Shaders", "shadow.vert"),
            Path.Combine("Resources", "Shaders", "shadow.frag"));
        _shadowMap = LoadShadowMap(2048, 2048);
    }

    private RenderTexture2D LoadShadowMap(int width, int height)
    {
        RenderTexture2D target = new();
        target.Id = Rlgl.LoadFramebuffer();
        target.Texture.Width = width;
        target.Texture.Width = height;
        if (target.Id > 0)
        {
            Rlgl.EnableFramebuffer(target.Id);

            target.Depth.Id = Rlgl.LoadTextureDepth(width, height, false);
            target.Depth.Width = width;
            target.Depth.Height = height;
            target.Depth.Format = PixelFormat.CompressedEtc2Rgb;
            target.Depth.Mipmaps = 1;

            Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth,
                FramebufferAttachTextureType.Texture2D, 0);
            if (Rlgl.FramebufferComplete(target.Id))
            {
                Logging.LogSuccess($"Framebuffer object created with id {target.Id}");
            }

            Rlgl.DisableFramebuffer();
        }
        else
        {
            Logging.LogWarning($"Framebuffer object can't be created...");
        }

        return target;
    }

    private void UnloadShadowMap(RenderTexture2D target)
    {
        if (target.Id > 0)
        {
            Rlgl.UnloadFramebuffer(target.Id);
        }
    }

    public void Run()
    {
        while (!WindowShouldClose() && !_exitWindow)
        {
            //delta time
            float newTime = (float)GetTime();
            float frameTime = newTime - _currentTime;
            if (frameTime > 0.25)
                frameTime = 0.25f;
            Time.DeltaTime = frameTime;
            _currentTime = newTime;

            Time.AccumulationTime += frameTime;
            //physics updates
            while (Time.AccumulationTime >= Time.FixedDeltaTime)
            {
                _t += Time.FixedDeltaTime;
                PhysicsWorld.Step(Time.FixedDeltaTime, true);
                foreach (var entity in _entities)
                {
                    entity.OnFixedUpdate();
                }

                Time.AccumulationTime -= Time.FixedDeltaTime;
            }

            //player
            foreach (var entity in _entities)
            {
                entity.OnUpdate();
            }

            //music
            AudioManager.UpdateAudio();
            // Bruh!

            if (!ImGui.GetIO().WantCaptureKeyboard)
            {
                if (IsKeyPressed(KeyboardKey.E))
                {
                    RigidBody body = PhysicsWorld.CreateRigidBody();
                    body.AddShape(new BoxShape(1));
                    body.Position = Camera.Position.ToJVector();
                }

                if (IsKeyPressed(KeyboardKey.Escape))
                {
                    _inEditor = !_inEditor;
                }
            }


            BeginDrawing();

            //shadow
            Matrix4x4 lightView;
            Matrix4x4 lightProj;
            BeginTextureMode(_shadowMap);
            {
                ClearBackground(Color.White);
                BeginMode3D(new Camera3D(
                    position: ShaderManager.DirectionalLight.Position,
                    target: ShaderManager.DirectionalLight.Target,
                    up: Vector3.UnitY, fovY: 20f,
                    projection: CameraProjection.Orthographic));
                {
                    lightView = Rlgl.GetMatrixModelview();
                    lightProj = Rlgl.GetMatrixProjection();
                    foreach (var entity in _entities)
                    {
                        entity.OnRender(_shadowMapShader);
                    }
                }
                EndMode3D();
            }
            EndTextureMode();

            Matrix4x4 lightViewProj = Raymath.MatrixMultiply(lightView, lightProj);

            ClearBackground(Color.RayWhite);
            ShaderManager.OnUpdate();
            SetShaderValueMatrix(ShaderManager[ShaderType.Static],
                GetShaderLocation(ShaderManager[ShaderType.Static], "lightVP"), lightViewProj);
            SetShaderValueTexture(ShaderManager[ShaderType.Static],
                GetShaderLocation(ShaderManager[ShaderType.Static], "shadowMap"), _shadowMap.Depth);
            SetShaderValueMatrix(ShaderManager[ShaderType.Skinned],
                GetShaderLocation(ShaderManager[ShaderType.Skinned], "lightVP"), lightViewProj);
            SetShaderValueTexture(ShaderManager[ShaderType.Skinned],
                GetShaderLocation(ShaderManager[ShaderType.Skinned], "shadowMap"), _shadowMap.Depth);
            BeginMode3D(Camera);
            foreach (var entity in _entities)
            {
                entity.OnRender(null);
            }
            DrawSphere(ShaderManager.DirectionalLight.Position, 0.2f, Color.White);
            DrawRay(new Ray(ShaderManager.DirectionalLight.Position, Vector3.Normalize(ShaderManager.DirectionalLight.Target)), Color.White);

            EndMode3D();

            foreach (var entity in _entities)
            {
                entity.OnPostRender();
            }

            rlImGui.Begin();
            if (_inEditor && ImGui.Begin("who needs an engine Engine", ref _inEditor, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Goober"))
                    {
                        if (ImGui.MenuItem("Close"))
                        {
                            _inEditor = false;
                        }

                        if (ImGui.MenuItem("Quit Program"))
                        {
                            _exitWindow = true;
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenuBar();
                }

                if (ImGui.Button("Play Sound"))
                {
                    PlaySound(_sound);
                    //PlaySound(AudioManager._allSFX[0]._sound);
                }

                if (ImGui.Button("Spawn Cube"))
                {
                    RigidBody body = PhysicsWorld.CreateRigidBody();
                    body.AddShape(new BoxShape(1));
                    body.Position = new JVector(0, 10, 0);
                }

                if (ImGui.Button("Respawn Player"))
                {
                    foreach (var entity in _entities)
                    {
                        if (entity is not PlayerEntity player) continue;
                        player.Teleport(new Vector3(2.0f, 4.0f, 6.0f));
                        break;
                    }
                }

                ShaderManager.OnImGui();

                foreach (var entity in _entities)
                {
                    if (ImGui.CollapsingHeader(entity.Name))
                    {
                        entity.OnImGuiWindowRender();
                    }
                }
            }

            if (!_firstCursor)
            {
                DisableCursor();
                _firstCursor = true;
            }

            if (_inEditor)
            {
                if (!_cursorActive)
                {
                    EnableCursor();
                    _cursorActive = true;
                }
            }
            else
            {
                if (_cursorActive)
                {
                    DisableCursor();
                    _cursorActive = false;
                }
            }

            foreach (var entity in _entities)
            {
                entity.OnUIRender();
            }

            DrawFPS(10, 10);

            ImGui.End();
            rlImGui.End();

            EndDrawing();
        }
    }

    public void Cleanup()
    {
        foreach (var entity in _entities)
        {
            entity.OnCleanup();
        }

        UnloadShadowMap(_shadowMap);
        UnloadShader(_shadowMapShader);
        ImGUIUtils.ClearFields();
        UnloadSound(_sound);
        AudioManager.ExitProgram();
        CloseWindow();
    }
}