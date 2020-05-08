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
        this.transform.SetParent(GuiMain.S.statusPanel.transform);
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

    public void OnPlayerPlayBtnClicked()
    {
        if (onPlay == false)
        {
            onPlay = true;
            btnPlay.gameObject.GetComponentsInChildren<Image>()[1].sprite = pauseTexture;
            playCoru = StartCoroutine(PlayStructure());
        }
        else
        {
            onPlay = false;
            btnPlay.gameObject.GetComponentsInChildren<Image>()[1].sprite = playTexture;
            StopCoroutine(playCoru);
        }

        GuiMain.S.SetAllUIInteract(L_ProjectPanelSrc.S.gameObject, !onPlay);
        GuiMain.S.SetAllUIInteract(L_FilePanelSrc.S.gameObject, !onPlay);
        GuiMain.S.SetAllUIInteract(L_EditPanelSrc.S.gameObject, !onPlay);
#if !UNITY_WEBGL
        GuiMain.S.SetAllUIInteract(R_LASPPanelSrc.S.gameObject, !onPlay);
#endif
        if (onPlay == true)
        {
            this.PlayerBtnChecked((false, false));
#if !UNITY_WEBGL
            //foreach (Selectable c in R_ServerPanelSrc.S.gameObject.GetComponentsInChildren<Selectable>())
            //{
            //    if (c.gameObject.name == "StartListeningBtn")
            //    {

            //        MainScr.S.localServer.Quit2();
            //        R_ServerPanelSrc.S.lclSrvrListening = false;
            //    }
            //}
            //R_ServerPanelSrc.S.NetLocalBtnChange();
            GuiMain.S.SetAllUIInteract(R_ServerPanelSrc.S.gameObject, false);
#endif
        }
        else
        {
#if !UNITY_WEBGL
            GuiMain.S.SetAllUIInteract(R_ServerPanelSrc.S.gameObject, true);
            R_ServerPanelSrc.S.transform.GetChild(2).GetChild(1).GetComponent<Button>().interactable = false;
#endif
            this.PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure(MainScr.S.CurrentArc().currentStr));
        }

    }

    IEnumerator PlayStructure()
    {
        while (true)
        {
            timer += Time.deltaTime;
            yield return null;
            if (timer >= L_DisplayPanelSrc.displayPanelPara.paraDict["play interval"].GetComponent<Slider>().value)
            {
                //print("HelloWorld")/*;*/
                if(MainScr.S.CurrentArc().currentStr == MainScr.S.CurrentArc().structureList.Count - 1)
                {
                    MainScr.S.CurrentArc().DrawStructure(0);
                }
                else
                {
                    MainScr.S.CurrentArc().DrawStructure('+');
                }
                timer = 0;
            }
        }

    }

    public void PlayerBtnChecked((bool lower, bool upper) playStatus)
    {
        //check if the player Button should be de-activate or re-activate
        if (playStatus.lower == true)
        {
            btnHead.interactable = true;
            btnFoward.interactable = true;
        }
        else
        {
            btnHead.interactable = false;
            btnFoward.interactable = false;
        }
        if (playStatus.upper == true)
        {
            //print(toolbarPaenl.transform.GetChild(1));
            btnTail.interactable = true;
            btnNext.interactable = true;
        }
        else
        {
            btnTail.interactable = false;
            btnNext.interactable = false;
        }
    }
}
