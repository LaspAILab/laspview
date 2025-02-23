using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class L_ProjectPanelSrc : MonoBehaviour
{
    static public L_ProjectPanelSrc S;

    public GameObject listCellPrefab;
    static public GameObject fileList;
    void Awake()
    {
        S = this;

        //Set ProjectPanel
        this.transform.SetParent(GuiMain.S.leftCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button pjButton = GameObject.Find("ProjectBtn").GetComponent<Button>();
        pjButton.onClick.AddListener(() =>
        {
           GuiMain.S.BtnHideObject(pjButton, this.gameObject);
        });

        fileList = this.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
        this.gameObject.SetActive(false);
    }

    public void ProjectListAddCell(string filename)
    {
        GameObject newCell = Instantiate(listCellPrefab);
        newCell.transform.SetParent(fileList.transform);
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
        int index = obj.transform.GetSiblingIndex();
        // MainScr.S.DrawArc(index);

    }



}
