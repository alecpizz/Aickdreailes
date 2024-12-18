using System.Numerics;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs.BleedingEdge;
using static Raylib_cs.BleedingEdge.Raylib;

namespace Engine.Entities;

public class StaticEntity : Entity
{
    private Model _model;
    private RigidBody _rigidBody;

    public unsafe StaticEntity(string path, Vector3 position)
    {
        try
        {
            _model = LoadModel(path);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return;
        }

        var tr = Transform;
        tr.Translation = position;
        Transform = tr;
        
        for (int i = 0; i < _model.MaterialCount; i++)
        {
            if (_model.Materials[i].Maps != null)
            {
                _model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture.Mipmaps = 4;
                GenTextureMipmaps(_model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture);
                SetTextureFilter(_model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture,
                    TextureFilter.Trilinear);
            }
        }

        _rigidBody = Engine.World.CreateRigidBody();
        _rigidBody.Tag = this;
        List<JTriangle> tris = new List<JTriangle>();

        for (int i = 0; i < _model.MeshCount; i++)
        {
            var mesh = _model.Meshes[i];
            Vector3* vertdata = (Vector3*)mesh.Vertices;

            for (int j = 0; j < mesh.TriangleCount; j++)
            {
                JVector a, b, c;
                if (mesh.Indices != null)
                {
                    a = vertdata[mesh.Indices[j * 3 + 0]].ToJVector();
                    b = vertdata[mesh.Indices[j * 3 + 1]].ToJVector();
                    c = vertdata[mesh.Indices[j * 3 + 2]].ToJVector();
                }
                else
                {
                    a = vertdata[i*3 + 0].ToJVector();
                    b = vertdata[i*3 + 1].ToJVector();
                    c = vertdata[i*3 + 2].ToJVector();
                }

                JVector normal = (c - b) % (a - b);

                if (MathHelper.CloseToZero(normal, 1e-12f))
                {
                    continue;
                }

                tris.Add(new JTriangle(b, a, c));
            }
        }

        var jtm = new TriangleMesh(tris);
        List<RigidBodyShape> triangleShapes = new List<RigidBodyShape>();
        for (int i = 0; i < jtm.Indices.Length; i++)
        {
            TriangleShape ts = new TriangleShape(jtm, i);
            triangleShapes.Add(ts);
        }
        
        _rigidBody.AddShape(triangleShapes, false);
        _rigidBody.Position = _model.Transform.Translation.ToJVector();
        _rigidBody.IsStatic = true;
    }

    public override unsafe void OnRender()
    {
        Matrix4x4 matrix = Matrix4x4.Identity;
        matrix *= Matrix4x4.CreateTranslation(Transform.Translation);
        matrix *= Matrix4x4.CreateFromQuaternion(Transform.Rotation);
        matrix *= Matrix4x4.CreateScale(Transform.Scale);
        for (int i = 0; i < _model.MeshCount; i++)
        {
            DrawMesh(_model.Meshes[i], _model.Materials[_model.MeshMaterial[i]], matrix);
        }
    }

    public override void OnCleanup()
    {
        UnloadModel(_model);
        Engine.World.Remove(_rigidBody);
    }
}