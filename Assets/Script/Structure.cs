//Class for a SINGLE structure of a arc file

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Structure : MonoBehaviour
{
    //public GameObject structureprefabVr;
    public GameObject atomPrefabVar;
    public GameObject latPrefabVar;
    public GameObject bondPrefabVar;
    public GameObject nodePrefabVar;
    public ArcStructures parent;

    //refererenced from structure.py of hsd Pymodule-cluster
    public int id =0;               //ordinal number
    public float energy = 0.0F;     //energy
    public float[] abc;             //a, b, c, alpha, betta, gamma of periodic condition
    public Matrix4x4 lat;          //matrix form of periodic contition
    public List<Atom> atomList;         //List of class & model Atom
    public List<Atom> extandAtomList;
    public List<LineRenderer> latList;  //List of model of periodic condittion
    public List<Bond> bondList;         //List of class & modelBond
    public List<Bond> extendBondList;         //List of class & modelBond
    public Transform latCenter;     //center of lattice, generally used as camera center
    public Vector3[] cellTranslate;

    public int pathid = 0;       //？？？
    public float conv = 0.0F;
    public float nminimum = 0.0F;
    public float numstr = 0.0F;
    public float nopathway = 0.0F;
    public bool ts = false;

    private List<Atom> pickAtom; //for pick Atom
    private List<Bond> changeBond = new List<Bond>(); 


    void Awake()
    {
        this.transform.position = Vector3.zero;
    }

    public void AddAtom(string line, int type = 1)
    {
        string[] lineSp = ConFun.S.PySplit(line);
        if (type == 1)
        {
            Vector3 cdnt = new Vector3(float.Parse(lineSp[1]), float.Parse(lineSp[2]), float.Parse(lineSp[3]));
            AddAtom(lineSp[0],cdnt,  (int)float.Parse(lineSp[8]));
        }
    }

    public Atom AddAtom(string ele, Vector3 cdnt,  int charge = 0)
    {
        
        GameObject node = Instantiate(nodePrefabVar);
        node.transform.position = cdnt;
        node.transform.SetParent(this.transform);

        Atom newAtom = Instantiate(atomPrefabVar).GetComponent<Atom>();
        newAtom.Init(cdnt, ele, node.transform, charge);
        this.atomList.Add(newAtom);
        newAtom.transform.SetParent(node.transform, true);
        newAtom.transform.localPosition = Vector3.zero;

        node.SetActive(false);
        return newAtom;
    }

    private Bond AddExtendBond(Bond originBond, Vector3 bias)
    {
        float stickRadius = parent.modelPara["stick radius"];

        //a is the point to atom, b is the parent;
        Bond newBond = Instantiate(bondPrefabVar).GetComponent<Bond>();
        extendBondList.Add(newBond);

        newBond.transform.position = originBond.transform.position + bias;
        newBond.transform.rotation = originBond.transform.rotation;
        newBond.transform.localScale = originBond.transform.localScale;
        newBond.transform.parent = originBond.transform.parent;
        newBond.GetComponent<Renderer>().material.color = originBond.GetComponent<Renderer>().material.color;

        return newBond;
    }


    private Atom AddExtendAtom(Atom originAtom, Vector3 bias)
    {
        Atom newAtom = Instantiate(atomPrefabVar).GetComponent<Atom>();
        this.extandAtomList.Add(newAtom);

        newAtom.transform.position = originAtom.transform.position + bias;
        newAtom.transform.rotation = originAtom.transform.rotation;
        newAtom.transform.localScale = originAtom.transform.localScale;
        newAtom.GetComponent<Renderer>().material.color = originAtom.color;

        newAtom.transform.SetParent(originAtom.transform.parent);
        newAtom.nodeTrans = originAtom.transform.parent;
        newAtom.originAtom = originAtom;

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
                if(b != null)
                    Object.Destroy(b.gameObject);
            }
        }
        bondList.Clear();

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
                        dieta = atomList[j].nodeTrans.position - atomList[i].nodeTrans.position;
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
                            dieta = atomList[j].nodeTrans.position - atomList[i].nodeTrans.position;
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
        this.AddAllBond(parent.modelPara["bond boundry"]);
    }

    public bool CheckBond(Atom atom, Atom btom, float bondBoundry)
    {
        float len0 = (atom.radius + btom.radius);
        float len = (btom.nodeTrans.position - atom.nodeTrans.position).magnitude;
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
        float len = (cellTranslate + btom.nodeTrans.position - atom.nodeTrans.position).magnitude;
        if (len < len0 * bondBoundry)
            return true;
        else
            return false;
    }

    public Bond AddBond(Atom a, Atom b, Vector3 direction, float rate, bool otherCell = false)
    {
        float stickRadius = parent.modelPara["stick radius"];

        //a is the point to atom, b is the parent;
        Bond newBond = Instantiate(bondPrefabVar).GetComponent<Bond>();
        newBond.Init(b, otherCell);
        newBond.transform.position = a.nodeTrans.position;
        newBond.transform.Translate(direction * 0.5f * rate);
        newBond.transform.Rotate(new Vector3(-direction.z, 0, direction.x), -Vector3.Angle(Vector3.up, direction));
        //newBond.transform.localScale = Vector3.Scale(newBond.transform.localScale, new Vector3(1, direction.magnitude * rate / 2, 1));
        newBond.transform.localScale = new Vector3(stickRadius, direction.magnitude * rate / 2, stickRadius);
        newBond.GetComponent<Renderer>().material.color = a.GetComponent<Renderer>().material.color;
        newBond.transform.SetParent(a.transform.parent);
        return newBond;
    }


    public void PickControlStart(List<GameObject> pickGO)
    {
        pickAtom = new List<Atom>();
        foreach (GameObject go in pickGO)
        {
            pickAtom.Add(go.GetComponent<Atom>());
        }

        foreach( Atom a in pickAtom)
        {
            foreach(Bond b1 in a.transform.parent.GetComponentsInChildren<Bond>())
            {
                //remove Bond who is from Pick Atom a to Unpick Atom 
                if (b1.point2OtherCell == true || !pickAtom.Contains(b1.pointToAtom)) 
                {
                    
                    bondList.Remove(b1);
                    Object.Destroy(b1.gameObject);
                    //remove Bond who is from Unpick Atom to Pick Atom a
                    foreach (Bond b2 in b1.pointToAtom.gameObject.transform.parent.GetComponentsInChildren<Bond>())
                    {
                        if(b2.pointToAtom == a)
                        {
                            bondList.Remove(b2);
                            Object.Destroy(b2.gameObject);
                        }
                    }
                }
            }

        }

    }

    public void PickControlUpdate()
    {
        Vector3 dieta;
        float rate;
        float bondBoundry = parent.modelPara["bond boundry"];
        Matrix4x4 inverse = this.lat.inverse;

        if (changeBond.Count != 0)
        {
            foreach (Bond b in changeBond)
            {
                Object.Destroy(b.gameObject);
            }
        }
        changeBond.Clear();

        foreach (Atom a1 in pickAtom)
        {
            //Vector3 p1 = a1.transform.parent.position;
            //Vector3 fractionalPos = inverse * p1;
            //int[] vecs = new int[3];
            //for (int i = 0; i < 3; i++)
            //{
            //    vecs[i] = Mathf.FloorToInt(fractionalPos[i] + 1.5e-5f);
            //    print(vecs[i]);
            //}

            foreach (Atom a2 in atomList)
            {
                Vector3 p2 = a2.transform.parent.position;

                if (CheckBond(a1, a2, bondBoundry)) 
                {
                    dieta = a2.nodeTrans.position - a1.nodeTrans.position;
                    rate = a1.radius / (a1.radius + a2.radius);
                    changeBond.Add(AddBond(a1, a2, dieta, rate));
                    changeBond.Add(AddBond(a2, a1, -dieta, 1 - rate));
                }
                //for (int i = 0; i < 3; i++)
                //{
                //    Vector3 vec = cellTranslate[i] * vecs[i];
                //    if (CheckBond(a1, a2, vec, bondBoundry))
                //    {
                //        dieta = a2.nodeTrans.position - a1.nodeTrans.position;
                //        rate = a1.radius / (a1.radius + a2.radius);
                //        changeBond.Add(AddBond(a1, a2, dieta + vec, rate));
                //        changeBond.Add(AddBond(a2, a1, -dieta - vec, 1 - rate));
                //    }
                //}

            }
        }

        //Update Extend Atom & Bond
        
        //foreach (Vector3 vec in cellTranslate)
        //{
        //    foreach (Atom a1 in pickAtom)
        //    {
        //        foreach (Atom a2 in atomList)
        //        {
                    

        //        }
        //    }
        //}
    }

    public void PickControlEnd()
    {
        this.AddAllBond();

        MoveIntoLatticAtoms(pickAtom);

        changeBond.Clear();
        pickAtom.Clear();  
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
        //print(this.lat);
        AddLatticeLine();

        //设置碰撞器半径//！！！
        Vector3 latticeMid = GetCellTransfer(1, 1, 1) / 2;
        this.GetComponent<SphereCollider>().center = latticeMid;
        this.GetComponent<SphereCollider>().radius = Vector3.Magnitude(latticeMid)+1.0f;
        cellTranslate = new Vector3[] { GetCellTransfer(1, 0, 0), GetCellTransfer(0, 1, 0), GetCellTransfer(0, 0, 1) };
    }

    void AddLatticeLine()
    {

        Vector3[] strucCorner = new Vector3[8];
        //print(lat);
        for (int i = 0; i < 8; i++)
        {
            strucCorner[i] = this.lat * ConFun.S.latCorner[i];
            //print(this.lat);
            //print(ConFun.S.latCorner[i]);
            //print(strucCorner[i]);
        }

        for (int i = 0; i < 4; i++)
            for (int j = 4; j < 8; j++)
                if (j != i + 4)
                    latList.Add(DrawLine(strucCorner[i], strucCorner[j]));

    }

    public Vector3 RmLattice() 
    {   
        foreach(LineRenderer l in latList)
        {
            Object.Destroy(l);
        }
        latList.Clear();
        return GetCellTransfer(1,1,1);
    }


    public void DrawModel()
    {
        Dictionary<string, float> modelPara = parent.modelPara;

        if (modelPara["display style"] == 0 || modelPara["display style"] == 1)
        {
            if (modelPara["ball radius type"] == 0)
            {
                foreach (Atom a in atomList) a.ChangeScalingRatio(modelPara["ball scaling ratio"]);
                //foreach (Atom a in extandAtomList) a.ChangeScalingRatio(modelPara["ball scaling ratio"]);
            }
            else if (modelPara["ball radius type"] == 1)
            {
                foreach (Atom a in atomList) a.transform.localScale = modelPara["stick radius"] * Vector3.one;
                //foreach (Atom a in extandAtomList) a.transform.localScale = modelPara["stick radius"] * Vector3.one;
            }
        }
        else if (modelPara["display style"] == 2)
        {
            foreach (Atom a in atomList) a.transform.localScale = 0.4f * Vector3.one;
            //foreach (Atom a in extandAtomList) a.transform.localScale = 0.4f * Vector3.one;

        }

        foreach(Atom  at in extandAtomList)
        {
            Object.Destroy(at.gameObject);
        }
        extandAtomList.Clear();

        if (modelPara["lat range a"] != 1 || modelPara["lat range b"] != 1 || modelPara["lat range c"] != 1)
        {
            for (int a = 0; a <= modelPara["lat range a"] - 1; a++)
            {
                for (int b = 0; b <= modelPara["lat range b"] - 1; b++)
                {
                    for (int c = 0; c <= modelPara["lat range c"] - 1; c++)
                    {
                        if (a == 0 && b == 0 && c == 0) continue;
                        foreach (Atom at in atomList)
                        {
                            this.AddExtendAtom(at, GetCellTransfer(a, b, c));
                        }
                    }
                }
            }
        }

        this.MoveIntoLatticAtoms(this.atomList);
        //warning:这里怎么改再说了

        foreach (Bond b in bondList)
        {
            Object.Destroy(b.gameObject);
        }
        bondList.Clear();
        foreach (Bond b in extendBondList)
        {
            Object.Destroy(b.gameObject);
        }
        extendBondList.Clear();

        if (modelPara["display style"] == 0 || modelPara["display style"] == 2)
        {
            this.AddAllBond();
        }

        if (modelPara["lat range a"] != 1 || modelPara["lat range b"] != 1 || modelPara["lat range c"] != 1)
        {
            for (int a = 0; a <= modelPara["lat range a"] - 1; a++)
            {
                for (int b = 0; b <= modelPara["lat range b"] - 1; b++)
                {
                    for (int c = 0; c <= modelPara["lat range c"] - 1; c++)
                    {
                        if (a == 0 && b == 0 && c == 0) continue;
                        foreach (Bond bd in bondList)
                        {
                            this.AddExtendBond(bd, GetCellTransfer(a, b, c));
                        }
                    }
                }
            }
        }
        this.gameObject.SetActiveRecursively(true);
    } 


    private LineRenderer DrawLine(Vector3 initPos ,Vector3 endPos)
    {
        //drawLine
        LineRenderer line;
        line = Instantiate(latPrefabVar).GetComponent<LineRenderer>();
        line.positionCount = 2;//设置两点
        line.startColor = Color.white; //设置直线颜色
        line.endColor = Color.white;
        line.startWidth = 0.05f;//设置直线宽度
        line.endWidth = 0.05f;

        //设置指示线的起点和终点
        line.SetPosition(0, initPos);
        line.SetPosition(1,  endPos);
        line.transform.SetParent(this.transform,true);
        line.gameObject.SetActive(false);
        return line;

    }

    public Vector3 GetCellTransfer(int i, int j, int k)
    {
        //get ia + jb + kc, which a,b,c is the vector of lattice
        Vector4 vec4 = this.lat * new Vector4(i, j, k, 0);
        return new Vector3(vec4[0], vec4[1], vec4[2]);
    }

}


class ExtendStructrue
{
    public int a;
    public int b;
    public int c;
    public List<Atom> extendAtomList;
    public List<Bond> extendBondList;

    public ExtendStructrue ExtendStructureFrom(Structure origin, Vector3 latRange)
    {


        return this;
    }


}
