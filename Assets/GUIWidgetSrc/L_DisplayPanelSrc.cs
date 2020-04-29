using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class L_DisplayPanelSrc : MonoBehaviour
{
    static public L_DisplayPanelSrc S;
    static public syfPanelPara displayPanelPara;
    static public Dictionary<string, float> defaultModelPara;

    void Awake()
    {
        S = this;
        displayPanelPara = this.GetComponent<syfPanelPara>();
        displayPanelPara.SetParaDict();
        //this.DisplayModelPara();

        defaultModelPara = new Dictionary<string, float> { { "lat range a", 1.0f }, { "lat range b", 1.0f }, { "lat range c", 1.0f }, { "play interval", 0.2f },
                                                            { "display style", 0.0f }, { "stick radius", 0.35f }, { "bond boundry", 0.63f },
                                                             { "ball radius type", 0.0f },{ "ball scaling ratio", 0.5f },{ "ball radius", 1.0f }};

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

    public void DisplayLoadModelPara()
    {
        Dictionary<string, float> arcPara = MainScr.S.CurrentArc().modelPara;
        foreach (KeyValuePair<string, GameObject> kvp in displayPanelPara.paraDict)
        {
            if (kvp.Value.GetComponent<Dropdown>())
            {
                int index = int.Parse(arcPara[kvp.Key].ToString());
                kvp.Value.GetComponent<Dropdown>().SetValueWithoutNotify(index);
            }
            else if (kvp.Value.GetComponent<InputField>())
            {
                string str = string.Format("{0:f3}", arcPara[kvp.Key].ToString());
                kvp.Value.GetComponent<InputField>().SetTextWithoutNotify( str);
            }
            else if (kvp.Value.GetComponent<Slider>())
            {
                float f = arcPara[kvp.Key];
                kvp.Value.GetComponent<Slider>().SetValueWithoutNotify(f);
                //print(kvp.Value.transform.parent.GetComponent<syfSliderValueText>());
                kvp.Value.transform.parent.GetComponent<syfSliderValueText>().ChangeText("N0");
            }
        }
    }


    public Dictionary<string, float> DisplayModelPara()
    {
        Dictionary<string, float> modelPara = new Dictionary<string, float>();
        foreach (KeyValuePair<string, GameObject> kvp in displayPanelPara.paraDict)
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
            else if (kvp.Value.GetComponent<Slider>())
            {
                v = kvp.Value.GetComponent<Slider>().value;
            }
            modelPara.Add(kvp.Key, v);
            //print((kvp.Key, v));
        }
        return modelPara;
    }
}
