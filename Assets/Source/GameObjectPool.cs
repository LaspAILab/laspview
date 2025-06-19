using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// A poolable game object
public interface IPoolable
{
    public void Init();
    public void Enable();
    public void Disable();
    public void Destroy();

    public bool IsUsed
    {
        get;
    }
}

public class GameObjectPool<T> where T : IPoolable
{
    public GameObject GameObjectPrefab;
    public GameObject InstantiateParent;

    private List<T> objects;
    private int numFree;

    public GameObjectPool(GameObject gameObjectPrefab, GameObject instantiateParent)
    {
        objects = new();
        numFree = 0;
        GameObjectPrefab = gameObjectPrefab;
        InstantiateParent = instantiateParent;
    }

    private GameObject CreateObject()
    {
        return GameObject.Instantiate(GameObjectPrefab, InstantiateParent.transform);
    }

    public T BorrowObject()
    {
        if (numFree == 0)
        {
            var newCollider = CreateObject();
            var scriptObject = newCollider.GetComponent<T>();
            scriptObject.Init();
            
            objects.Add(scriptObject);
            return scriptObject;
        }

        foreach (var colliderScript in objects)
        {
            if (colliderScript.IsUsed)
            {
                continue;
            }

            numFree--;
            colliderScript.Enable();

            return colliderScript;
        }

        throw new IndexOutOfRangeException("Should be at least one free colliders, but cannot find one");
    }

    public void DropObject(T collider)
    {
        if (!objects.Contains(collider))
        {
            return;
        }

        collider.Disable();
        numFree++;
    }

    public void ShrinkCapacity()
    {
        List<T> remainingObjects = new();

        foreach (var item in objects)
        {
            if (item.IsUsed)
            {
                remainingObjects.Add(item);
            }
            else
            {
                item.Destroy();
            }
        }
        
        objects = remainingObjects;
        numFree = 0;
    }

    public IEnumerable<T> EnumerateObjects()
    {
        return objects.Where(x => x.IsUsed);
    }
}