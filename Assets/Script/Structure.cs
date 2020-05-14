//Class for a SINGLE structure of a arc file

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class Structure : MonoBehaviour
{
    //public GameObject structureprefabVr;
    public GameObject atomPrefabVar;
    public GameObject latPrefabVar;
    //public GameObject bondPrefabVar;
    public GameObject nodePrefabVar;
    public ArcStructures parent;

    //refererenced from structure.py of hsd Pymodule-cluster
    public int id =0;               //ordinal number
    public float energy = 0.0F;     //energy
    public float[] abc;             //a, b, c, alpha, betta, gamma of periodic condition
    public Matrix4x4 lat;          //matrix form of periodic contition
    public List<Atom> atomList;         //List of class & model Atom
    //public List<Atom> extandAtomList;
    public List<LineRenderer> latList;  //List of model of periodic condittion
    public List<Bond> bondList;         //List of class & modelBond
    //public List<Bond> extendBondList;         //List of class & modelBond
    public Transform latCenter;     //center of lattice, generally used as camera center
    public Vector3[] cellTranslate;
    public ExtendStructure[,,] extendStructs;
    public int currentExtendA = 1;
    public int currentExtendB = 1;
    public int currentExtendC = 1;
    public Dictionary<string, float> strucModelPara;


    public int pathid = 0;       //？？？
    public float conv = 0.0F;
    public float nminimum = 0.0F;
    public float numstr = 0.0F;
    public float nopathway = 0.0F;
    public bool ts = false;




    void Awake()
    {
        this.transform.position = Vector3.zero;
        extendStructs = new ExtendStructure[3,3,3];
    }

    public void AddAtom(string line, int type = 1)
    {
        string[] lineSp = ConFun.S.PySplit(line);
        if (type == 1)
        {
            Vector3 cdnt = new Vector3(float.Parse(lineSp[1]), float.Parse(lineSp[2]), float.Parse(lineSp[3]));
            AddAtom(lineSp[0], cdnt, (int)float.Parse(lineSp[8]));
        }
    }

    public Atom AddAtom(string ele, Vector3 cdnt,  int charge = 0)
    {
        
        GameObject node = Instantiate(nodePrefabVar);
        node.transform.position = cdnt;
        node.transform.SetParent(this.transform);

        Atom newAtom = Instantiate(atomPrefabVar).GetComponent<Atom>();
        newAtom.Init(cdnt, ele, node.transform, charge);

        newAtom.index = this.atomList.Count;
        this.atomList.Add(newAtom);

        newAtom.transform.SetParent(node.transform, true);
        newAtom.transform.localPosition = Vector3.zero;

        node.SetActive(false);
        return newAtom;
    }


    public void MoveIntoLatticAtoms(List<Atom> alist)
    {
        //Check if an Atom is in the Lattice
        //Only change postion, won't change cdnt
        Transform nodeTransform;

        Matrix4x4 inverse = this.lat.inverse;
        foreach (Atom a in alist)
        {
            nodeTransform = a.transform.parent;
            Vector3 fractionalPos = inverse * nodeTransform.position;

            for (int i = 0; i < 3; i++)
            {
                int n = Mathf.FloorToInt((fractionalPos[i] + 2e-5f));
                if (n != 0)
                {
                    Vector4 v = n * this.lat.GetColumn(i);
                    nodeTransform.position -= new Vector3(v[0], v[1], v[2]);
                }
            }

        }
    }

    public void AddAllBond( float bondBoundry)
    {
        if (bondList.Count != 0)
        {
            foreach(Bond b in bondList)
            {
                if (b != null)
                    ObjectPool.S.RestoreBond(b);
            }
        }
        bondList.Clear();
        GC.Collect();

        Vector3 dieta;
        float rate;
        if (atomList.Count >= 1)
        {
            for(int i = 0; i < atomList.Count; i++)
            {
                for ( int j = i+1; j<atomList.Count; j++)
                {
                    if (CheckBond(atomList[i], atomList[j], bondBoundry))
                    {
                        dieta = atomList[j].transform.position - atomList[i].transform.position;
                        rate = atomList[i].radius / (atomList[i].radius + atomList[j].radius);                  
                        bondList.Add(AddBond(atomList[i], atomList[j], dieta, rate));
                        bondList.Add(AddBond(atomList[j], atomList[i], -dieta, 1-rate));
                    }

                }
            }
        }
        if (atomList.Count >= 1)
        {
            //Vector3[] cellTranslate = { GetCellTransfer(1, 0, 0), GetCellTransfer(0, 1, 0), GetCellTransfer(0, 0, 1) };
            foreach (Vector3 vec in cellTranslate)
            {
                //print(vec);
                for (int i = 0; i < atomList.Count; i++)
                {
                    for (int j = 0; j < atomList.Count; j++)
                    {
                        if (CheckBond(atomList[i], atomList[j], vec, bondBoundry ))
                        {
                            dieta = atomList[j].transform.position - atomList[i].transform.position;
                            rate = atomList[i].radius / (atomList[i].radius + atomList[j].radius);
                            bondList.Add(AddBond(atomList[i], atomList[j], dieta + vec, rate, true));
                            bondList.Add(AddBond(atomList[j], atomList[i], -dieta - vec, 1-rate, true));
                        }
                    }
                }
            }
        }
    }
    
    public void AddAllBond()
    {
        this.AddAllBond(parent.arcModelPara["tolerance"]);
    }

    public bool CheckBond(Atom atom, Atom btom, float bondBoundry)
    {
        float len0 = (atom.radius + btom.radius);
        float len = (btom.transform.position - atom.transform.position).magnitude;
        if (len < len0 * bondBoundry)
        {
            return true;
        }
        else
            return false;
    }

    public bool CheckBond(Atom atom, Atom btom, Vector3 cellTranslate, float bondBoundry)
    {

        float len0 = (atom.radius + btom.radius);
        float len = (cellTranslate + btom.transform.position - atom.transform.position).magnitude;
        if (len < len0 * bondBoundry)
            return true;
        else
            return false;
    }

    public Bond AddBond(Atom a, Atom b, Vector3 direction, float rate, bool otherCell = false)
    {
        float stickRadius = parent.arcModelPara["stick radius"];

        //a is the point to atom, b is the parent;

        Bond newBond = ObjectPool.S.RentBond();
        //print(newBond.gameObject.name);
        newBond.Init(a, b, otherCell);

        newBond.transform.position = a.transform.position;
        newBond.transform.localRotation = Quaternion.identity;

        newBond.transform.Translate(direction * 0.5f * rate);
        //print(newBond.transform.position);
        newBond.transform.Rotate(new Vector3(-direction.z, 0, direction.x), -Vector3.Angle(Vector3.up, direction));
        //newBond.transform.localScale = Vector3.Scale(newBond.transform.localScale, new Vector3(1, direction.magnitude * rate / 2, 1));
        newBond.transform.localScale = new Vector3(stickRadius, direction.magnitude * rate / 2, stickRadius);
        newBond.GetComponent<Renderer>().material.color = a.GetComponent<Renderer>().material.color;
        newBond.transform.SetParent(a.transform.parent, true);

        return newBond;
    }




    public void AddLat(string line)
    {
        string[] lineSp = ConFun.S.PySplit(line);

        float[] abc = new float[6] { float.Parse(lineSp[1]), float.Parse(lineSp[2]), float.Parse(lineSp[3]),
                                     float.Parse(lineSp[4]),float.Parse(lineSp[5]),float.Parse(lineSp[6])};
        AddLat(abc);
    }


    public void AddLat(float[] abc)
    {
        //string[] lineSp = ConFun.S.PySplit(line);

        //float[] abc = new float[6] { float.Parse(lineSp[1]), float.Parse(lineSp[2]), float.Parse(lineSp[3]),
        //                             float.Parse(lineSp[4]),float.Parse(lineSp[5]),float.Parse(lineSp[6])};


        //bc2 = b * *2 + c * *2 - 2 * b * c * m.cos(alpha)
        //h1 = a
        //h2 = b * m.cos(gamma)
        //h3 = b * m.sin(gamma)
        //h4 = c * m.cos(beta)
        //h5 = ((h2 - h4) * *2 + h3 * *2 + c * *2 - h4 * *2 - bc2) / (2 * h3)
        //h6 = m.sqrt(c * *2 - h4 * *2 - h5 * *2)
        //abc[0:5]
        float bc2 = abc[1] * abc[1] + abc[2] * abc[2] - 2 * abc[1] * abc[2] * Mathf.Cos(abc[3] / 180.0f * Mathf.PI);
        float h1 = abc[0];
        float h2 = abc[1] * Mathf.Cos(abc[5] / 180.0f * Mathf.PI);
        float h3 = abc[1] * Mathf.Sin(abc[5] / 180.0f * Mathf.PI);
        float h4 = abc[2] * Mathf.Cos(abc[4] / 180.0f * Mathf.PI);
        float h5 = ((h2 - h4) * (h2 - h4) + h3 * h3 + abc[2] * abc[2] - h4 * h4 - bc2 ) /(2 * h3);
        float h6 = Mathf.Pow(abc[2] * abc[2] - h4 * h4 - h5 * h5, 0.5F);

        this.abc = abc;
        this.lat = new Matrix4x4(   new Vector4(h1,0,0,0),
                                    new Vector4(h2,h3,0,0),
                                    new Vector4(h4,h5,h6,0),
                                    new Vector4(0,0,0,1));
        //[h1,0,0]
        //[h2,h3,0]
        //[h4,h5,h6]



        cellTranslate = new Vector3[] { GetCellTransfer(1, 0, 0), GetCellTransfer(0, 1, 0), GetCellTransfer(0, 0, 1) };
    }

    void AddLatticeLine(Color lineColor)
    {
        Vector3[] strucCorner = new Vector3[8];
        //print(lat);
        for (int i = 0; i < 8; i++)
        {
            strucCorner[i] = this.lat * ConFun.S.latCorner[i];
        }
        for (int i = 0; i < 4; i++)
            for (int j = 4; j < 8; j++)
                if (j != i + 4)
                {
                    //drawLine
                    LineRenderer line;
                    line = Instantiate(latPrefabVar).GetComponent<LineRenderer>();
                    line.positionCount = 2;//设置两点
                    line.startColor = lineColor; //设置直线颜色
                    line.endColor = lineColor;
                    line.startWidth = 0.03f;//设置直线宽度
                    line.endWidth = 0.03f;

                    //设置指示线的起点和终点
                    line.SetPosition(0, strucCorner[i]);
                    line.SetPosition(1, strucCorner[j]);
                    line.transform.SetParent(this.transform, true);
                    line.gameObject.SetActive(false);
                    latList.Add(line);

                }
    }

    public void RmLattice() 
    {   
        foreach(LineRenderer l in latList)
        {
            UnityEngine.Object.Destroy(l);
        }
        latList.Clear();
    }

    private void SetStrucCollider()
    {
        //设置碰撞器半径//！！！
        Vector3 latticeMid = GetCellTransfer(currentExtendA, currentExtendB, currentExtendC) / 2;
        this.GetComponent<SphereCollider>().center = latticeMid;
        this.GetComponent<SphereCollider>().radius = Vector3.Magnitude(latticeMid);
    }


    public void DrawModel()
    {
        GuiMain.S.CameraActicate();

        this.MoveIntoLatticAtoms(this.atomList);

        Dictionary<string, float> modelPara = parent.arcModelPara;
        if (strucModelPara == null)
        {
            ChangeBackground();
            ChangeAtomModel(atomList);
            ChangeAllBondModel();
            ChangeLatRange(modelPara);

            strucModelPara = new Dictionary<string, float>(modelPara);
            this.gameObject.SetActiveRecursively(true);
        }
        else
        {
            if (strucModelPara["lat range a"] != modelPara["lat range a"] ||
           strucModelPara["lat range b"] != modelPara["lat range b"] ||
           strucModelPara["lat range c"] != modelPara["lat range c"])
            {
                ChangeLatRange(modelPara);
            }

            if (strucModelPara["display style"] != modelPara["display style"] ||
                strucModelPara["ball radius type"] != modelPara["ball radius type"] ||
                strucModelPara["ball scale"] != modelPara["ball scale"] ||
                strucModelPara["ball radius"] != modelPara["ball radius"])
            {
                ChangeAtomModel(atomList);
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            if (extendStructs[i, j, k] != null)
                            {
                                ChangeAtomModel(extendStructs[i, j, k].extendAtomList);
                            }
                        }
                    }
                }
            }

            //bool needRePaintBond = ;
            if (strucModelPara["display style"] != modelPara["display style"] || strucModelPara["tolerance"] != modelPara["tolerance"])
            {
                ChangeAllBondModel();

            }

            if (strucModelPara["stick radius"] != modelPara["stick radius"])
            {
                float radius = modelPara["stick radius"];
                foreach (Bond bd in bondList)
                {
                    bd.gameObject.transform.localScale = new Vector3(radius, bd.gameObject.transform.localScale.y, radius);
                }
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            if (extendStructs[i, j, k] != null)
                            {

                                foreach (Bond bd in extendStructs[i, j, k].extendBondList)
                                {
                                    bd.gameObject.transform.localScale = new Vector3(radius, bd.gameObject.transform.localScale.y, radius);
                                }
                            }
                        }
                    }
                }
            }

            if (strucModelPara["background"] != modelPara["background"])
            {
                ChangeBackground();
            }

            this.gameObject.SetActiveRecursively(true);
            strucModelPara = new Dictionary<string, float>(modelPara);
        }
       
    } 

    public void ChangeAtomModel(List<Atom> listOfAtom)
    {
        Dictionary<string, float> modelPara = parent.arcModelPara;
        if (modelPara["display style"] == 0 || modelPara["display style"] == 1)
        {
            if (modelPara["ball radius type"] == 0)
            {
                foreach (Atom at in listOfAtom) at.ChangeScalingRatio(modelPara["ball scale"]);
            }
            else if (modelPara["ball radius type"] == 1)
            {
                foreach (Atom at in listOfAtom) at.transform.localScale = modelPara["ball radius"] * Vector3.one;
            }
        }
        else if (modelPara["display style"] == 2)
        {
            foreach (Atom at in listOfAtom) at.transform.localScale = 0.4f * Vector3.one;
        }
    }

    public void ChangeAllBondModel()
    {
        Dictionary<string, float> modelPara = parent.arcModelPara;
        foreach (Bond bd in bondList)
        {
            ObjectPool.S.RestoreBond(bd);
        }
        bondList.Clear();

        if (modelPara["display style"] == 0 || modelPara["display style"] == 2)
        {
            this.AddAllBond();
        }
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    if (extendStructs[i, j, k] != null)
                    {
                        extendStructs[i, j, k].ChangeExtendBondModel();
                    }
                }
            }
        }
        GC.Collect();

    }

    public void ChangeBackground()
    {
        Dictionary<string, float> modelPara = parent.arcModelPara;
        if (modelPara["background"] == 0)
        {
            Camera.main.backgroundColor = Color.black;
        }
        else if (modelPara["background"] == 1)
        {
            Camera.main.backgroundColor = Color.grey;
        }
        else if (modelPara["background"] == 2)
        {
            Camera.main.backgroundColor = Color.white;
        }
        ChangeLat();
    }

    public void ChangeLat()
    {
        Dictionary<string, float> modelPara = parent.arcModelPara;
        RmLattice();
        AddLat(this.abc);
        if (modelPara["background"] == 2)
        {
            AddLatticeLine(Color.grey);
        }
        else
        {
            AddLatticeLine(Color.white);
        }
        SetStrucCollider();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    if (i == 0 && j == 0 && k == 0) continue;
                    if (extendStructs[i, j, k] != null)
                    {
                        extendStructs[i, j, k].ChangeExtendLat();
                    }
                }
            }
        }


    }

    public void ChangeLatRange(Dictionary<string, float> modelPara)
    {
        int a = (int)modelPara["lat range a"];
        int b = (int)modelPara["lat range b"];
        int c = (int)modelPara["lat range c"];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    if (i == 0 && j == 0 && k == 0) continue;
                    else if (extendStructs[i, j, k] == null && (i < a && j < b && k < c))
                    {
                        //print(((i, j, k), extendStructs[i, j, k], (i < a && j < b && k < c)));
                        extendStructs[i, j, k] = ObjectPool.S.RentExtendStructure();
                        extendStructs[i, j, k].Init(this, i, j, k);
                        //print(extendStructs[i, j, k].extendAtomList[0].originAtom.derivedAtom.Count);
                    }
                    else if (extendStructs[i, j, k] != null && (i >= a || j >= b || k >= c))
                    {
                        extendStructs[i, j, k].DestroyExtendStructrue();
                        ObjectPool.S.RestoreExtendStructrue(extendStructs[i, j, k]);
                        extendStructs[i, j, k] = null;
                    }
                }
            }
        }
        GC.Collect();
        currentExtendA = a;
        currentExtendB = b;
        currentExtendC = c;
        SetStrucCollider();
    }

    public Vector3 GetCellTransfer(int i, int j, int k)
    {
        //get ia + jb + kc, which a,b,c is the vector of lattice
        Vector4 vec4 = this.lat * new Vector4(i, j, k, 0);
        return new Vector3(vec4[0], vec4[1], vec4[2]);
    }


    public void DeDrawModel()
    {
        //foreach (Bond b in bondList)
        //{
        //    //Destroy all bond before setActive False to save memory
        //    UnityEngine.Object.Destroy(b.gameObject);
        //}
        //bondList.Clear();
    }

}

