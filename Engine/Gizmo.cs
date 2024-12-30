using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Raymath;
using static Raylib_cs.Rlgl;

namespace Engine;

public static class Gizmo
{
    [Flags]
    public enum GizmoFlags
    {
        GIZMO_DISABLED = 0, // 0: Disables gizmo drawing

        // Bitwise flags
        GIZMO_TRANSLATE = 1 << 0, // Enables translation gizmo
        GIZMO_ROTATE = 1 << 1, // Enables rotation gizmo
        GIZMO_SCALE = 1 << 2, // Enables scaling gizmo (implicitly enables GIZMO_LOCAL)
        GIZMO_ALL = GIZMO_TRANSLATE | GIZMO_ROTATE | GIZMO_SCALE, // Enables all gizmos

        // Mutually exclusive axis orientation flags
        // Default: Global axis orientation
        GIZMO_LOCAL = 1 << 3, // Orients axes locally
        GIZMO_VIEW = 1 << 4 // Orients axes based on screen view
    }

    public enum GizmoActiveAxis
    {
        GZ_ACTIVE_X = 1 << 0, // Active transformation on the X-axis
        GZ_ACTIVE_Y = 1 << 1, // Active transformation on the Y-axis
        GZ_ACTIVE_Z = 1 << 2, // Active transformation on the Z-axis
        GZ_ACTIVE_XYZ = GZ_ACTIVE_X | GZ_ACTIVE_Y | GZ_ACTIVE_Z // Active transformation on all axes
    }

    public enum GizmoAction
    {
        GZ_ACTION_NONE = 0, // No active transformation
        GZ_ACTION_TRANSLATE, // Translation (movement) transformation
        GZ_ACTION_SCALE, // Scaling transformation
        GZ_ACTION_ROTATE // Rotation transformation
    }

    public enum GizmoAxes
    {
        GZ_AXIS_X = 0, // Index of the X-axis
        GZ_AXIS_Y = 1, // Index of the Y-axis
        GZ_AXIS_Z = 2, // Index of the Z-axis

        GIZMO_AXIS_COUNT = 3
    }

    public struct GizmoAxis
    {
        public Vector3 normal;
        public Color color;
    }

    public class GizmoGlobals
    {
        public GizmoAxis[] axisCfg = new GizmoAxis[(int)GizmoAxes.GIZMO_AXIS_COUNT];
        public float gizmoSize;
        public float lineWidth;
        public float trArrowWidthFactor; // Width of the arrows (and cubes) as a fraction of gizmoSize.
        public float trArrowLengthFactor; // Length of the arrows as a fraction of gizmoSize.
        public float trPlaneOffsetFactor; // Offset of the gizmo planes from the center as a fraction of gizmoSize.
        public float trPlaneSizeFactor; // Size of the planes (quad representation) as a fraction of gizmoSize.
        public float trCircleRadiusFactor; // Radius of the central circle as a fraction of gizmoSize.
        public Color trCircleColor; // Color of the central circle.
        public int curAction; // Currently active GizmoAction.
        public int activeAxis; // Active axis (a combination of GizmoActiveAxis flags) for the current action.
        public Transform startTransform; // Backup Transform saved before the transformation begins.
        public TransformData activeTransform; // Pointer to the active Transform to update during transformation.
        public Vector3 startWorldMouse;
    }

    public class TransformData
    {
        public Vector3 Translation;
        public Vector3 Scale;
        public Quaternion Rotation;

        public TransformData()
        {
            Translation = Vector3.Zero;
            Scale = Vector3.One;
            Rotation = Quaternion.Identity;
        }

        public TransformData(Transform tr)
        {
            Translation = tr.Translation;
            Scale = tr.Scale;
            Rotation = tr.Rotation;
        }

        public TransformData(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }

        public TransformData(TransformData tr)
        {
            Translation = tr.Translation;
            Scale = tr.Scale;
            Rotation = tr.Rotation;
        }
        
    }

    public struct GizmoData(ref TransformData data)
    {
        public Matrix4x4 invViewProj = default; // Inverted View-Projection matrix.

        public TransformData
            curTransform = data; // Pointer to the current Transform. Only one can be the "activeTransform" at a time.

        public Vector3[]
            axis = new Vector3[(int)GizmoAxes
                .GIZMO_AXIS_COUNT]; // Current axes used for transformations (may differ from global axes).
        // Axes can be in global, view, or local mode depending on configuration.

