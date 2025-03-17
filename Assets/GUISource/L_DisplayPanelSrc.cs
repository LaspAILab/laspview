using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class L_DisplayPanelSrc : MonoBehaviour
{
    static public L_DisplayPanelSrc S;
    static public syfPanelPara displayPanelPara;
    public static Dictionary<string, float> defaultModelPara;

    public Toggle showLatticeToggle;

    void Awake()
    {
        S = this;
        displayPanelPara = this.GetComponent<syfPanelPara>();
        displayPanelPara.SetParaDict();
        //this.DisplayModelPara();

        defaultModelPara = new Dictionary<string, float>
        {
            { "lat range a", 1.0f }, { "lat range b", 1.0f }, { "lat range c", 1.0f }, { "play interval", 0.2f },
            { "display style", 0.0f }, { "stick radius", 0.35f }, { "tolerance", 1.26f },
            { "ball radius type", 0.0f }, { "ball scale", 1f }, { "ball radius", 1.0f }, { "background", 0.0f }
        };
        DisplayLoadModelPara();

        //Set DisplayPanel
        this.transform.SetParent(GuiMain.S.leftCanvas.transform, false);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button dspButton = GameObject.Find("DisplayBtn").GetComponent<Button>();
        dspButton.onClick.AddListener(() =>
        {
            if (!gameObject.activeSelf)
            {
                GuiMain.S.ClearLeftPanel();
            }

            GuiMain.S.BtnHideObject(dspButton, this.gameObject);
        });
        this.gameObject.SetActive(false);
    }

    public void DisplayOnParaChange()
    {
        var modelPara = DisplayModelPara();
        var viewport = Camera.main.GetComponent<ViewportController>();
        viewport.SetLatticeRepeat((int)modelPara["lat range a"], (int)modelPara["lat range b"],
            (int)modelPara["lat range c"]);
        viewport.structureRenderer.SetAtomStyle(modelPara["ball scale"]);
        viewport.structureRenderer.SetBondStyle(modelPara["stick radius"]);
        viewport.latticeRenderer.SetDisplay(showLatticeToggle.isOn);
        App.Instance.BondTolerance = modelPara["tolerance"];

        SetBackground((int)modelPara["background"]);
        // MainScr.S.CurrentStructure().DrawModel();
        // GuiMain.S.FocusCameraCenter();
    }

    private void SetBackground(int type)
    {
        switch (type)
        {
            case 0:
                Camera.main.backgroundColor = Color.black;
                break;
            case 1:
                Camera.main.backgroundColor = new Color32(0, 0, 139, 255);
                break;
            case 2:
                Camera.main.backgroundColor = Color.grey;
                break;
            case 3:
                Camera.main.backgroundColor = Color.white;
                break;
        }
    }

    public void DisplayLoadModelPara()
    {
        Dictionary<string, float> arcPara = defaultModelPara;
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
                kvp.Value.GetComponent<InputField>().SetTextWithoutNotify(str);
            }
            else if (kvp.Value.GetComponent<Slider>())
            {
                float f = arcPara[kvp.Key];
                //if (kvp.Value.GetComponent<Slider>().onValueChanged.lis)
                kvp.Value.GetComponent<Slider>().onValueChanged
                    .SetPersistentListenerState(1, UnityEngine.Events.UnityEventCallState.Off);
                kvp.Value.GetComponent<Slider>().value = f;
                //print(kvp.Value.transform.parent.GetComponent<syfSliderValueText>());
                //kvp.Value.transform.parent.GetComponent<syfSliderValueText>().ChangeText("N0");
                kvp.Value.GetComponent<Slider>().onValueChanged
                    .SetPersistentListenerState(1, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            }
        }

        DisplayOnSytleChange();
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


    public void DisplayOnSytleChange()
    {
        int style = displayPanelPara.paraDict["display style"].GetComponent<Dropdown>().value;
        {
            if (style == 0)
            {
                displayPanelPara.paraDict["ball radius type"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["ball scale"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["ball radius"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["stick radius"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["tolerance"].transform.parent.gameObject.SetActive(true);
            }
            else if (style == 1)
            {
                displayPanelPara.paraDict["ball radius type"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["ball scale"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["ball radius"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["stick radius"].transform.parent.gameObject.SetActive(false);
                displayPanelPara.paraDict["tolerance"].transform.parent.gameObject.SetActive(false);
            }
            else if (style == 2)
            {
                displayPanelPara.paraDict["ball radius type"].transform.parent.gameObject.SetActive(false);
                displayPanelPara.paraDict["ball scale"].transform.parent.gameObject.SetActive(false);
                displayPanelPara.paraDict["ball radius"].transform.parent.gameObject.SetActive(false);
                displayPanelPara.paraDict["stick radius"].transform.parent.gameObject.SetActive(true);
                displayPanelPara.paraDict["tolerance"].transform.parent.gameObject.SetActive(true);
            }

            DisplayOnBallStyleChange();
        }
    }


    public void DisplayOnBallStyleChange()
    {
        int style = displayPanelPara.paraDict["ball radius type"].GetComponent<Dropdown>().value;
        if (style == 0)
        {
            displayPanelPara.paraDict["ball scale"].transform.parent.gameObject.SetActive(true);
            displayPanelPara.paraDict["ball radius"].transform.parent.gameObject.SetActive(false);
        }
        else if (style == 1)
        {
            displayPanelPara.paraDict["ball scale"].transform.parent.gameObject.SetActive(false);
            displayPanelPara.paraDict["ball radius"].transform.parent.gameObject.SetActive(true);
        }
    }
}