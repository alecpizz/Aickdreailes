using System.Numerics;
using ImGuiNET;
using Jitter2;
using Raylib_cs.BleedingEdge;
using rlImGui_cs;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            const int screenWidth = 1280;
            const int screenHeight = 720;
            const int fps = 200;
            
            SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
            InitWindow(screenWidth, screenHeight, "My Window!");
            InitAudioDevice();
            SetTargetFPS(fps);

            Sound sound = LoadSound(@"Resources\Sounds\tada.mp3");
            PlaySound(sound);
            Camera3D camera = new Camera3D()
            {
                Position = new Vector3(2.0f, 4.0f, 6.0f),
                Target = new Vector3(0.0f, 0.5f, 0.0f),
                Up = new Vector3(0.0f, 1.0f, 0.0f),
                FovY = 45.0f,
                Projection = CameraProjection.Perspective
            };

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
            
            
            double t = 0.0;
            double dt = 1.0 / fps;

            double currentTime = GetTime();
            double accumulator = 0.0;
            
            
            while (!WindowShouldClose() && !exitWindow)
            {
                double newTime = GetTime();
                double frameTime = newTime - currentTime;
                if (frameTime > 0.25)
                    frameTime = 0.25f;
                currentTime = newTime;

                accumulator += frameTime;
                while (accumulator >= dt)
                {
                    t += dt;
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