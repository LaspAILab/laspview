using UnityEngine;
using UnityEngine.Serialization;

public class ColliderScript : MonoBehaviour, IPoolable
{
    public AtomView Atom = null;

    public Vector3 latticeOffset;
    public Vector3 ActualPosition => Atom.Position + latticeOffset;

    private SphereCollider colliderComponent;
    private bool isUsed = false;

    public void Init()
    {
        colliderComponent = GetComponent<SphereCollider>();
        isUsed = true;
    }

    public void Destroy()
    {
        GameObject.Destroy(gameObject);
    }

    public bool IsUsed => isUsed;

    public void Enable()
    {
        gameObject.SetActive(true);
        isUsed = true;
    }

    public void Disable()
    {
        gameObject.SetActive(false);
        isUsed = false;
    }

    public void SetAtom(AtomView atom, Vector3 position, float radius)
    {
        Atom = atom;
        latticeOffset = position - Atom.Position;
        colliderComponent.center = position;
        colliderComponent.radius = radius / 2;
    }
}