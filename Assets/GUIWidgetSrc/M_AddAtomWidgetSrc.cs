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

        //Set Add Atom Widget
        this.transform.SetParent(GameObject.Find("PhysicalResizer").transform);
        this.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        this.gameObject.SetActive(false);
    }

    public void EditWidgetAddAtom()
    {
        string ele = paraDict["element"].GetComponent<InputField>().text;
        float x = float.Parse(paraDict["x"].GetComponent<InputField>().textComponent.text);
        float y = float.Parse(paraDict["y"].GetComponent<InputField>().textComponent.text);
        float z = float.Parse(paraDict["z"].GetComponent<InputField>().textComponent.text);
        Vector3 pos = new Vector3(x, y, z);

        if (ele.Trim() == "")
        {
            ConFun.printStatusInfo("Err: No Element");
            return;
        }
        if (!ConFun.elements.ContainsKey(ele))
        {
            ConFun.printStatusInfo("Err: " + ele + "is not an available element.");
            return;
        }

        Atom p = MainScr.S.CurrentStructure().AddAtom(ele, pos);
        GuiMain.S.EditSaveStruc(new List<Atom> { p }, 'c');
        //Cancel pickGO
        GuiMain.S.DisSelectPickGO();
        MainScr.S.CurrentStructure().ReDrawModel();

        GuiMain.S.pickGO = new List<GameObject> { p.gameObject };
        foreach (Renderer ren in p.transform.parent.GetComponentsInChildren<Renderer>())
        {
            ren.material.color = Color.yellow;
        }
        GuiMain.S.EditSaveStruc(new List<Atom> { p });
    }


}
