using UnityEngine;
using UnityEngine.Serialization;

public class ColliderScript : MonoBehaviour
{
    public bool isUsed = false;
    public AtomView Atom = null;
    public Vector3 actualPosition;
    
    private SphereCollider colliderComponent;

    public void Init()
    {
        colliderComponent = GetComponent<SphereCollider>();
        isUsed = true;
    }

    public void SetAtom(AtomView atom, Vector3 coords, float radius)
    {
        Atom = atom;
        actualPosition = coords;
        colliderComponent.center = coords;
        colliderComponent.radius = radius / 2;
    }
    
    public void SetPosition(Vector3 coords)
    {
        actualPosition = coords;
        colliderComponent.center = coords;
    }
}
