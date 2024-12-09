using System.Numerics;
using ImGuiNET;
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

            SetConfigFlags(ConfigFlags.Msaa4XHint | ConfigFlags.VSyncHint | ConfigFlags.WindowResizable);
            InitWindow(screenWidth, screenHeight, "My Window!");

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
            SetupImGuiStyling(0.75f);
            bool active = true;
            Vector4 color = default;
            bool exitWindow = false;

            while (!WindowShouldClose() && !exitWindow)
            {
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
                DrawCube(Vector3.Zero, 2.0f, 4.0f, 2.0f, Color.White);

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
            CloseWindow();
        }

        private static void SetupImGuiStyling(float alphaThreshold)
        {
            var style = ImGui.GetStyle();
            style.Colors[(int)ImGuiCol.Text] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.94f, 0.94f, 0.94f, 0.94f);
            style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(1.00f, 1.00f, 1.00f, 0.94f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0.00f, 0.00f, 0.00f, 0.39f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(1.00f, 1.00f, 1.00f, 0.10f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(1.00f, 1.00f, 1.00f, 0.94f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.40f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
            style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.96f, 0.96f, 0.96f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(1.00f, 1.00f, 1.00f, 0.51f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.82f, 0.82f, 0.82f, 1.00f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.98f, 0.98f, 0.98f, 0.53f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.69f, 0.69f, 0.69f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.59f, 0.59f, 0.59f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.49f, 0.49f, 0.49f, 1.00f);
            style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.24f, 0.52f, 0.88f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.Button] = new Vector4(0.26f, 0.59f, 0.98f, 0.40f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.06f, 0.53f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.Header] = new Vector4(0.26f, 0.59f, 0.98f, 0.31f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.50f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
            style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);

            for (ImGuiCol i = 0; i < ImGuiCol.COUNT; i++)
            {
                var color = style.Colors[(int)i];
                ImGui.ColorConvertRGBtoHSV(color.X, color.Y, color.Z, out float h, out float s, out float v);
                if (s < 0.1f)
                {
                    v = 1.0f - v;
                }
                ImGui.ColorConvertHSVtoRGB(h, s, v, out float r, out float g, out float b);
                color.X = r;
                color.Y = g;
                color.Z = b;
                if (color.W < alphaThreshold || i == ImGuiCol.FrameBg || i == ImGuiCol.WindowBg ||
                    i == ImGuiCol.ChildBg)
                {
                    color.W *= alphaThreshold;
                }
                style.Colors[(int)i] = color;
            }

            style.ChildBorderSize = 1.0f;
            style.FrameBorderSize = 0.0f;
            style.PopupBorderSize = 1.0f;
            style.WindowBorderSize = 0.0f;
            style.FrameRounding = 3.0f;
            style.Alpha = 1.0f;
            style.ChildRounding = 3.0f;
            style.WindowRounding = 3.0f;
        }
    }
}