using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class M_EditLatticeWidgetSrc : MonoBehaviour
{
    static public M_EditLatticeWidgetSrc S;

    static public Dictionary<string, GameObject> paraDict;
    public Button closeBtn;

    void Awake()
    {
        S = this;

        syfPanelPara p = this.GetComponent<syfPanelPara>();
        p.SetParaDict();
        paraDict = p.paraDict;

        closeBtn = this.transform.GetChild(0).GetComponentInChildren<Button>();

        //Set Edit Lattice Widget
        this.transform.SetParent(GameObject.Find("PhysicalResizer").transform);
        this.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        this.gameObject.SetActive(false);
    }
    public void EditWidgetEditLattice()
    {
        float a = float.Parse(paraDict["a"].GetComponent<InputField>().textComponent.text);
        float b = float.Parse(paraDict["b"].GetComponent<InputField>().textComponent.text);
        float c = float.Parse(paraDict["c"].GetComponent<InputField>().textComponent.text);
        float alpha = float.Parse(paraDict["alpha"].GetComponent<InputField>().textComponent.text);
        float betta = float.Parse(paraDict["betta"].GetComponent<InputField>().textComponent.text);
        float gamma = float.Parse(paraDict["gamma"].GetComponent<InputField>().textComponent.text);
        MainScr.S.CurrentStructure().ChangeLatPara( new float[6] { a, b, c, alpha, betta, gamma }, paraDict["keep fractional position"].GetComponent<Toggle>().isOn);
    }




    public void SetLatPara(float[] abc)
    {
        paraDict["a"].GetComponent<InputField>().text = abc[0].ToString();
        paraDict["b"].GetComponent<InputField>().text = abc[1].ToString();
        paraDict["c"].GetComponent<InputField>().text = abc[2].ToString();
        paraDict["alpha"].GetComponent<InputField>().text = abc[3].ToString();
        paraDict["betta"].GetComponent<InputField>().text = abc[4].ToString();
        paraDict["gamma"].GetComponent<InputField>().text = abc[5].ToString();

    }
}
