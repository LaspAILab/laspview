using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class B_ToolbarPanelSrc : MonoBehaviour
{
    // Start is called before the first frame update
    static public B_ToolbarPanelSrc S;

    public Sprite pauseTexture;
    public Sprite playTexture;

    public Button btnHead;
    public Button btnFoward;
    public Button btnFocus;
    public Button btnPlay;
    public Button btnNext;
    public Button btnTail;

    Coroutine playCoru;
    public bool onPlay;
    float timer = 0;


    void Awake()
    {
        S = this;

        //Set toolbar 
        this.transform.SetParent(GuiMain.S.statusPanel.transform, false);
        this.transform.localScale = Vector3.one;
        this.transform.SetSiblingIndex(1);

        onPlay = false;
        btnHead = this.transform.GetChild(0).gameObject.GetComponent<Button>();
        btnFoward = this.transform.GetChild(1).gameObject.GetComponent<Button>();
        btnFocus = this.transform.GetChild(2).gameObject.GetComponent<Button>();
        btnPlay = this.transform.GetChild(3).gameObject.GetComponent<Button>();
        btnNext = this.transform.GetChild(4).gameObject.GetComponent<Button>();
        btnTail = this.transform.GetChild(5).gameObject.GetComponent<Button>();
    }
}