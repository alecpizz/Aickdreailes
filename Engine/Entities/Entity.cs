using System.Numerics;
using ImGuiNET;
using Raylib_cs.BleedingEdge;

namespace Engine.Entities;


//entity
// render3D() -> render within 3d context
// renderUI() -> render within ui context
// update() -> logic update
// fixedUpdate() -> fixed logic update

//should this be an interface?

public class Entity
{
    public Transform Transform { get; protected set; } = new Transform(Vector3.Zero,
        Quaternion.Identity, Vector3.One);

    public string Name { get; private set; } = "";
    
    public Entity(string name)
    {
        Name = name;
    }
    
    public virtual void OnUpdate()
    {
    }

    public virtual void OnFixedUpdate()
    {
        throw new NotImplementedException();
    }

    public virtual void OnPreRender()
    {
        throw new NotImplementedException();
    }

    public virtual void OnRender()
    {
        
    }

    public virtual void OnPostRender()
    {
        throw new NotImplementedException();
    }

    public virtual void OnUIRender()
    {
    }

    public virtual void OnImGuiWindowRender()
    {
        var transform = Transform;
        ImGui.InputFloat3("Position", ref transform.Translation);
        var quaternionToEuler = Raymath.QuaternionToEuler(transform.Rotation);
        ImGui.InputFloat3("Euler Angles", ref quaternionToEuler);
        ImGui.InputFloat3("Scale", ref transform.Scale);
    }

    public virtual void OnCleanup()
    {
        
    }
}