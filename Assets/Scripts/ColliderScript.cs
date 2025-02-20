using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public bool IsUsed = false;
    public AtomView Atom = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetAtom(AtomView atom, Vector3 coords, float radius)
    {
        Atom = atom;

        var colliderComponent = GetComponent<SphereCollider>();
        colliderComponent.center = coords;
        colliderComponent.radius = radius / 2;
    }

    public void Drop()
    {
        IsUsed = false;
        GetComponent<SphereCollider>().enabled = false;
        gameObject.SetActive(false);
    }

    public void Activate()
    {
        IsUsed = true;
        GetComponent<SphereCollider>().enabled = true;
        gameObject.SetActive(true);
    }
}
