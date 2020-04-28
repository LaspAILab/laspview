using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class L_DisplayPanelSrc : MonoBehaviour
{
    static public L_DisplayPanelSrc S;
    static public syfPanelPara laspPanelPara;
    void Awake()
    {
        S = this;
        laspPanelPara = this.GetComponent<syfPanelPara>();
        laspPanelPara.SetParaDict();
        //this.DisplayModelPara();

        //Set DisplayPanel
        this.transform.SetParent(GuiMain.S.leftCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button dspButton = GameObject.Find("DisplayBtn").GetComponent<Button>();
        dspButton.onClick.AddListener(() =>
        {
            GuiMain.S.BtnHideObject(dspButton, this.gameObject);
        });
        this.gameObject.SetActive(false);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisplayOnParaChange()
    {
        MainScr.S.CurrentArc().modelPara = DisplayModelPara();
        MainScr.S.CurrentStructure().DrawModel();
    }

    public Dictionary<string, float> DisplayModelPara()
    {
        Dictionary<string, float> modelPara = new Dictionary<string, float>();
        foreach (KeyValuePair<string, GameObject> kvp in laspPanelPara.paraDict)
        {
            float v = 0;
            if (kvp.Value.GetComponent<Dropdown>())
            {
                v = kvp.Value.GetComponent<Dropdown>().value;
            }
            else if (kvp.Value.GetComponent<InputField>())
            {
                v = float.Parse(kvp.Value.GetComponent<InputField>().text);
            }
            modelPara.Add(kvp.Key, v);
        }
        return modelPara;
    }
}
