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
        Camera.main.GetComponent<ViewportController>().RefocusCamera();
    }

    public void OnPlayerHeadBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        MainScr.S.SwitchArc(0);
    }

    public void OnPlayerTailBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        MainScr.S.SwitchArc(MainScr.S.currentFile.Structures.Count - 1);
    }

    public void OnPlayerForwardBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        MainScr.S.SwitchArc(MainScr.S.currentArcIndex + 1);
    }

    public void OnPlayerBackBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        MainScr.S.SwitchArc(MainScr.S.currentArcIndex - 1);
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
        //GuiMain.S.SetAllUIInteract(R_LASPPanelSrc.S.gameObject, !onPlay);
#endif
        if (onPlay)
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
            R_ServerPanelSrc.S.NetLocalBtnChange();
#endif
            // this.PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure(MainScr.S.CurrentArc().currentStr));
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
                if (MainScr.S.currentArcIndex == MainScr.S.currentFile.Structures.Count - 1)
                {
                    MainScr.S.SwitchArc(0);
                }
                else
                {
                    MainScr.S.SwitchArc(MainScr.S.currentArcIndex + 1);
                }

                timer = 0;
            }
        }
    }

    public void PlayerBtnChecked((bool lower, bool upper) playStatus)
    {
        //check if the player Button should be de-activate or re-activate
        if (playStatus.lower)
        {
            btnHead.interactable = true;
            btnFoward.interactable = true;
        }
        else
        {
            btnHead.interactable = false;
            btnFoward.interactable = false;
        }

        if (playStatus.upper)
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