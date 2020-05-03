using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class L_EditPanelSrc : MonoBehaviour
{
    static public L_EditPanelSrc S;

    public Dictionary<string, GameObject> paraDict;

    void Awake()
    {
        S = this;
        syfPanelPara p = this.GetComponent<syfPanelPara>();
        p.SetParaDict();
        paraDict = p.paraDict;

        //Set Edit panel
        this.transform.SetParent(GuiMain.S.leftCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button edButton = GameObject.Find("EditBtn").GetComponent<Button>();
        edButton.onClick.AddListener(() =>
        {
            GuiMain.S.BtnHideObject(edButton, this.gameObject);
            GuiMain.S.editEnable = !GuiMain.S.editEnable;
            GuiMain.S.SetAllUIInteract(L_ProjectPanelSrc.S.gameObject, !GuiMain.S.editEnable);
            GuiMain.S.SetAllUIInteract(L_FilePanelSrc.S.gameObject, !GuiMain.S.editEnable);
#if !UNITY_WEBGL
            GuiMain.S.SetAllUIInteract(R_LASPPanelSrc.S.gameObject, !GuiMain.S.editEnable);
#endif
            if (GuiMain.S.editEnable == true)
            {
                //GuiMain.S.DisSelectPickGO();
                foreach (Selectable c in B_ToolbarPanelSrc.S.gameObject.GetComponentsInChildren<Selectable>())
                {
                    if (c.gameObject.name != "FocusCenterBtn")
                        GuiMain.S.SetUIInteract(c, false);
                }
#if !UNITY_WEBGL
                foreach (Selectable c in R_ServerPanelSrc.S.gameObject.GetComponentsInChildren<Selectable>())
                {
                    if (c.gameObject.name == "StartListeningBtn")
                    {

                        MainScr.S.localServer.Quit2();
                        R_ServerPanelSrc.S.lclSrvrListening = false;
                    }
                }
                R_ServerPanelSrc.S.NetLocalBtnChange();
                GuiMain.S.SetAllUIInteract(R_ServerPanelSrc.S.gameObject, false);
#endif
            }
            else
            {
#if !UNITY_WEBGL
                GuiMain.S.SetAllUIInteract(R_ServerPanelSrc.S.gameObject, true);
#endif
                GuiMain.S.SetAllUIInteract(B_ToolbarPanelSrc.S.gameObject, true);
                B_ToolbarPanelSrc.S.PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure(MainScr.S.CurrentArc().currentStr));
                GuiMain.S.FocusCameraCenter();
                GuiMain.S.DisSelectPickGO();
            }

        });
        GuiMain.S.editEnable = false;
        this.gameObject.SetActive(false);
    }
    // Start is called before the first frame update

    public void EditPanelAddAtom()
    {
        Button btn = paraDict["add atom"].GetComponent<Button>();
        GuiMain.S.SetUIInteract(btn, false);

        M_AddAtomWidgetSrc.S.gameObject.SetActive(true);
        M_AddAtomWidgetSrc.S.closeBtn.onClick.AddListener(() =>
        {
            GuiMain.S.SetUIInteract(btn, true);
            M_AddAtomWidgetSrc.S.closeBtn.onClick.RemoveAllListeners();
            M_AddAtomWidgetSrc.S.gameObject.SetActive(false);
        });
    }
    public void EditPanelEditLattice()
    {
        Button btn = paraDict["edit lattice"].GetComponent<Button>();
        GuiMain.S.SetUIInteract(btn, false);

        M_EditLatticeWidgetSrc.S.gameObject.SetActive(true);
        M_EditLatticeWidgetSrc.S.SetLatPara(MainScr.S.CurrentStructure().abc);
        M_EditLatticeWidgetSrc.S.closeBtn.onClick.AddListener(() =>
        {
            GuiMain.S.SetUIInteract(btn, true);
            M_EditLatticeWidgetSrc.S.closeBtn.onClick.RemoveAllListeners();
            M_EditLatticeWidgetSrc.S.gameObject.SetActive(false);
        });
    }
}
