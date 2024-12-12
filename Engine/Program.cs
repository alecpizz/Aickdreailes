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
        static Matrix4x4 GetRayLibTransformMatrix(RigidBody body)
        {
            JMatrix ori = JMatrix.CreateFromQuaternion(body.Orientation);
            JVector pos = body.Position;

            return new Matrix4x4(ori.M11, ori.M12, ori.M13, pos.X,
                ori.M21, ori.M22, ori.M23, pos.Y,
                ori.M31, ori.M32, ori.M33, pos.Z,
                0, 0, 0, 1.0f);
        }

        

        public static void Main(string[] args)
        {
            const int screenWidth = 1280;
            const int screenHeight = 720;

            SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
            InitWindow(screenWidth, screenHeight, "My Window!");
            InitAudioDevice();
            int fps = GetMonitorRefreshRate(GetCurrentMonitor());
            SetTargetFPS(fps);

            Mesh boxMesh = GenMeshCube(1, 1, 1);
            Material boxMat = LoadMaterialDefault();

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
            // RigidBody plane = world.CreateRigidBody();
            // plane.AddShape(new BoxShape(10f, 0.2f, 10f));
            // plane.Position = new JVector(0, -0.1f, 0);
            // plane.IsStatic = true;

            for (int i = 0; i < 20; i++)
            {
                RigidBody body = world.CreateRigidBody();
                body.AddShape(new BoxShape(1));
                body.Position = new JVector(0, i * 2 + 0.5f, 0);
            }

            Shader shader = LoadShader(@"Resources\Shaders\lighting.vert",
                @"Resources\Shaders\lighting.frag");
            boxMat.Shader = shader;
            SetShaderValue(shader, GetShaderLocation(shader, "ambient"),
                new Vector4(0.1f, 0.1f, 0.1f, 1.0f),
                ShaderUniformDataType.Vec4);

            Light[] lights = new Light[4];
            lights[0] = Light.CreateLight(LightType.Point, new Vector3(-2, 1, -2), Vector3.Zero,
                Color.Yellow, shader);
            lights[1] = Light.CreateLight(LightType.Point, new Vector3(2, 1, 2), Vector3.Zero,
                Color.Red, shader);
            lights[2] = Light.CreateLight(LightType.Point, new Vector3(-2, 1, 2), Vector3.Zero,
                Color.Green, shader);
            lights[3] = Light.CreateLight(LightType.Point, new Vector3(2, 1, -2), Vector3.Zero,
                Color.Blue, shader);

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
                        JVector normal = (c - b) % (a- b);

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

            Time.FixedDeltaTime = dt;
            while (!WindowShouldClose() && !exitWindow)
            {
                float newTime = (float)GetTime();
                float frameTime = newTime - currentTime;
                if (frameTime > 0.25)
                    frameTime = 0.25f;
                Time.DeltaTime = frameTime;
                currentTime = newTime;

                accumulator += frameTime;
                while (accumulator >= dt)
                {
                    t += dt;
                    world.Step(dt, true);
                    accumulator -= dt;
                }

                player?.Update(ref camera);
                if (ImGui.GetIO().WantCaptureMouse || active)
                {
                }
                else
                {
                    // UpdateCamera(ref camera, CameraMode.Custom);
                }

                SetShaderValue(shader, GetShaderLocation(shader, "viewPos"),
                    camera.Position, ShaderUniformDataType.Vec3);

                if (!ImGui.GetIO().WantCaptureKeyboard)
                {
                    if (IsKeyPressed(KeyboardKey.Y))
                    {
                        lights[0].Enabled = !lights[0].Enabled;
                    }

                    if (IsKeyPressed(KeyboardKey.R))
                    {
                        lights[1].Enabled = !lights[1].Enabled;
                    }

                    if (IsKeyPressed(KeyboardKey.G))
                    {
                        lights[2].Enabled = !lights[2].Enabled;
                    }

                    if (IsKeyPressed(KeyboardKey.B))
                    {
                        lights[3].Enabled = !lights[3].Enabled;
                    }

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

                for (int i = 0; i < 4; i++)
                {
                    Light.UpdateLightValues(shader, lights[i]);
                }


                BeginDrawing();

                ClearBackground(Color.Black);

                BeginMode3D(camera);

                // BeginShaderMode(shader);
                //
                // DrawPlane(Vector3.Zero, new Vector2(10.0f, 10.0f), Color.White);
                // DrawCube(Vector3.Zero, 1.0f, 1.0f, 1.0f, Color.White);
                //
                //
                // EndShaderMode();

                foreach (var body in world.RigidBodies)
                {
                    if (body == world.NullBody || body == citybody || body == player?.Body) continue; // do not draw this
                    body.DebugDraw(physDrawer);
                }

                DrawModelEx(city, new Vector3(citybody.Position.X, citybody.Position.Y, citybody.Position.Z),
                    Vector3.UnitY, 0.0f,  Vector3.One, Color.White);

                // Draw spheres to show where the lights are
                for (int i = 0; i < 4; i++)
                {
                    if (lights[i].Enabled) DrawSphereEx(lights[i].Position, 0.2f, 8, 8, lights[i].Color);
                    else DrawSphere(lights[i].Position, 0.2f, ColorAlpha(lights[i].Color, 0.3f));
                }


                DrawGrid(10, 1.0f);

                // DrawModel(city, Vector3.Zero, 1.0f, Color.White);
                // DrawModelWires(city, Vector3.Zero, 1.0f, Color.Black);
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

            UnloadShader(shader);
            UnloadSound(sound);
            CloseAudioDevice();
            CloseWindow();
        }
    }
}