        public float gizmoSize = 0; // Actual gizmo size, adjusted to maintain camera-independent scaling.
        public Vector3 camPos = default; // Position of the camera, extracted during rendering.
        public Vector3 right = default, up = default, forward = default; // Local orientation vectors: right, up, and forward.
        public int flags = 0;
    }

    public static GizmoGlobals GIZMO = new GizmoGlobals
    {
        axisCfg = new GizmoAxis[]
        {
            new GizmoAxis()
            {
                color = new Color(229, 72, 91, 255),
                normal = Vector3.UnitX
            },
            new GizmoAxis()
            {
                color = new Color(131, 205, 56, 255),
                normal = Vector3.UnitY
            },
            new GizmoAxis()
            {
                color = new Color(69, 138, 242, 255),
                normal = Vector3.UnitZ
            },
        },
        gizmoSize = 1.5f,
        lineWidth = 2.5f,
        trArrowWidthFactor = 0.15f,
        trArrowLengthFactor = 0.1f,
        trPlaneOffsetFactor = 0.3f,
        trPlaneSizeFactor = 0.15f,
        trCircleRadiusFactor = 0.1f,
        trCircleColor = new Color(255, 255, 255, 200),
        curAction = (int)GizmoAction.GZ_ACTION_NONE,
        activeAxis = 0,
        startTransform = default,
        activeTransform = default,
        startWorldMouse = default
    };

    public static bool DrawGizmo3D(int flags, ref TransformData transform)
    {
        if (flags == (int)GizmoFlags.GIZMO_DISABLED)
        {
            return false;
        }

        GizmoData data = new GizmoData();
        Matrix4x4 matProj = GetMatrixProjection();
        Matrix4x4 matView = GetMatrixModelview();
        Matrix4x4 invMat = MatrixInvert(matView);

        data.invViewProj = MatrixMultiply(MatrixInvert(matProj), invMat);

        data.camPos = new Vector3(invMat.M14, invMat.M24, invMat.M34);

        data.right = new Vector3(matView.M11, matView.M12, matView.M13);
        data.up = new Vector3(matView.M21, matView.M22, matView.M23);
        data.forward = Vector3Normalize(Vector3Subtract(transform.Translation, data.camPos));

        data.curTransform = transform;

        data.gizmoSize = GIZMO.gizmoSize * Vector3Distance(data.camPos, transform.Translation) * 0.1f;

        data.flags = flags;
        data.axis = new Vector3[3];

        ComputeAxisOrientation(ref data);

        DrawRenderBatchActive();
        float prevLineWidth = GetLineWidth();
        SetLineWidth(GIZMO.lineWidth);
        DisableBackfaceCulling();
        DisableDepthTest();
        DisableDepthMask();

        //------------------------------------------------------------------------

        for (int i = 0; i < (int)GizmoAxes.GIZMO_AXIS_COUNT; ++i)
        {
            if ((data.flags & (int)GizmoFlags.GIZMO_TRANSLATE) != 0)
            {
                DrawGizmoArrow(ref data, i);
            }

            if ((data.flags & (int)GizmoFlags.GIZMO_SCALE) != 0)
            {
                DrawGizmoCube(ref data, i);
            }

            if ((data.flags & ((int)GizmoFlags.GIZMO_SCALE | (int)GizmoFlags.GIZMO_TRANSLATE)) != 0)
            {
                DrawGizmoPlane(ref data, i);
            }

            if ((data.flags & (int)GizmoFlags.GIZMO_ROTATE) != 0)
            {
                DrawGizmoCircle(ref data, i);
            }
        }

        if ((data.flags & ((int)GizmoFlags.GIZMO_SCALE | (int)GizmoFlags.GIZMO_TRANSLATE)) != 0)
        {
            DrawGizmoCenter(ref data);
        }

        //------------------------------------------------------------------------

        DrawRenderBatchActive();
        SetLineWidth(prevLineWidth);
        EnableBackfaceCulling();
        EnableDepthTest();
        EnableDepthMask();

        //------------------------------------------------------------------------

        // If there's an active transformation, only the interested gizmo handles the input
        if (!IsGizmoTransforming() || TransformEqual(data.curTransform, GIZMO.activeTransform))
        {
            GizmoHandleInput(ref data);
        }

        //------------------------------------------------------------------------

        return IsThisGizmoTransforming(ref data);
    }
    public static Matrix4x4 GizmoToMatrix(TransformData transform)
    {
        return MatrixMultiply(MatrixMultiply(MatrixScale(transform.Scale.X, transform.Scale.Y, transform.Scale.Z),
                QuaternionToMatrix(transform.Rotation)),
            MatrixTranslate(transform.Translation.X, transform.Translation.Y, transform.Translation.Z));
    }

