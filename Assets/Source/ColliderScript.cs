using UnityEngine;
using UnityEngine.Serialization;

public class ColliderScript : MonoBehaviour
{
    public bool isUsed = false;
    public AtomView Atom = null;

    public Vector3 latticeOffset;
    public Vector3 ActualPosition => Atom.Position + latticeOffset;

    private SphereCollider colliderComponent;

    public void Init()
    {
        colliderComponent = GetComponent<SphereCollider>();
        isUsed = true;
    }

    public void SetAtom(AtomView atom, Vector3 position, float radius)
    {
        Atom = atom;
        latticeOffset = position - Atom.Position;
        colliderComponent.center = position;
        colliderComponent.radius = radius / 2;
    }

    public void SetPosition(Vector3 position)
    {
        latticeOffset = position - Atom.Position;
        colliderComponent.center = position;
    }
}