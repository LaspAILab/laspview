using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class B_ToolbarPanelSrc : MonoBehaviour
{
    // Start is called before the first frame update
    static public B_ToolbarPanelSrc S;
    void Awake()
    {
        S = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void OnPlayerFocusBtnClicked()
    {
        GuiMain.S.FocusCameraCenter();
    }

    public void OnPlayerHeadBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure(0));
    }

    public void OnPlayeTailBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        ArcStructures arc = MainScr.S.CurrentArc();
        PlayerBtnChecked(arc.DrawStructure(arc.lenStructures - 1));
    }

    public void OnPlayeForwardBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure('+'));
    }

    public void OnPlayeBackBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure('-'));
    }

    public void PlayerBtnChecked((bool lower, bool upper) playStatus)
    {
        //check if the player Button should be de-activate or re-activate
        if (playStatus.lower == true)
        {
            this.transform.GetChild(0).gameObject.GetComponent<Button>().interactable = true;
            this.transform.GetChild(1).gameObject.GetComponent<Button>().interactable = true;
        }
        else
        {
            this.transform.GetChild(0).gameObject.GetComponent<Button>().interactable = false;
            this.transform.GetChild(1).gameObject.GetComponent<Button>().interactable = false;
        }
        if (playStatus.upper == true)
        {
            //print(toolbarPaenl.transform.GetChild(1));
            this.transform.GetChild(3).gameObject.GetComponent<Button>().interactable = true;
            this.transform.GetChild(4).gameObject.GetComponent<Button>().interactable = true;
        }
        else
        {
            this.transform.GetChild(3).gameObject.GetComponent<Button>().interactable = false;
            this.transform.GetChild(4).gameObject.GetComponent<Button>().interactable = false;
        }
    }
}
