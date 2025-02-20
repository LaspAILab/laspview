using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public bool IsUsed = false;
    public int AtomIndex = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetAtom(int atomIndex, Vector3 coords, float radius)
    {
        AtomIndex = atomIndex;

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
