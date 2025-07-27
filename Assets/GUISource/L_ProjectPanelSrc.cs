using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class L_ProjectPanelSrc : MonoBehaviour
{
    static public L_ProjectPanelSrc S;

    public GameObject listCellPrefab;
    static public GameObject fileList;

    void Awake()
    {
        S = this;

        //Set ProjectPanel
        this.transform.SetParent(GuiMain.S.leftCanvas.transform, false);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button pjButton = GameObject.Find("ProjectBtn").GetComponent<Button>();
        pjButton.onClick.AddListener(() =>
        {
            if (!gameObject.activeSelf)
            {
                GuiMain.S.ClearLeftPanel();
            }
            
            GuiMain.S.BtnHideObject(pjButton, this.gameObject);
        });

        fileList = this.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
        this.gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        // Update file list
        for (int i = 0; i < fileList.transform.childCount; i++)
        {
            Destroy(fileList.transform.GetChild(i).gameObject);
        }
        
        foreach (var kv in App.Instance.OpenFileList)
        {
            ProjectListAddCell(kv.Key);
        }
    }

    public void ProjectListAddCell(string filename)
    {
        GameObject newCell = Instantiate(listCellPrefab);
        newCell.transform.SetParent(fileList.transform, false);
        newCell.transform.localScale = Vector3.one;
        newCell.GetComponentInChildren<Text>().text = filename;
        newCell.GetComponent<Button>().onClick.AddListener(() => ProjectListChangeArc(newCell));
    }

    public void ProjectListPopCell(int i)
    {
        GameObject popCell = fileList.transform.GetChild(i).gameObject;
        Object.Destroy(popCell);
    }

    public void ProjectListChangeArc(GameObject obj)
    {
        //called by 
        string filename = obj.GetComponentInChildren<Text>().text;

        App.Instance.SwitchArcFile(filename);
    }
}