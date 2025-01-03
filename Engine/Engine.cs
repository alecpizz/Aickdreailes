using System.Numerics;
using Engine.Entities;
using ImGuiNET;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;
using rlImGui_cs;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine;

public class Engine
{
    private bool _exitWindow;
    private float _currentTime;
    private float _accumulator;
    private Sound _sound;
    private float _t;
    private static bool _uiActive;
    private List<Entity> _entities = new List<Entity>();
    public static World PhysicsWorld = new World();
    public static PhysDrawer PhysDrawer = new PhysDrawer();
    public static Camera3D Camera;
    public static bool UIActive => _uiActive;
    private bool _cursorActive = false;
    private bool _firstCursor = false;

    public Engine()
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
        InitWindow(screenWidth, screenHeight, "My Window!");
        InitAudioDevice();
        int fps = GetMonitorRefreshRate(GetCurrentMonitor());
        SetTargetFPS(fps);

        _sound = LoadSound(Path.Combine("Resources", "Sounds", "tada.mp3"));

        Camera = new Camera3D()
        {
            Position = new Vector3(2.0f, 4.0f, 6.0f),
            Target = new Vector3(0.0f, 0.5f, 0.0f),
            Up = new Vector3(0.0f, 1.0f, 0.0f),
            FovY = 45.0f,
            Projection = CameraProjection.Perspective
        };

        PhysicsWorld.SubstepCount = 4;
        
        // TODO: This breaks on Mac!
        OpenTK.Graphics.GLLoader.LoadBindings(
            new OpenTKBindingContext()
        );

        SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();
        ImGUIUtils.SetupSteamTheme();


        float t = 0.0f;
        float dt = 1.0f / fps;

        _currentTime = (float)GetTime();
        
        // Skybox
        SkyboxEntityPBR skybox = new SkyboxEntityPBR(
            Path.Combine("Resources", "Textures", "petit_port_2k.hdr")
        );
        _entities.Add(skybox);
        
        // Lighting
        Light[] lights =
        [
            Light.CreateLight(
                LightType.Directional,
                Vector3.Zero,
                new Vector3(1.0F, -1.0F, 1.0F),
                Color.White,
                skybox.GetShader()
            )
        ];
        
        // Alarm clock render test
        _entities.Add(new StaticEntityPBR(
            Path.Combine("Resources", "Models", "AlarmClockTest", "alarm_clock.gltf"),
            new Vector3(-3.0F, 0.0F, 0.0F),
            skybox,
            lights
        ));
        
        // Cone render test
        _entities.Add(new StaticEntityPBR(
            Path.Combine("Resources", "Models", "ConeTest", "ConeTestModel.gltf"),
            new Vector3(-5.0F, 0.0F, 0.0F),
            skybox,
            lights
        ));
        
        //gm big city
        _entities.Add(new StaticEntityPBR(
            Path.Combine("Resources","Models","GM Big City","scene.gltf"), 
            Vector3.Zero,
            skybox,
            lights
        ));
        _entities.Add(new RagdollEntity(Path.Combine("Resources", "Models", "motorman.glb")));
        //player
        _entities.Add(new PlayerEntity(new Vector3(2.0f, 4.0f, 6.0f)));
        _entities.Add(new ViewModelEntity(Path.Combine("Resources", "Models", "USP", "scene.gltf"), (PlayerEntity)_entities[^1]));

        Image image = LoadImage(Path.Combine("Resources", "Textures", "icon.png"));
        SetWindowIcon(image);
        UnloadImage(image);
        Time.FixedDeltaTime = dt;
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

            _accumulator += frameTime;
            //physics updates
            while (_accumulator >= Time.FixedDeltaTime)
            {
                _t += Time.FixedDeltaTime;
                PhysicsWorld.Step(Time.FixedDeltaTime, true);
                _accumulator -= Time.FixedDeltaTime;
            }

            //player
            foreach (var entity in _entities)
            {
                entity.OnUpdate();
            }


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
                    _uiActive = !_uiActive;
                }
            }


            BeginDrawing();

            ClearBackground(Color.RayWhite);

            BeginMode3D(Camera);

            foreach (var entity in _entities)
            {
                entity.OnRender();
            }

            EndMode3D();

            foreach (var entity in _entities)
            {
                entity.OnPostRender();
            }

            rlImGui.Begin();
            if (_uiActive && ImGui.Begin("who needs an engine Engine", ref _uiActive, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Goober"))
                    {
                        if (ImGui.MenuItem("Close"))
                        {
                            _uiActive = false;
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

            if (_uiActive)
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
        CloseAudioDevice();
        CloseWindow();
    }
}