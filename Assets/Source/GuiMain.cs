using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System;
using UnityEngine.Serialization;

public class GuiMain : MonoBehaviour
{
    static public GuiMain S;
    public GameObject B_ToolbarPanelPrefab;
    public GameObject L_DisplayPanelPrefab;
    public GameObject L_FilePanelPrefab;
    public GameObject L_ProjectPanelPrefab;
    public GameObject R_LatticeEditPanelPrefab;
    public GameObject R_AtomEditPanelPrefab;
    public GameObject M_InfoPanelPrefab;
#if !UNITY_WEBGL
    //public GameObject M_LaspInWidgetPrefab;
    //public GameObject R_LASPPanelPrefab;
    public GameObject R_ServerPanelPrefab;
#endif

    GameObject menuPanel;
    public GameObject statusPanel;
    public GameObject leftCanvas;
    public GameObject rightCanvas;

    private Color UiPressedColor = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
    private Color UiUnPressedColor = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));

    //********************* VAR for MOUSE CONTROL*********************

    void Awake()
    {
        S = this;

        menuPanel = GameObject.Find("MenuPanel");
        statusPanel = GameObject.Find("StatusPanel");
        leftCanvas = GameObject.Find("LeftCanvas");
        rightCanvas = GameObject.Find("RightCanvas");
        SetMenuBtn();
    }

    public void ClearLeftPanel()
    {
        
        GameObject.Find("ProjectBtn").GetComponent<Button>().image.color = UiUnPressedColor;
        GameObject.Find("FileBtn").GetComponent<Button>().image.color = UiUnPressedColor;
        GameObject.Find("DisplayBtn").GetComponent<Button>().image.color = UiUnPressedColor;
        
        for (int i = 0; i < leftCanvas.transform.childCount; i++)
        {
            var go = leftCanvas.transform.GetChild(i).gameObject;
            if (go.name != "PlaceholderBlock")
            {
                go.SetActive(false);
            }
        }
    }

    // Relate Button with GameObjects, mainly panels, that can be hidden.
    public void BtnHideObject(Button btn, GameObject go = null)
    {
        if (go == null) return;
        if (go.activeSelf)
        {
            go.SetActive(false);
            btn.image.color = UiUnPressedColor;
        }
        else
        {
            go.SetActive(true);
            btn.image.color = UiPressedColor;
        }
    }

    public void SetMenuBtn()
    {
        GameObject b_ToolbarPanel = Instantiate(B_ToolbarPanelPrefab);
        GameObject l_ProjectPanel = Instantiate(L_ProjectPanelPrefab);
        GameObject l_FilePanel = Instantiate(L_FilePanelPrefab);
        GameObject l_DisplayPanel = Instantiate(L_DisplayPanelPrefab);
#if !UNITY_WEBGL
        // GameObject r_ServerPanel = Instantiate(R_ServerPanelPrefab);
        // GameObject r_LASPPanel = Instantiate(R_LASPPanelPrefab);
        // GameObject m_LaspInWidget = Instantiate(M_LaspInWidgetPrefab);
#endif
        GameObject l_AtomPanel = Instantiate(R_AtomEditPanelPrefab);
        GameObject l_EditPanel = Instantiate(R_LatticeEditPanelPrefab);
        GameObject m_InfoPanel = Instantiate(M_InfoPanelPrefab, transform, false);
    }

    public void SetAllUIInteract(GameObject go, bool interactable)
    {
        foreach (Selectable c in go.GetComponentsInChildren<Selectable>())
        {
            SetUIInteract(c, interactable);
        }
    }

    public void SetUIInteract(Selectable s, bool interactable)
    {
        if (interactable)
        {
            s.image.color = UiUnPressedColor;
            s.interactable = true;
        }
        else
        {
            s.image.color = UiPressedColor;
            s.interactable = false;
        }
    }
}