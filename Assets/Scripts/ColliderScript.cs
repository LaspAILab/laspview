using UnityEngine;
using UnityEngine.Serialization;

public class ColliderScript : MonoBehaviour
{
    public bool isUsed = false;
    public AtomView Atom = null;
    
    private SphereCollider colliderComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init()
    {
        colliderComponent = GetComponent<SphereCollider>();
    }

    public void SetAtom(AtomView atom, Vector3 coords, float radius)
    {
        Atom = atom;
        colliderComponent.center = coords;
        colliderComponent.radius = radius / 2;
    }

    public void Drop()
    {
        isUsed = false;
        colliderComponent.enabled = false;
        gameObject.SetActive(false);
    }

    public void Activate()
    {
        isUsed = true;
        colliderComponent.enabled = true;
        gameObject.SetActive(true);
    }
}
