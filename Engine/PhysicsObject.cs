using System.Numerics;
using BulletSharp;
using BulletSharp.Math;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;
using Raylib_cs.BleedingEdge;
namespace Engine;

public class PhysicsObject : IDisposable
{
    public RigidBody RigidBody { get; private set; }
    private Model _cube;
    public PhysicsObject(float yPos, Vector3 scale, float mass, bool isStatic)
    {
        var box = new BoxShape(new BulletSharp.Math.Vector3(scale.X, scale.Y, scale.Z) * 0.5f);
        BulletSharp.Math.Vector3 localInertia = BulletSharp.Math.Vector3.Zero;
        if (!isStatic)
        {
            localInertia = box.CalculateLocalInertia(mass);
        }
        DefaultMotionState state = new DefaultMotionState(Matrix.Translation(BulletSharp.Math.Vector3.UnitY * yPos));
        var bodyInfo = new RigidBodyConstructionInfo(mass, state, box, localInertia);
        RigidBody = new RigidBody(bodyInfo);
        bodyInfo.Dispose();
        Mesh mesh = Raylib.GenMeshCube(scale.X, scale.Y, scale.Z);
        _cube = Raylib.LoadModelFromMesh(mesh);
    }

    public void Render()
    {
        Matrix trans;
        if (RigidBody.MotionState != null)
        {
            RigidBody.MotionState.GetWorldTransform(out trans);
        }
        else
        {
            return;
        }

        trans.Decompose(out var scale, out var rotation, out var translation);
        var toMat = Matrix4x4.Identity;
        toMat = Matrix4x4.Transform(toMat, new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
        toMat *= Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z);
        _cube.Transform = toMat;
        Raylib.DrawModelWires(_cube, new Vector3(translation.X, translation.Y, translation.Z), 1.0f, Color.Gray);
    }

    public void Dispose()
    {
        RigidBody.Dispose();
    }
}