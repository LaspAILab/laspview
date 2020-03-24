//Basic class of a Atom Model, containing information inlcuding: 
//element/ele, coordinate/position/cdnt, charge(reserved), color(read from ConFun)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom : MonoBehaviour
{
    public GameObject atomPrefabVar;
    public Color color;
    public Transform nodeTrans;

    public string ele;
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
        nodeTrans = node;
    }

    public void ChangeModel()
    {   //change atomPrefabVar when the model is changed;
        //reserve

    }

    public void ChangeRadius(float rate)
    {
        float scale = this.radius * rate;
        this.transform.localScale = scale * Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
