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

public unsafe class Engine
{
    private bool _exitWindow;
    private float _currentTime;
    private float _accumulator;
    private Sound _sound;
    private Shader _skyShader;
    private Model _skyBox;
    private Image _skyTexture;
    private Texture2D _cubeMap;
    private float _t;
    private bool _uiActive;
    private PlayerRayCaster _playerRayCaster;
    private bool _firstMouse = false;
    private List<Entity> _entities = new List<Entity>();
    public static World PhysicsWorld = new World();
    public static PhysDrawer PhysDrawer = new PhysDrawer();
    public static Camera3D Camera;
    public Engine()
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
        InitWindow(screenWidth, screenHeight, "My Window!");
        InitAudioDevice();
        int fps = GetMonitorRefreshRate(GetCurrentMonitor());
        SetTargetFPS(fps);

        _sound = LoadSound(@"Resources\Sounds\tada.mp3");

        Camera = new Camera3D()
        {
            Position = new Vector3(2.0f, 4.0f, 6.0f),
            Target = new Vector3(0.0f, 0.5f, 0.0f),
            Up = new Vector3(0.0f, 1.0f, 0.0f),
            FovY = 45.0f,
            Projection = CameraProjection.Perspective
        };

        PhysicsWorld.SubstepCount = 4;
        

        SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();
        ImGUIUtils.SetupSteamTheme();
        

        float t = 0.0f;
        float dt = 1.0f / fps;

        _currentTime = (float)GetTime();
   
        _entities.Add(new StaticEntity(@"Resources\Models\GM Big City\scene.gltf", Vector3.Zero));

        _entities.Add(new PlayerEntity(new Vector3(2.0f, 4.0f, 6.0f)));
        _playerRayCaster = new PlayerRayCaster(PhysicsWorld);
        _skyShader = LoadShader(@"Resources\Shaders\skybox.vert", @"Resources\Shaders\skybox.frag");
        Mesh cube = GenMeshCube(1.0f, 1.0f, 1.0f);
        _skyBox = LoadModelFromMesh(cube);
        SetShaderValue(_skyShader, GetShaderLocation(_skyShader, "environmentMap"),
            (int)MaterialMapIndex.Cubemap, ShaderUniformDataType.Int);
        SetMaterialShader(ref _skyBox, 0, ref _skyShader);
        _skyTexture = LoadImage(@"Resources\Textures\cubemap.png");
        _cubeMap = LoadTextureCubemap(_skyTexture, CubemapLayout.AutoDetect);
        SetMaterialTexture(_skyBox.Materials, MaterialMapIndex.Cubemap, _cubeMap);
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
            
            _playerRayCaster.Update(Camera);

            if (ImGui.GetIO().WantCaptureMouse || _uiActive)
            {
            }
            else
            {
                // UpdateCamera(ref camera, CameraMode.Free);
                if (!_firstMouse && IsMouseButtonPressed(MouseButton.Left))
                {
                    DisableCursor();
                    _firstMouse = true;
                }
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
                    if (!_uiActive)
                    {
                        DisableCursor();
                    }
                    else
                    {
                        EnableCursor();
                    }
                }
            }


            BeginDrawing();

            ClearBackground(Color.RayWhite);

            BeginMode3D(Camera);

            //funny skybox
            Rlgl.DisableBackfaceCulling();
            Rlgl.DisableDepthMask();
            DrawModel(_skyBox, Vector3.Zero, 1.0f, Color.White);
            Rlgl.EnableBackfaceCulling();
            Rlgl.EnableDepthMask();
            
            foreach (var entity in _entities)
            {
                entity.OnRender();
            }

            // foreach (var body in World.RigidBodies)
            // {
            //     if (body == World.NullBody || body == _cityBody || body == _player?.Body)
            //         continue; // do not draw this
            //     body.DebugDraw(_physDrawer);
            // }

            

            DrawGrid(10, 1.0f);

            foreach (var pt in _playerRayCaster._hitPoints)
            {
                DrawSphere(pt, 0.2f, Color.Red);
            }

            EndMode3D();

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
        UnloadShader(_skyShader);
        UnloadImage(_skyTexture);
        UnloadTexture(_cubeMap);
        UnloadSound(_sound);
        CloseAudioDevice();
        CloseWindow();
    }
}