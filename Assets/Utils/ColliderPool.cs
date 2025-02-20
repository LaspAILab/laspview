using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


class ColliderPool
{
    public static ColliderPool GlobalPool;

    public GameObject colliderPrefab;

    public GameObject colliderParent;

    List<GameObject> colliders;
    int numFree = 0;

    public ColliderPool(GameObject colliderPrefab, GameObject colliderParent)
    {
        colliders = new();
        this.colliderPrefab = colliderPrefab;
        this.colliderParent = colliderParent;
    }

    private GameObject CreateColliderObject()
    {
        return GameObject.Instantiate(colliderPrefab, colliderParent.transform);
    }

    public ColliderScript BorrowCollider()
    {
        if (numFree == 0)
        {
            var newCollider = CreateColliderObject();
            colliders.Add(newCollider);
            return newCollider.GetComponent<ColliderScript>();
        }
        else
        {
            foreach (var item in colliders)
            {
                var colliderScript = item.GetComponent<ColliderScript>();
                if (!colliderScript.IsUsed)
                {
                    numFree--;
                    colliderScript.Activate();
                    return colliderScript;
                }
            }

            throw new IndexOutOfRangeException("Should be at least one free colliders, but cannot find one");
        }
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
            colliders.RemoveAll(x => !x.GetComponent<ColliderScript>().IsUsed);
            numFree = 0;
        }
    }
}
