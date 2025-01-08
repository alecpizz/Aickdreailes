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
        ShaderManager.AddLight(new Light(type: LightType.Directional, enabled: true, position: Vector3.Zero,
            target: new Vector3(1.0F, -1.0F, 1.0F), color: Color.White));
        _entities.Add(new RagdollEntity("Ragdoll", new Vector3(0f, 4f, 0f)));
     
        _entities.Add(new PhysicsEntity(
            Path.Combine("Resources", "Models", "ConeTest", "ConeTestModel.gltf"),
            new Transform()
            {
                Rotation = Quaternion.Identity,
                Scale = Vector3.One * 3,
                Translation = new Vector3(-5.0f, 0.0f, 0.0f)
            },
            new Vector3(0F,-.140f, 0.0F),
            "Cone"
        ));
        
        _entities.Add(new PhysicsEntity(
            Path.Combine("Resources", "Models", "AlarmClockTest", "alarm_clock.gltf"),
            new Transform()
            {
                Rotation = Quaternion.Identity,
                Scale = Vector3.One * 3,
                Translation = new Vector3(-3.0f, 0.0f, 0.0f)
            }, Vector3.UnitY * -.15f, "Clock"
        ));
        //gm big city
        _entities.Add(new StaticEntityPBR(
            Path.Combine("Resources","Models","GM Big City","scene.gltf"), 
            Vector3.Zero
        ));
        // _entities.Add(new RagdollEntity(Path.Combine("Resources", "Models", "motorman.glb")));
        //player
        _entities.Add(new PlayerEntity(new Vector3(2.0f, 4.0f, 6.0f)));
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
        _shadowMapShader = LoadShader(Path.Combine("Resources", "Shaders", "shadowmap.vert"), Path.Combine("Resources", "Shaders", "shadowmap.frag"));
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

            ClearBackground(Color.RayWhite);

            BeginMode3D(Camera);
            ShaderManager.OnUpdate();
            
            foreach (var entity in _entities)
            {
                entity.OnRender(null);
            }
   

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

        ImGUIUtils.ClearFields();
        UnloadSound(_sound);
        AudioManager.ExitProgram();
        CloseWindow();
    }
}