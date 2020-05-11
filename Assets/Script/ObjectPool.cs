using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPool : MonoBehaviour
{
    static public ObjectPool S;

    public GameObject atomPrefabVar;
    public GameObject bondPrefabVar;
    public GameObject nodePrefabVar;
    public GameObject extendPrefabVar;


    //private ObjectPool<Atom> atomPool = new ObjectPool<Atom>();
    private ObjectPool<Bond> bondPool = new ObjectPool<Bond>();
    //private ObjectPool<Transform> nodePool = new ObjectPool<Transform>();
    //private ObjectPool<ExtendStructure> extendPool = new ObjectPool<ExtendStructure>();
    private Transform garbage;

    public Vector3 farBias = new Vector3(2500f, 0, 0);

    private void Awake()
    {
        S = this;
        //atomPool.Init(atomPrefabVar);
        bondPool.Init(bondPrefabVar);
        //nodePool.Init(nodePrefabVar);
        //extendPool.Init(extendPrefabVar);

        garbage = Instantiate(nodePrefabVar).GetComponent<Transform>();
        garbage.gameObject.name = "Garbage";
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
        return bondPool.Rent();
    }

    public void RestoreBond(Bond bd)
    {
        bd.gameObject.transform.position = farBias;
        bd.originAtom = null;
        bd.originBond = null;
        bd.pointToAtom = null;
        bd.extendParent = null;
        bd.gameObject.transform.rotation = Quaternion.identity;

        bondPool.Restore(bd);
        bd.transform.SetParent(garbage);
    }

    //public ExtendStructure RentExtendStructure()
    //{
    //    return extendPool.Rent();
    //}

    //public void RestoreExtendStructrue(ExtendStructure extend)
    //{

    //    foreach (Bond bd in extend.extendBondList)
    //    {
    //        RestoreBond(bd);
    //    }
    //    extend.extendBondList.Clear();
    //    foreach (Atom at in extend.extendAtomList)
    //    {
    //        RestoreNode(at.transform.parent);
    //        at.originAtom.derivedAtom.Remove(at);
    //        RestoreAtom(at);
    //    }
    //    extend.extendAtomList.Clear();
    //    foreach (LineRenderer lt in extend.extendLatLineList)
    //    {
    //        GameObject.Destroy(lt.gameObject);
    //    }
    //    extend.extendLatLineList.Clear();
    //}
}

class ObjectPool<T>
{
    GameObject prefabGO;

    public List<T> pool = new List<T>();
    List<T> used = new List<T>();
    //int poolIndex = 0;
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
            used.Add(component);
            return component;
        }
        else
        {
            T componet = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            used.Add(componet);
            return componet;
        }
    }

    public void Restore(T component)
    {
        used.Remove(component);
        pool.Add(component);
    }

}