    private static bool TransformEqual(Transform t1, Transform t2)
    {
        return t1.Translation == t2.Translation && t1.Rotation == t2.Rotation && t1.Scale == t2.Scale;
    }
    
    private static bool TransformEqual(TransformData t1, TransformData t2)
    {
        return t1.Translation == t2.Translation && t1.Rotation == t2.Rotation && t1.Scale == t2.Scale;
    }

    public static void ComputeAxisOrientation(ref GizmoData gizmoData)
    {
        int flags = gizmoData.flags;

        // Scaling is currently supported only in local mode
        if ((flags & (int)GizmoFlags.GIZMO_SCALE) != 0)
        {
            flags &= ~(int)GizmoFlags.GIZMO_VIEW;
            flags |= (int)GizmoFlags.GIZMO_LOCAL;
        }

        if ((flags & (int)GizmoFlags.GIZMO_VIEW) != 0)
        {
            gizmoData.axis[(int)GizmoAxes.GZ_AXIS_X] = gizmoData.right;
            gizmoData.axis[(int)GizmoAxes.GZ_AXIS_Y] = gizmoData.up;
            gizmoData.axis[(int)GizmoAxes.GZ_AXIS_Z] = gizmoData.forward;
        }
        else
        {
            gizmoData.axis[(int)GizmoAxes.GZ_AXIS_X] = GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_X].normal;
            gizmoData.axis[(int)GizmoAxes.GZ_AXIS_Y] = GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_Y].normal;
            gizmoData.axis[(int)GizmoAxes.GZ_AXIS_Z] = GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_Z].normal;

            if ((flags & (int)GizmoFlags.GIZMO_LOCAL) != 0)
            {
                for (int i = 0; i < 3; ++i)
                {
                    gizmoData.axis[i] = Vector3Normalize(
                        Vector3RotateByQuaternion(gizmoData.axis[i], gizmoData.curTransform.Rotation));
                }
            }
        }
    }

    public static bool IsGizmoAxisActive(int axis)
    {
        return (axis == (int)GizmoAxes.GZ_AXIS_X && (GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_X) != 0) ||
               (axis == (int)GizmoAxes.GZ_AXIS_Y && (GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Y) != 0) ||
               (axis == (int)GizmoAxes.GZ_AXIS_Z && (GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Z) != 0);
    }

    public static bool CheckGizmoType(ref GizmoData data, int type)
    {
        return (data.flags & type) == type;
    }

    public static bool IsGizmoTransforming()
    {
        return GIZMO.curAction != (int)GizmoAction.GZ_ACTION_NONE;
    }

    public static bool IsThisGizmoTransforming(ref GizmoData gizmoData)
    {
        return IsGizmoTransforming() && TransformEqual(gizmoData.curTransform, GIZMO.activeTransform);
    }

    public static bool IsGizmoScaling()
    {
        return GIZMO.curAction == (int)GizmoAction.GZ_ACTION_SCALE;
    }

    public static bool IsGizmoTranslating()
    {
        return GIZMO.curAction == (int)GizmoAction.GZ_ACTION_TRANSLATE;
    }

    public static bool IsGizmoRotating()
    {
        return GIZMO.curAction == (int)GizmoAction.GZ_ACTION_ROTATE;
    }

    public static Vector3 Vec3ScreenToWorld(Vector3 source, ref Matrix4x4 matViewProjInv)
    {
        Quaternion qt = QuaternionTransform(new Quaternion(source.X, source.Y, source.Z, 1.0f), matViewProjInv);
        return new Vector3(
            qt.X / qt.W,
            qt.Y / qt.W,
            qt.Z / qt.W
        );
    }

    public static Ray Vec3ScreenToWorldRay(Vector2 position, ref Matrix4x4 matViewProjInv)
    {
        Ray ray = default;

        float width = GetScreenWidth();

        float height = GetScreenHeight();

        Vector2 deviceCoords = new Vector2((2.0f * position.X) / width - 1.0f, 1.0f - (2.0f * position.Y) / height);

        Vector3 nearPoint = Vec3ScreenToWorld(new Vector3(deviceCoords.X, deviceCoords.Y, 0.0f), ref matViewProjInv);

        Vector3 farPoint = Vec3ScreenToWorld(new Vector3(deviceCoords.X, deviceCoords.Y, 1.0f), ref matViewProjInv);

        Vector3 cameraPlanePointerPos = Vec3ScreenToWorld(new Vector3(deviceCoords.X, deviceCoords.Y, -1.0f),
            ref matViewProjInv);

        Vector3 direction = Vector3Normalize(Vector3Subtract(farPoint, nearPoint));

        /*
        if (camera.projection == CAMERA_PERSPECTIVE) ray.position = camera.position;
        else if (camera.projection == CAMERA_ORTHOGRAPHIC) ray.position = cameraPlanePointerPos;
        */
        ray.Position = cameraPlanePointerPos;

        // Apply calculated vectors to ray
        ray.Direction = direction;

        return ray;
    }

    public static void DrawGizmoCube(ref GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(ref data) && (!IsGizmoAxisActive(axis) || !IsGizmoScaling()))
        {
            return;
        }

        float gizmoSize = CheckGizmoType(ref data, (int)GizmoFlags.GIZMO_SCALE | (int)GizmoFlags.GIZMO_TRANSLATE)
            ? data.gizmoSize * 0.5f
            : data.gizmoSize;

        Vector3 endPos = Vector3Add(data.curTransform.Translation,
            Vector3Scale(data.axis[axis], gizmoSize * (1.0f - GIZMO.trArrowWidthFactor)));

        DrawLine3D(data.curTransform.Translation, endPos, GIZMO.axisCfg[axis].color);

        float boxSize = data.gizmoSize * GIZMO.trArrowWidthFactor;
        Vector3 dim1 = Vector3Scale(data.axis[(axis + 1) % 3], boxSize);
        Vector3 dim2 = Vector3Scale(data.axis[(axis + 2) % 3], boxSize);
        Vector3 n = data.axis[axis];
        Color col = GIZMO.axisCfg[axis].color;
        Vector3 depth = Vector3Scale(n, boxSize);
        Vector3 a = Vector3Subtract(Vector3Subtract(endPos, Vector3Scale(dim1, 0.5f)), Vector3Scale(dim2, 0.5f));
        Vector3 b = Vector3Add(a, dim1);
        Vector3 c = Vector3Add(b, dim2);
        Vector3 d = Vector3Add(a, dim2);
        Vector3 e = Vector3Add(a, depth);
        Vector3 f = Vector3Add(b, depth);
        Vector3 g = Vector3Add(c, depth);
        Vector3 h = Vector3Add(d, depth);

        Begin(DrawMode.Quads);
        Color4ub(col.R, col.G, col.B, col.A);
        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(e.X, e.Y, e.Z);
        Vertex3f(f.X, f.Y, f.Z);
        Vertex3f(g.X, g.Y, g.Z);
        Vertex3f(h.X, h.Y, h.Z);
        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(e.X, e.Y, e.Z);
        Vertex3f(f.X, f.Y, f.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(f.X, f.Y, f.Z);
        Vertex3f(g.X, g.Y, g.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(f.X, f.Y, f.Z);
        Vertex3f(e.X, e.Y, e.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(g.X, g.Y, g.Z);
        Vertex3f(h.X, h.Y, h.Z);
        Vertex3f(d.X, d.Y, d.Z);
        End();
    }

    public static void DrawGizmoPlane(ref GizmoData data, int index)
    {
        if (IsThisGizmoTransforming(ref data))
        {
            return;
        }

        Vector3 dir1 = data.axis[(index + 1) % 3];
        Vector3 dir2 = data.axis[(index + 2) % 3];
        Color col = GIZMO.axisCfg[index].color;

        float offset = GIZMO.trPlaneOffsetFactor * data.gizmoSize;
        float size = GIZMO.trPlaneSizeFactor * data.gizmoSize;

        Vector3 a = Vector3Add(Vector3Add(data.curTransform.Translation, Vector3Scale(dir1, offset)),
            Vector3Scale(dir2, offset));
        Vector3 b = Vector3Add(a, Vector3Scale(dir1, size));
        Vector3 c = Vector3Add(b, Vector3Scale(dir2, size));
        Vector3 d = Vector3Add(a, Vector3Scale(dir2, size));

        Begin(DrawMode.Quads);

        Color4ub(col.R, col.G, col.B, (byte)(col.A * 0.5f));

        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(d.X, d.Y, d.Z);

        End();

        Begin(DrawMode.Lines);
        Color4ub(col.R, col.G, col.B, col.A);

        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(a.X, a.Y, a.Z);

        End();
    }

    public static void DrawGizmoArrow(ref GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(ref data) && (!IsGizmoAxisActive(axis) || !IsGizmoTranslating()))
        {
            return;
        }

        Vector3 endPos = Vector3Add(data.curTransform.Translation,
            Vector3Scale(data.axis[axis],
                data.gizmoSize * (1.0f - GIZMO.trArrowLengthFactor)));

        if ((data.flags & (int)GizmoFlags.GIZMO_SCALE) == 0)
            DrawLine3D(data.curTransform.Translation, endPos, GIZMO.axisCfg[axis].color);

        float arrowLength = data.gizmoSize * GIZMO.trArrowLengthFactor;
        float arrowWidth = data.gizmoSize * GIZMO.trArrowWidthFactor;

        Vector3 dim1 = Vector3Scale(data.axis[(axis + 1) % 3], arrowWidth);
        Vector3 dim2 = Vector3Scale(data.axis[(axis + 2) % 3], arrowWidth);
        Vector3 n = data.axis[axis];
        Color col = GIZMO.axisCfg[axis].color;

        Vector3 v = Vector3Add(endPos, Vector3Scale(n, arrowLength));

        Vector3 a = Vector3Subtract(Vector3Subtract(endPos, Vector3Scale(dim1, 0.5f)), Vector3Scale(dim2, 0.5f));
        Vector3 b = Vector3Add(a, dim1);
        Vector3 c = Vector3Add(b, dim2);
        Vector3 d = Vector3Add(a, dim2);

        Begin(DrawMode.Triangles);

        Color4ub(col.R, col.G, col.B, col.A);

        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(a.X, a.Y, a.Z);
        Vertex3f(v.X, v.Y, v.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(b.X, b.Y, b.Z);
        Vertex3f(v.X, v.Y, v.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(c.X, c.Y, c.Z);
        Vertex3f(v.X, v.Y, v.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(d.X, d.Y, d.Z);
        Vertex3f(v.X, v.Y, v.Z);
        Vertex3f(a.X, a.Y, a.Z);

        End();
    }

    public static void DrawGizmoCenter(ref GizmoData data)
    {
        Vector3 origin = data.curTransform.Translation;

        float radius = data.gizmoSize * GIZMO.trCircleRadiusFactor;
        Color col = GIZMO.trCircleColor;
        int angleStep = 15;

        PushMatrix();
        Translatef(origin.X, origin.Y, origin.Z);

        Begin(DrawMode.Lines);
        Color4ub(col.R, col.G, col.B, col.A);
        for (int i = 0; i < 360; i += angleStep)
        {
            float angle = float.DegreesToRadians(i);
            Vector3 p = Vector3Scale(data.right, MathF.Sin(angle) * radius);
            p = Vector3Add(p, Vector3Scale(data.up, MathF.Cos(angle) * radius));
            Vertex3f(p.X, p.Y, p.Z);

            angle += float.DegreesToRadians(angleStep);
            p = Vector3Scale(data.right, MathF.Sin(angle) * radius);
            p = Vector3Add(p, Vector3Scale(data.up, MathF.Cos(angle) * radius));
            Vertex3f(p.X, p.Y, p.Z);
        }

        End();
        PopMatrix();
    }

    public static void DrawGizmoCircle(ref GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(ref data) && (!IsGizmoAxisActive(axis) || !IsGizmoRotating()))
        {
            return;
        }

        Vector3 origin = data.curTransform.Translation;

        Vector3 dir1 = data.axis[(axis + 1) % 3];
        Vector3 dir2 = data.axis[(axis + 2) % 3];

        Color col = GIZMO.axisCfg[axis].color;

        float radius = data.gizmoSize;
        int angleStep = 10;

        PushMatrix();
        Translatef(origin.X, origin.Y, origin.Z);

        Begin(DrawMode.Lines);
        Color4ub(col.R, col.G, col.B, col.A);
        for (int i = 0; i < 360; i += angleStep)
        {
            float angle = float.DegreesToRadians(i);
            Vector3 p = Vector3Scale(dir1, MathF.Sin(angle) * radius);
            p = Vector3Add(p, Vector3Scale(dir2, MathF.Cos(angle) * radius));
            Vertex3f(p.X, p.Y, p.Z);

            angle += float.DegreesToRadians(angleStep);
            p = Vector3Scale(dir1, MathF.Sin(angle) * radius);
            p = Vector3Add(p, Vector3Scale(dir2, MathF.Cos(angle) * radius));
            Vertex3f(p.X, p.Y, p.Z);
        }

        End();
        PopMatrix();
    }

    public static bool CheckOrientedBoundingBox(ref GizmoData data, Ray ray, Vector3 obbCenter, Vector3 obbHalfSize)
    {
        Vector3 oLocal = Vector3Subtract(ray.Position, obbCenter);

        Ray localRay;

        localRay.Position.X = Vector3DotProduct(oLocal, data.axis[(int)GizmoAxes.GZ_AXIS_X]);
        localRay.Position.Y = Vector3DotProduct(oLocal, data.axis[(int)GizmoAxes.GZ_AXIS_Y]);
        localRay.Position.Z = Vector3DotProduct(oLocal, data.axis[(int)GizmoAxes.GZ_AXIS_Z]);

        localRay.Direction.X = Vector3DotProduct(ray.Direction, data.axis[(int)GizmoAxes.GZ_AXIS_X]);
        localRay.Direction.Y = Vector3DotProduct(ray.Direction, data.axis[(int)GizmoAxes.GZ_AXIS_Y]);
        localRay.Direction.Z = Vector3DotProduct(ray.Direction, data.axis[(int)GizmoAxes.GZ_AXIS_Z]);

        BoundingBox aabbLocal = new BoundingBox(-obbHalfSize, obbHalfSize);

        return GetRayCollisionBox(localRay, aabbLocal).Hit;
    }

    public static bool CheckGizmoAxis(ref GizmoData data, int axis, Ray ray, int type)
    {
        float[] halfDim = new float[3];

        halfDim[axis] = data.gizmoSize * 0.5f;
        halfDim[(axis + 1) % 3] = data.gizmoSize * GIZMO.trArrowWidthFactor * 0.5f;
        halfDim[(axis + 2) % 3] = halfDim[(axis + 1) % 3];

        if (type == (int)GizmoFlags.GIZMO_SCALE &&
            CheckGizmoType(ref data, (int)(GizmoFlags.GIZMO_TRANSLATE | GizmoFlags.GIZMO_SCALE)))
        {
            halfDim[axis] *= 0.5f;
        }

        Vector3 obbCenter = Vector3Add(data.curTransform.Translation,
            Vector3Scale(data.axis[axis], halfDim[axis]));

        return CheckOrientedBoundingBox(ref data, ray, obbCenter, new Vector3(
            halfDim[0], halfDim[1], halfDim[2]));
    }

    public static bool CheckGizmoPlane(ref GizmoData data, int axis, Ray ray)
    {
        Vector3 dir1 = data.axis[(axis + 1) % 3];
        Vector3 dir2 = data.axis[(axis + 2) % 3];


        float offset = GIZMO.trPlaneOffsetFactor * data.gizmoSize;
        float size = GIZMO.trPlaneSizeFactor * data.gizmoSize;

        Vector3 a = Vector3Add(Vector3Add(data.curTransform.Translation, Vector3Scale(dir1, offset)),
            Vector3Scale(dir2, offset));
        Vector3 b = Vector3Add(a, Vector3Scale(dir1, size));
        Vector3 c = Vector3Add(b, Vector3Scale(dir2, size));
        Vector3 d = Vector3Add(a, Vector3Scale(dir2, size));

        return GetRayCollisionQuad(ray, a, b, c, d).Hit;
    }

    public static bool CheckGizmoCircle(ref GizmoData data, int index, Ray ray)
    {
        Vector3 origin = data.curTransform.Translation;

        Vector3 dir1 = data.axis[(index + 1) % 3];
        Vector3 dir2 = data.axis[(index + 2) % 3];

        float circleRadius = data.gizmoSize;
        int angleStep = 10;

        float sphereRadius = /*2.0f **/ circleRadius * MathF.Sin(float.DegreesToRadians(angleStep / 2.0f));

        for (int i = 0; i < 360; i += angleStep)
        {
            float angle = float.DegreesToRadians(i);
            Vector3 p = Vector3Add(origin, Vector3Scale(dir1, MathF.Sin(angle) * circleRadius));
            p = Vector3Add(p, Vector3Scale(dir2, MathF.Cos(angle) * circleRadius));

            if (GetRayCollisionSphere(ray, p, sphereRadius).Hit)
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckGizmoCenter(ref GizmoData data, Ray ray)
    {
        return GetRayCollisionSphere(ray, data.curTransform.Translation, data.gizmoSize * GIZMO.trCircleRadiusFactor)
            .Hit;
    }

    public static Vector3 GetWorldMouse(ref GizmoData data)
    {
        float dist = Vector3Distance(data.camPos, data.curTransform.Translation);
        Ray mouseRay = Vec3ScreenToWorldRay(GetMousePosition(), ref data.invViewProj);
        return Vector3Add(mouseRay.Position, Vector3Scale(mouseRay.Direction, dist));
    }

    public static void GizmoHandleInput(ref GizmoData data)
    {
        int action = GIZMO.curAction;

        if (action != (int)GizmoAction.GZ_ACTION_NONE)
        {
            if (!IsMouseButtonDown(MouseButton.Left))
            {
                //SetMouseCursor(MOUSE_CURSOR_DEFAULT);
                action = (int)GizmoAction.GZ_ACTION_NONE;
                GIZMO.activeAxis = 0;
            }
            else
            {
                Vector3 endWorldMouse = GetWorldMouse(ref data);
                Vector3 pVec = Vector3Subtract(endWorldMouse, GIZMO.startWorldMouse);

                switch (action)
                {
                    case (int)GizmoAction.GZ_ACTION_TRANSLATE:
                    {
                        GIZMO.activeTransform.Translation = GIZMO.startTransform.Translation;
                        if (GIZMO.activeAxis == (int)GizmoActiveAxis.GZ_ACTIVE_XYZ)
                        {
                            GIZMO.activeTransform.Translation = Vector3Add(GIZMO.activeTransform.Translation,
                                Vector3Project(pVec, data.right));
                            GIZMO.activeTransform.Translation = Vector3Add(GIZMO.activeTransform.Translation,
                                Vector3Project(pVec, data.up));
                        }
                        else
                        {
                            if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_X) != 0)
                            {
                                Vector3 prj = Vector3Project(pVec, data.axis[(int)GizmoAxes.GZ_AXIS_X]);
                                GIZMO.activeTransform.Translation =
                                    Vector3Add(GIZMO.activeTransform.Translation, prj);
                            }

                            if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Y) != 0)
                            {
                                Vector3 prj = Vector3Project(pVec, data.axis[(int)GizmoAxes.GZ_AXIS_Y]);
                                GIZMO.activeTransform.Translation =
                                    Vector3Add(GIZMO.activeTransform.Translation, prj);
                            }

                            if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Z) != 0)
                            {
                                Vector3 prj = Vector3Project(pVec, data.axis[(int)GizmoAxes.GZ_AXIS_Z]);
                                GIZMO.activeTransform.Translation =
                                    Vector3Add(GIZMO.activeTransform.Translation, prj);
                            }
                        }
                    }
                        break;
                    case (int)GizmoAction.GZ_ACTION_SCALE:
                    {
                        GIZMO.activeTransform.Scale = GIZMO.startTransform.Scale;
                        if (GIZMO.activeAxis == (int)GizmoActiveAxis.GZ_ACTIVE_XYZ)
                        {
                             float delta = Vector3DotProduct(pVec, GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_X].normal);
                            GIZMO.activeTransform.Scale = Vector3AddValue(GIZMO.activeTransform.Scale, delta);
                        }
                        else
                        {
                            if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_X) != 0)
                            {
                                Vector3 prj = Vector3Project(pVec, GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_X].normal);
                                // data->axis[GIZMO_AXIS_X]);
                                GIZMO.activeTransform.Scale = Vector3Add(GIZMO.activeTransform.Scale, prj);
                            }

                            if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Y) != 0)
                            {
                                Vector3 prj = Vector3Project(pVec, GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_Y].normal);
                                GIZMO.activeTransform.Scale = Vector3Add(GIZMO.activeTransform.Scale, prj);
                            }

                            if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Z) != 0)
                            {
                                Vector3 prj = Vector3Project(pVec, GIZMO.axisCfg[(int)GizmoAxes.GZ_AXIS_Z].normal);
                                GIZMO.activeTransform.Scale = Vector3Add(GIZMO.activeTransform.Scale, prj);
                            }
                        }
                    }
                        break;
                    case (int)GizmoAction.GZ_ACTION_ROTATE:
                    {
                        GIZMO.activeTransform.Rotation = GIZMO.startTransform.Rotation;
                        //SetMouseCursor(MOUSE_CURSOR_RESIZE_EW);
                         float delta = Clamp(Vector3DotProduct(pVec, Vector3Add(data.right, data.up)), -2 * MathF.PI,
                            +2 * MathF.PI);
                        if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_X) != 0)
                        {
                            Quaternion q = QuaternionFromAxisAngle(data.axis[(int)GizmoAxes.GZ_AXIS_X], delta);
                            GIZMO.activeTransform.Rotation = QuaternionMultiply(q, GIZMO.activeTransform.Rotation);
                        }

                        if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Y) != 0)
                        {
                             Quaternion q = QuaternionFromAxisAngle(data.axis[(int)GizmoAxes.GZ_AXIS_Y], delta);
                            GIZMO.activeTransform.Rotation = QuaternionMultiply(q, GIZMO.activeTransform.Rotation);
                        }

                        if ((GIZMO.activeAxis & (int)GizmoActiveAxis.GZ_ACTIVE_Z) != 0)
                        {
                             Quaternion q = QuaternionFromAxisAngle(data.axis[(int)GizmoAxes.GZ_AXIS_Z], delta);
                            GIZMO.activeTransform.Rotation = QuaternionMultiply(q, GIZMO.activeTransform.Rotation);
                        }

                        //BUG FIXED: Updating the transform "starting point" prevents uncontrolled rotations in local mode
                        GIZMO.startTransform.Translation = GIZMO.activeTransform.Translation;
                        GIZMO.startTransform.Rotation = GIZMO.activeTransform.Rotation;
                        GIZMO.startTransform.Scale = GIZMO.activeTransform.Scale;
                        GIZMO.startWorldMouse = endWorldMouse;
                    }
                        break;
                    default:
                        break;
                }
            }
        }
        else
        {
            if (IsMouseButtonPressed(MouseButton.Left))
            {
                 Ray mouseRay = Vec3ScreenToWorldRay(GetMousePosition(), ref data.invViewProj);

                int hit = -1;
                action = (int)GizmoAction.GZ_ACTION_NONE;

                for (int k = 0; hit == -1 && k < 2; ++k)
                {
                    int gizmoFlag = (int)(k == 0 ? GizmoFlags.GIZMO_SCALE : GizmoFlags.GIZMO_TRANSLATE);
                    int gizmoAction = (int)(k == 0 ? GizmoAction.GZ_ACTION_SCALE : GizmoAction.GZ_ACTION_TRANSLATE);

                    if ((data.flags & gizmoFlag) != 0)
                    {
                        if (CheckGizmoCenter(ref data, mouseRay))
                        {
                            action = gizmoAction;
                            hit = 6;
                            break;
                        }

                        for (int i = 0; i < (int)GizmoAxes.GIZMO_AXIS_COUNT; ++i)
                        {
                            if (CheckGizmoAxis(ref data, i, mouseRay, gizmoFlag))
                            {
                                action = gizmoAction;
                                hit = i;
                                break;
                            }

                            if (CheckGizmoPlane(ref data, i, mouseRay))
                            {
                                action = CheckGizmoType(ref data, (int)(GizmoFlags.GIZMO_SCALE | GizmoFlags.GIZMO_TRANSLATE))
                                    ? (int)GizmoFlags.GIZMO_TRANSLATE
                                    : gizmoAction;
                                hit = 3 + i;
                                break;
                            }
                        }
                    }
                }

                if (hit == -1 && (data.flags & (int)GizmoFlags.GIZMO_ROTATE) != 0)
                {
                    for (int i = 0; i < (int)GizmoAxes.GIZMO_AXIS_COUNT; ++i)
                    {
                        if (CheckGizmoCircle(ref data, i, mouseRay))
                        {
                            action = (int)GizmoAction.GZ_ACTION_ROTATE;
                            hit = i;
                            break;
                        }
                    }
                }

                GIZMO.activeAxis = 0;
                if (hit >= 0)
                {
                    switch (hit)
                    {
                        case 0:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_X;
                            break;
                        case 1:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_Y;
                            break;
                        case 2:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_Z;
                            break;
                        case 3:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_Y | (int)GizmoActiveAxis.GZ_ACTIVE_Z;
                            break;
                        case 4:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_X | (int)GizmoActiveAxis.GZ_ACTIVE_Z;
                            break;
                        case 5:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_X | (int)GizmoActiveAxis.GZ_ACTIVE_Y;
                            break;
                        case 6:
                            GIZMO.activeAxis = (int)GizmoActiveAxis.GZ_ACTIVE_XYZ;
                            break;
                    }

                    GIZMO.activeTransform = data.curTransform;
                    GIZMO.startTransform = new Transform()
                    {
                        Rotation = data.curTransform.Rotation,
                        Scale = data.curTransform.Scale,
                        Translation = data.curTransform.Scale
                    };
                    GIZMO.startWorldMouse = GetWorldMouse(ref data);
                }
            }
        }

        GIZMO.curAction = action;
    }
}