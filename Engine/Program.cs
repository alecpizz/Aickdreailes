using System.Numerics;
using ImGuiNET;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;
using rlImGui_cs;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine
{
    public static unsafe class Program
    {
        public static void Main(string[] args)
        {
            const int screenWidth = 1280;
            const int screenHeight = 720;

            SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
            InitWindow(screenWidth, screenHeight, "My Window!");
            InitAudioDevice();
            int fps = GetMonitorRefreshRate(GetCurrentMonitor());
            SetTargetFPS(fps);
            
            Sound sound = LoadSound(@"Resources\Sounds\tada.mp3");
            
            Camera3D camera = new Camera3D()
            {
                Position = new Vector3(2.0f, 4.0f, 6.0f),
                Target = new Vector3(0.0f, 0.5f, 0.0f),
                Up = new Vector3(0.0f, 1.0f, 0.0f),
                FovY = 45.0f,
                Projection = CameraProjection.Perspective
            };

            World world = new World();
            world.SubstepCount = 4;
      

            for (int i = 0; i < 20; i++)
            {
                RigidBody body = world.CreateRigidBody();
                body.AddShape(new BoxShape(1));
                body.Position = new JVector(0, i * 2 + 0.5f, 0);
            }
            
            SetExitKey(KeyboardKey.Null);
            rlImGui.Setup();
            ImGUIUtils.SetupSteamTheme();
            bool active = true;
            Vector4 color = default;
            bool exitWindow = false;


            float t = 0.0f;
            float dt = 1.0f / fps;

            float currentTime = (float)GetTime();
            float accumulator = 0.0f;

            Model city = LoadModel(@"Resources\Models\GM Big City\scene.gltf");
            for (int i = 0; i < city.MaterialCount; i++)
            {
                if (city.Materials[i].Maps != null)
                {
                    SetTextureFilter(city.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture,
                        TextureFilter.Trilinear);
                }
            }

            RigidBody citybody = world.CreateRigidBody();
            List<JTriangle> tris = new List<JTriangle>();

            for (int i = 0; i < city.MeshCount; i++)
            {
                var mesh = city.Meshes[i];
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


            citybody.AddShape(triangleShapes, false);
            citybody.Position = city.Transform.Translation.ToJVector();

            citybody.IsStatic = true;
            PhysDrawer physDrawer = new PhysDrawer();
            Player player = new Player(world, camera.Position.ToJVector());

            Shader skyShader = LoadShader(@"Resources\Shaders\skybox.vert", @"Resources\Shaders\skybox.frag");
            Mesh cube = GenMeshCube(1.0f, 1.0f, 1.0f);
            Model skyBox = LoadModelFromMesh(cube);
            SetShaderValue(skyShader, GetShaderLocation(skyShader, "environmentMap"),
                (int)MaterialMapIndex.Cubemap, ShaderUniformDataType.Int);
            SetMaterialShader(ref skyBox, 0, ref skyShader);
            Image skyTexture = LoadImage(@"Resources\Textures\cubemap.png");
            Texture2D cubeMap = LoadTextureCubemap(skyTexture, CubemapLayout.AutoDetect);
            SetMaterialTexture(skyBox.Materials, MaterialMapIndex.Cubemap, cubeMap);
            Time.FixedDeltaTime = dt;
            
            while (!WindowShouldClose() && !exitWindow)
            {
                //delta time
                float newTime = (float)GetTime();
                float frameTime = newTime - currentTime;
                if (frameTime > 0.25)
                    frameTime = 0.25f;
                Time.DeltaTime = frameTime;
                currentTime = newTime;

                accumulator += frameTime;
                //physics updates
                while (accumulator >= Time.FixedDeltaTime)
                {
                    t += Time.FixedDeltaTime;
                    world.Step(dt, true);
                    accumulator -= Time.FixedDeltaTime;
                }

                //player
                player?.Update(ref camera);
               
                if (ImGui.GetIO().WantCaptureMouse || active)
                {
                }
                else
                {
                    // UpdateCamera(ref camera, CameraMode.Free);
                }

                if (!ImGui.GetIO().WantCaptureKeyboard)
                {
                    if (IsKeyPressed(KeyboardKey.E))
                    {
                        RigidBody body = world.CreateRigidBody();
                        body.AddShape(new BoxShape(1));
                        body.Position = camera.Position.ToJVector();
                    }

                    if (IsKeyPressed(KeyboardKey.Escape))
                    {
                        active = !active;
                        if (!active)
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

                BeginMode3D(camera);
                
                //funny skybox
                Rlgl.DisableBackfaceCulling();
                Rlgl.DisableDepthMask();
                DrawModel(skyBox, Vector3.Zero, 1.0f, Color.White);
                Rlgl.EnableBackfaceCulling();
                Rlgl.EnableDepthMask();
              

                foreach (var body in world.RigidBodies)
                {
                    if (body == world.NullBody || body == citybody || body == player?.Body)
                        continue; // do not draw this
                    body.DebugDraw(physDrawer);
                }

                DrawModelEx(city, Vector3.Zero,
                    Vector3.UnitY, 0.0f, Vector3.One, Color.White);

                DrawGrid(10, 1.0f);
                
                EndMode3D();

                rlImGui.Begin();
                if (active && ImGui.Begin("Fuck Unity", ref active, ImGuiWindowFlags.MenuBar))
                {
                    if (ImGui.BeginMenuBar())
                    {
                        if (ImGui.BeginMenu("Goon"))
                        {
                            if (ImGui.MenuItem("Close"))
                            {
                                active = false;
                            }

                            if (ImGui.MenuItem("Quit Program"))
                            {
                                exitWindow = true;
                            }

                            ImGui.EndMenu();
                        }

                        ImGui.EndMenuBar();
                    }

                    ImGui.ColorEdit4("Schmegma", ref color);
                    if (ImGui.Button("Play Sound"))
                    {
                        PlaySound(sound);
                    }

                    if (ImGui.Button("Spawn Cube"))
                    {
                        RigidBody body = world.CreateRigidBody();
                        body.AddShape(new BoxShape(1));
                        body.Position = new JVector(0, 10, 0);
                    }

                    if (ImGui.Button("Spawn Player"))
                    {
                        if (player != null)
                        {
                            world.Remove(player.Body);
                        }

                        player = new Player(world, camera.Position.ToJVector());
                    }
                }

                DrawFPS(10, 10);

                DrawText($"Player Velocity {player.Body.Velocity.Length()}", 10, 20, 20, Color.White);

                ImGui.End();
                rlImGui.End();

                EndDrawing();
            }

            UnloadShader(skyShader);
            UnloadImage(skyTexture);
            UnloadTexture(cubeMap);
            UnloadSound(sound);
            CloseAudioDevice();
            CloseWindow();
        }
    }
}