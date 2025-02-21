using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


class ColliderPool
{
    public static ColliderPool GlobalPool;

    public GameObject ColliderPrefab;

    public GameObject ColliderParent;

    private List<GameObject> colliders;
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
            colliders.Add(newCollider);
            var colliderScript = newCollider.GetComponent<ColliderScript>();
            colliderScript.Init();
            return colliderScript;
        }

        foreach (var item in colliders)
        {
            var colliderScript = item.GetComponent<ColliderScript>();
            if (colliderScript.isUsed)
            {
                continue;
            }

            numFree--;
            colliderScript.Activate();
            return colliderScript;
        }

        throw new IndexOutOfRangeException("Should be at least one free colliders, but cannot find one");
    }

    public void DropCollider(ColliderScript collider, bool forceRecycle = false)
    {
        if (!colliders.Contains(collider.gameObject))
        {
            return;
        }

        collider.Drop();
        numFree++;

        if (forceRecycle)
        {
            colliders.RemoveAll(x => !x.GetComponent<ColliderScript>().isUsed);
            numFree = 0;
        }
    }
}