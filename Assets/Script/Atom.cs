//Basic class of a Atom Model, containing information inlcuding: 
//element/ele, coordinate/position/cdnt, charge(reserved), color(read from ConFun)

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Atom : MonoBehaviour
{
    public GameObject atomPrefabVar;

    //public Transform nodeTrans;
    public Atom originAtom;
    public ExtendStructure extendParent; 
    public List<Atom> derivedAtom = new List<Atom>();
    public int index;

    public string ele;
    public Color color;
    public Vector3 cdnt;
    public int charge;
    public float radius;
    

    // Start is called before the first frame update
    public void Init(Vector3 cdnt, string ele, Transform node, int charge = 0)
    {
        this.ele = ele;
        this.cdnt = cdnt;
        this.charge = charge;
        this.radius = 2 * ConFun.S.eleRadius[ele];
        this.color = ConFun.S.eleColor[ele];
        Renderer render = this.GetComponent<Renderer>();
        render.material.color = this.color;

        //nodeTrans = node;
        this.originAtom = this;
        extendParent = null;
    }


    public void ChangeScalingRatio(float ratio)
    {
        float scale = Mathf.Max( this.radius * ratio, 0.4f);
        this.transform.localScale = scale * Vector3.one;
    }
}
