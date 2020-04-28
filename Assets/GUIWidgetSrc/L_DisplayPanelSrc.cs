using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class L_DisplayPanelSrc : MonoBehaviour
{
    static public L_DisplayPanelSrc S;
    void Awake()
    {
        S = this;

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
}
