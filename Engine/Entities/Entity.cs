using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace Engine.Entities;

//entity
// render3D() -> render within 3d context
// renderUI() -> render within ui context
// update() -> logic update
// fixedUpdate() -> fixed logic update

//should this be an interface?

public class Entity
{
    public Transform Transform { get; protected set; } = new Transform()
    {
        Rotation = Quaternion.Identity,
        Scale = Vector3.One,
        Translation = Vector3.Zero
    };

    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;

    public Entity(string name)
    {
        Name = name;
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnFixedUpdate()
    {
    }

    public virtual void OnPreRender()
    {
        throw new NotImplementedException();
    }

    public virtual void OnRender(Shader? shader = null)
    {
    }

    public virtual void OnPostRender()
    {
    }

    public virtual void OnUIRender()
    {
    }

    public virtual void OnImGuiWindowRender()
    {
        var transform = Transform;
        ImGUIUtils.DrawTransform(ref transform);
        Transform = transform;
        ImGUIUtils.DrawFields(this);
    }

    public virtual void OnCleanup()
    {
    }

    public void SetActive(bool active)
    {
        IsActive = active;
    }
}