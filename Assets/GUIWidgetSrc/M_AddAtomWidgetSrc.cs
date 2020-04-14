using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class M_AddAtomWidgetSrc : MonoBehaviour
{
    static public M_AddAtomWidgetSrc S;

    static public Dictionary<string, GameObject> paraDict;
    public Button closeBtn;

    void Awake()
    {
        S = this;
        syfPanelPara p =  this.GetComponent<syfPanelPara>();
        p.SetParaDict();
        paraDict = p.paraDict;

        closeBtn = this.transform.GetChild(0).GetComponentInChildren<Button>();
        foreach (GameObject g in paraDict.Values) print(g);
    }
    // Start is called before the first frame update
    
    public void EditWidgetAddAtom()
    {
        print(paraDict["x"].GetComponent<InputField>().textComponent);
        string ele = paraDict["element"].GetComponent<InputField>().text;
        if (!ConFun.S.eleTable.ContainsKey(ele))
        {
            ConFun.S.printStatusInfo(ele + "is not an available element.Use element name such as H, Si, Cu, Uup.");
            return;
        }
        float x = float.Parse(paraDict["x"].GetComponent<InputField>().textComponent.text);
        float y = float.Parse(paraDict["y"].GetComponent<InputField>().textComponent.text);
        float z = float.Parse(paraDict["z"].GetComponent<InputField>().textComponent.text);

        Vector3 pos = new Vector3(x, y, z);
        Atom p = MainScr.S.CurrentStructure().AddAtom(ele, pos);

        //Cancel pickGO
        foreach (GameObject go in GuiMain.S.pickGO)
        {
            foreach (Renderer ren in go.transform.parent.GetComponentsInChildren<Renderer>())
            {
                ren.material.color = go.GetComponent<Atom>().color;
            }
        }
        MainScr.S.CurrentArc().DrawStructure(MainScr.S.CurrentArc().currentStr);

        GuiMain.S.pickGO = new List<GameObject> { p.gameObject };
        foreach (Renderer ren in p.transform.parent.GetComponentsInChildren<Renderer>())
        {
            ren.material.color = Color.yellow;
        }
    }


}
