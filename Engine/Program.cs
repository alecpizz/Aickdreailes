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
    public static class Program
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
            const int fps = 200;
            
            SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
            InitWindow(screenWidth, screenHeight, "My Window!");
            InitAudioDevice();
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
            RigidBody plane = world.CreateRigidBody();
            plane.AddShape(new BoxShape(10));
            plane.Position = new JVector(0, -5f, 0);
            plane.IsStatic = true;
            
            for(int i = 0; i < 20; i++)
            {
                RigidBody body = world.CreateRigidBody();
                body.AddShape(new BoxShape(1));
                body.Position = new JVector(0, i * 2 + 0.5f, 0);
            }
            
            Shader shader = LoadShader(@"Resources\Shaders\lighting.vert",
                @"Resources\Shaders\lighting.frag");

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
            ImGUIUtils.SetupModernTheme(0.75f);
            bool active = true;
            Vector4 color = default;
            bool exitWindow = false;
            
            
            float t = 0.0f;
            float dt = 1.0f / fps;

            float currentTime = (float)GetTime();
            float accumulator = 0.0f;
            
            
            while (!WindowShouldClose() && !exitWindow)
            {
                float newTime = (float)GetTime();
                float frameTime = newTime - currentTime;
                if (frameTime > 0.25)
                    frameTime = 0.25f;
                currentTime = newTime;

                accumulator += frameTime;
                while (accumulator >= dt)
                {
                    t += dt;
                    world.Step(dt, true);
                    accumulator -= dt;
                }
                
                if (ImGui.GetIO().WantCaptureMouse)
                {
                    if (GetMouseWheelMove() == 0)
                    {
                        UpdateCamera(ref camera, CameraMode.Orbital);
                    }
                }
                else
                {
                    UpdateCamera(ref camera, CameraMode.Orbital);
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

                    if (IsKeyPressed(KeyboardKey.Escape))
                    {
                        active = !active;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    Light.UpdateLightValues(shader, lights[i]);
                }

                
                BeginDrawing();

                ClearBackground(Color.RayWhite);

                BeginMode3D(camera);

                BeginShaderMode(shader);

                DrawPlane(Vector3.Zero, new Vector2(10.0f, 10.0f), Color.White);
                DrawCube(Vector3.Zero, 1.0f, 1.0f, 1.0f, Color.White);

                
                EndShaderMode();
                
                foreach(var body in world.RigidBodies)
                {
                    if (body == plane || body == world.NullBody) continue; // do not draw this
                    DrawMesh(boxMesh, boxMat , GetRayLibTransformMatrix(body));
                }
              

                // Draw spheres to show where the lights are
                for (int i = 0; i < 4; i++)
                {
                    if (lights[i].Enabled) DrawSphereEx(lights[i].Position, 0.2f, 8, 8, lights[i].Color);
                    else DrawSphere(lights[i].Position, 0.2f, ColorAlpha(lights[i].Color, 0.3f));
                }
                
                
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
                }

                DrawFPS(10, 10);

                DrawText("Use keys [Y][R][G][B] to toggle lights", 10, 40, 20, Color.DarkGray);

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