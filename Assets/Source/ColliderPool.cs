using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class ColliderPool
{
    public GameObject ColliderPrefab;
    public GameObject ColliderParent;

    private List<ColliderScript> colliders;
    private int numFree;

    public ColliderPool(GameObject colliderPrefab, GameObject colliderParent)
    {
        colliders = new();
        numFree = 0;
        ColliderPrefab = colliderPrefab;
        ColliderParent = colliderParent;
    }

    private GameObject CreateColliderObject()
    {
        return GameObject.Instantiate(ColliderPrefab, ColliderParent.transform);
    }

    public ColliderScript BorrowCollider()
    {
        if (numFree == 0)
        {
            var newCollider = CreateColliderObject();
            var colliderScript = newCollider.GetComponent<ColliderScript>();
            colliderScript.Init();
            
            colliders.Add(colliderScript);
            return colliderScript;
        }

        foreach (var colliderScript in colliders)
        {
            if (colliderScript.isUsed)
            {
                continue;
            }

            numFree--;
            colliderScript.gameObject.SetActive(true);
            colliderScript.isUsed = true;

            return colliderScript;
        }

        throw new IndexOutOfRangeException("Should be at least one free colliders, but cannot find one");
    }

    public void DropCollider(ColliderScript collider)
    {
        if (!colliders.Contains(collider))
        {
            return;
        }

        collider.isUsed = false;
        collider.gameObject.SetActive(false);
        numFree++;
    }

    public void ShrinkCapacity()
    {
        List<ColliderScript> remainingColliders = new();

        foreach (var item in colliders)
        {
            if (item.isUsed)
            {
                remainingColliders.Add(item);
            }
            else
            {
                GameObject.Destroy(item.gameObject);
            }
        }
        
        colliders = remainingColliders;
        numFree = 0;
    }

    public IEnumerable<ColliderScript> EnumerateColliders()
    {
        return colliders.Where(x => x.isUsed);
    }
}