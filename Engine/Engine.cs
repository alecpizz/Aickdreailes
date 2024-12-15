using System.Numerics;
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
    private PhysDrawer _physDrawer;
    private Player _player;
    private World _world;
    private Sound _sound;
    private Model _city;
    private Shader _skyShader;
    private Model _skyBox;
    private Image _skyTexture;
    private Texture2D _cubeMap;
    private float _t;
    private Camera3D _camera;
    private bool _uiActive;
    private RigidBody _cityBody;
    private PlayerRayCaster _playerRayCaster;
    private bool _firstMouse = false;

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

        _camera = new Camera3D()
        {
            Position = new Vector3(2.0f, 4.0f, 6.0f),
            Target = new Vector3(0.0f, 0.5f, 0.0f),
            Up = new Vector3(0.0f, 1.0f, 0.0f),
            FovY = 45.0f,
            Projection = CameraProjection.Perspective
        };

        _world = new World();
        _world.SubstepCount = 4;


        for (int i = 0; i < 20; i++)
        {
            RigidBody body = _world.CreateRigidBody();
            body.AddShape(new BoxShape(1));
            body.Position = new JVector(0, i * 2 + 0.5f, 0);
        }

        SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();
        ImGUIUtils.SetupSteamTheme();
        

        float t = 0.0f;
        float dt = 1.0f / fps;

        _currentTime = (float)GetTime();
   

        _city = LoadModel(@"Resources\Models\GM Big City\scene.gltf");
        for (int i = 0; i < _city.MaterialCount; i++)
        {
            if (_city.Materials[i].Maps != null)
            {
                _city.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture.Mipmaps = 4;
                GenTextureMipmaps(_city.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture);
                SetTextureFilter(_city.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture,
                    TextureFilter.Trilinear);
            }
        }
       

        _cityBody = _world.CreateRigidBody();
        List<JTriangle> tris = new List<JTriangle>();

        for (int i = 0; i < _city.MeshCount; i++)
        {
            var mesh = _city.Meshes[i];
            Vector3* vertdata = (Vector3*)mesh.Vertices;
            if (mesh.Indices != null)
            {
                for (int j = 0; j < mesh.TriangleCount; j++)
                {
                    JVector a = vertdata[mesh.Indices[j * 3 + 0]].ToJVector();
                    JVector b = vertdata[mesh.Indices[j * 3 + 1]].ToJVector();
                    JVector c = vertdata[mesh.Indices[j * 3 + 2]].ToJVector();
                    JVector normal = (c - b) % (a - b);

                    if (MathHelper.CloseToZero(normal, 1e-12f))
                    {
                        continue;
                    }

                    tris.Add(new JTriangle(b, a, c));
                }
            }
        }

        var jtm = new TriangleMesh(tris);
        List<RigidBodyShape> triangleShapes = new List<RigidBodyShape>();
        for (int i = 0; i < jtm.Indices.Length; i++)
        {
            TriangleShape ts = new TriangleShape(jtm, i);
            triangleShapes.Add(ts);
        }


        _cityBody.AddShape(triangleShapes, false);
        _cityBody.Position = _city.Transform.Translation.ToJVector();

        _cityBody.IsStatic = true;
        _physDrawer = new PhysDrawer();
        _player = new Player(_world, _camera.Position.ToJVector());
        _playerRayCaster = new PlayerRayCaster(_world);
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
                _world.Step(Time.FixedDeltaTime, true);
                _accumulator -= Time.FixedDeltaTime;
            }

            //player
            _player?.Update(ref _camera);
            _playerRayCaster.Update(_camera);

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
                    RigidBody body = _world.CreateRigidBody();
                    body.AddShape(new BoxShape(1));
                    body.Position = _camera.Position.ToJVector();
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

            BeginMode3D(_camera);

            //funny skybox
            Rlgl.DisableBackfaceCulling();
            Rlgl.DisableDepthMask();
            DrawModel(_skyBox, Vector3.Zero, 1.0f, Color.White);
            Rlgl.EnableBackfaceCulling();
            Rlgl.EnableDepthMask();


            foreach (var body in _world.RigidBodies)
            {
                if (body == _world.NullBody || body == _cityBody || body == _player?.Body)
                    continue; // do not draw this
                body.DebugDraw(_physDrawer);
            }

            DrawModelEx(_city, Vector3.Zero,
                Vector3.UnitY, 0.0f, Vector3.One, Color.White);

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
                    RigidBody body = _world.CreateRigidBody();
                    body.AddShape(new BoxShape(1));
                    body.Position = new JVector(0, 10, 0);
                }

                if (ImGui.Button("Respawn Player"))
                {
                    if (_player != null)
                    {
                        _world.Remove(_player.Body);
                    }

                    _camera.Position = new Vector3(2.0f, 4.0f, 6.0f);
                    _player = new Player(_world, new JVector(2.0f, 4.0f, 6.0f));
                }
            }

            DrawFPS(10, 10);

            DrawText($"Player Velocity {_player.Body.Velocity.Length()}", 10, 20, 20, Color.White);

            ImGui.End();
            rlImGui.End();

            EndDrawing();
        }
    }

    public void Cleanup()
    {
        UnloadShader(_skyShader);
        UnloadImage(_skyTexture);
        UnloadTexture(_cubeMap);
        UnloadSound(_sound);
        CloseAudioDevice();
        CloseWindow();
    }
}