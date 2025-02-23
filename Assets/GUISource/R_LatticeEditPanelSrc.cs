using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class R_LatticeEditPanelSrc : MonoBehaviour
{
    static public R_LatticeEditPanelSrc S;

    void Awake()
    {
        S = this;

        //Set Edit panel
        this.transform.SetParent(GuiMain.S.rightCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button edButton = GameObject.Find("EditBtn").GetComponent<Button>();
        edButton.onClick.AddListener(() =>
        {
            GuiMain.S.BtnHideObject(edButton, this.gameObject);
        });
        gameObject.SetActive(false);
    }
    
    
}
