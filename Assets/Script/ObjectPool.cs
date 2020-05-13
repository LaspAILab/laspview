using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPool : MonoBehaviour
{
    static public ObjectPool S;

    //public GameObject atomPrefabVar;
    public GameObject bondPrefabVar;
    public GameObject nodePrefabVar;
    public GameObject extendPrefabVar;

    private Vector3 defaultBondScale = new Vector3(0.35f, 1, 0.35f);
    //private ObjectPool<Atom> atomPool = new ObjectPool<Atom>();
    public ObjectPool<Bond> bondPool = new ObjectPool<Bond>();
    //private ObjectPool<Transform> nodePool = new ObjectPool<Transform>();
    private ObjectPool<ExtendStructure> extendPool = new ObjectPool<ExtendStructure>();
    private Transform garbage;

    public Vector3 farBias = new Vector3(2500f, 0, 0);

    private void Awake()
    {
        S = this;
        //atomPool.Init(atomPrefabVar);
        bondPool.Init(bondPrefabVar);
        //nodePool.Init(nodePrefabVar);
        extendPool.Init(extendPrefabVar);

        garbage = Instantiate(nodePrefabVar).GetComponent<Transform>();
        garbage.transform.position = farBias;
        garbage.gameObject.name = "Garbage";
    }

    private void LateUpdate()
    {
        bondPool.UpdatePool();
        extendPool.UpdatePool();
        //print(garbage.childCount);
    }

    //public Atom RentAtom()
    //{
    //    return atomPool.Rent();
    //}

    //public void RestoreAtom(Atom a)
    //{
    //    a.gameObject.transform.position = farBias;
    //    a.derivedAtom.Clear();
    //    a.originAtom = null;
    //    a.extendParent = null;
    //    atomPool.Restore(a);
    //    a.transform.SetParent(garbage);
    //}

    //public Transform RentNode()
    //{
    //    return nodePool.Rent();
    //}

    //public void RestoreNode(Transform nodeTrans)
    //{
    //    nodeTrans.position = farBias;
    //    nodeTrans.DetachChildren();
    //    nodePool.Restore(nodeTrans);
    //    nodeTrans.transform.SetParent(garbage);
    //}

    public Bond RentBond()
    {
        Bond bd = bondPool.Rent();
        bd.originBond = bd;
        return bd;
    }

    public void RestoreBond(Bond bd)
    {
        if (bd.originBond == null)
        {
            return;
        }

        bd.transform.SetParent(garbage);
        bd.gameObject.transform.localPosition = Vector3.zero;
        bd.gameObject.transform.rotation = Quaternion.identity;
        bd.gameObject.transform.localScale = defaultBondScale;

        bd.fromAtom = null;
        bd.originBond = null;
        bd.pointToAtom = null;
        bd.extendParent = null;

        bondPool.Restore(bd);
        //GameObject.Destroy(bd.gameObject);
    }

    public ExtendStructure RentExtendStructure()
    {
        return extendPool.Rent();
    }

    public void RestoreExtendStructrue(ExtendStructure extend)
    {
        if (extend.originStructure == null)
        {
            return;
        }
        extend.originStructure = null;
        extend.transform.SetParent(garbage);
        extendPool.Restore(extend);
    }
}

public class ObjectPool<T>
{
    GameObject prefabGO;

    public List<T> pool = new List<T>();
    public List<T> temp = new List<T>();   
    public int max = 99999999;


    public void Init(GameObject go)
    {
        prefabGO = go;
    }

    public T Rent()
    {
        if (pool.Count == 0)
        {
            T component = GameObject.Instantiate(prefabGO).GetComponent<T>();
            //pool.Add(component);
            //used.Add(component);
            return component;
        }
        else
        {
            T componet = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            //used.Add(componet);
            return componet;
        }
    }

    public void UpdatePool()
    {
        if (temp.Count != 0)
        {
            pool.AddRange(temp);
            temp.Clear();
        }
    }
    public void Restore(T component)
    {
        //used.Remove(component);
        //if (!(temp.Contains(component)))
        //{
            temp.Add(component);
        //}

    }

}