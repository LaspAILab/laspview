using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ExtendStructure : MonoBehaviour
{
    public Structure originStructure;
    //public Transform extendTranform;

    public Vector3 bias;

    public List<Atom> extendAtomList;
    public List<Bond> extendBondList;
    public List<LineRenderer> extendLatLineList;


    public void Init(Structure origin, int a0, int b0, int c0)
    {
        //alive = true;
        originStructure = origin;
        this.transform.SetParent(origin.transform);

        extendAtomList = new List<Atom> { };
        extendBondList = new List<Bond> { };
        extendLatLineList = new List<LineRenderer> { };

        bias = originStructure.GetCellTransfer(a0, b0, c0);

        foreach (Atom at in origin.atomList)
        {
            AddExtendAtom(at, bias);
        }
        //print(origin.bondList.Count);
        foreach (Bond bd in origin.bondList)
        {
            AddExtendBond(bd, bias);
        }
        AddExtendLatLine(bias);

    }

    private Bond AddExtendBond(Bond originBond, Vector3 bias)
    {
        //float stickRadius = originStructure.parent.arcModelPara["stick radius"];

        //a is the point to atom, b is the parent;
        Bond newBond = ObjectPool.S.RentBond();
        extendBondList.Add(newBond);


        int index = originBond.fromAtom.index;
        newBond.transform.SetParent(extendAtomList[index].transform.parent);

        newBond.transform.position = originBond.transform.position + bias;
        newBond.transform.rotation = originBond.transform.rotation;
        newBond.transform.localScale = originBond.transform.localScale;

        newBond.extendParent = this;
        newBond.originBond = originBond;
        newBond.fromAtom = null;
        newBond.pointToAtom = null;

        newBond.GetComponent<Renderer>().material.color = extendAtomList[index].GetComponent<Renderer>().material.color;


        return newBond;
    }

    private Atom AddExtendAtom(Atom originAtom, Vector3 bias)
    {

        Transform nodeTrans = UnityEngine.Object.Instantiate(originStructure.nodePrefabVar).transform;
        nodeTrans.SetParent(this.transform);
        nodeTrans.position = originAtom.transform.position + bias;

        Atom newAtom = UnityEngine.Object.Instantiate(originStructure.atomPrefabVar).GetComponent<Atom>();
        extendAtomList.Add(newAtom);

        //newAtom.nodeTrans = nodeTrans;
        newAtom.originAtom = originAtom;
        newAtom.transform.position = originAtom.transform.position + bias;
        newAtom.GetComponent<Renderer>().material.color = originAtom.color;
        newAtom.color = originAtom.color;
        newAtom.radius = originAtom.radius;
        newAtom.transform.localScale = originAtom.transform.localScale;
        newAtom.extendParent = this;

        newAtom.transform.SetParent(nodeTrans, true);
        //newAtom.transform.localPosition = Vector3.zero;

        originAtom.derivedAtom.Add(newAtom);

        return newAtom;
    }

    private void AddExtendLatLine(Vector3 bias)

    {
        Vector3[] strucCorner = new Vector3[8];
        //print(lat);
        for (int i = 0; i < 8; i++)
        {
            strucCorner[i] = originStructure.lat * ConFun.LatticeCorners[i];
        }
        Color lineColor = originStructure.latList[0].startColor;
        for (int i = 0; i < 4; i++)
            for (int j = 4; j < 8; j++)
                if (j != i + 4)
                {
                    //drawLine
                    LineRenderer line;
                    line = UnityEngine.Object.Instantiate(originStructure.latPrefabVar).GetComponent<LineRenderer>();
                    line.positionCount = 2;//设置两点
                    line.startColor = lineColor; //设置直线颜色
                    line.endColor = lineColor;
                    line.startWidth = 0.0015f * originStructure.GetCellTransfer(1, 1, 1).magnitude;//设置直线宽度
                    line.endWidth = 0.0015f * originStructure.GetCellTransfer(1, 1, 1).magnitude;

                    //设置指示线的起点和终点
                    line.SetPosition(0, strucCorner[i] + bias);
                    line.SetPosition(1, strucCorner[j] + bias);
                    line.transform.SetParent(originStructure.transform, true);
                    line.gameObject.SetActive(false);
                    extendLatLineList.Add(line);

                }
    }

    public void ChangeExtendBondModel()
    {
        Dictionary<string, float> modelPara = originStructure.parent.arcModelPara;
        foreach (Bond bd in extendBondList)
        {
            ObjectPool.S.RestoreBond(bd);
        }
        extendBondList.Clear();
        GC.Collect();
        //print(originStructure.bondList.Count);
        if (modelPara["display style"] == 0 || modelPara["display style"] == 2)
        {
            foreach (Bond bd in originStructure.bondList)
            {
                AddExtendBond(bd, bias);
            }
        }
    }

    ////public void RemoveExtendBond(Bond bd)
    //{
    //    this.extendBondList.Remove(bd);
    //    //UnityEngine.Object.Destroy(bd.gameObject);
    //}

    public void ChangeExtendLat()
    {
        foreach (LineRenderer lt in extendLatLineList)
        {
            GameObject.Destroy(lt.gameObject);
        }
        extendLatLineList.Clear();
        AddExtendLatLine(this.bias);
    }

    public void DestroyExtendStructrue()
    {

        foreach (Bond bd in extendBondList)
        {
            ObjectPool.S.RestoreBond(bd);
        }
        extendBondList.Clear();
        foreach (Atom at in extendAtomList)
        {
            at.originAtom.derivedAtom.Remove(at);
            GameObject.Destroy(at.transform.parent.gameObject);
        }
        extendAtomList.Clear();
        foreach (LineRenderer lt in extendLatLineList)
        {
            GameObject.Destroy(lt.gameObject);
        }
        extendLatLineList.Clear();

        bias = Vector3.zero;
    }
}
