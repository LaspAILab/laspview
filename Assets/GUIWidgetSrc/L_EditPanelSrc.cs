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

}
