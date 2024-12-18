using System.Numerics;
using Raylib_cs.BleedingEdge;

namespace Engine.Entities;


//entity
// render3D() -> render within 3d context
// renderUI() -> render within ui context
// update() -> logic update, components update
// fixedUpdate() -> fixed logic update, componets fixed update

public class Entity
{
    public Transform Transform { get; protected set; } = new Transform(Vector3.Zero,
        Quaternion.Identity, Vector3.One);
    
    public virtual void OnUpdate()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public virtual void OnCleanup()
    {
        
    }
}