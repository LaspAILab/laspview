using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
public class M_SupercellWidgetSrc : MonoBehaviour
{
    public static M_SupercellWidgetSrc S;

    static public Dictionary<string, GameObject> paraDict;
    public Button closeBtn;


    // Start is called before the first frame update
    void Awake()
    {
        S = this;

        syfPanelPara p = this.GetComponent<syfPanelPara>();
        p.SetParaDict();
        paraDict = p.paraDict;

        closeBtn = this.transform.GetChild(0).GetComponentInChildren<Button>();

        //Set Edit Lattice Widget
        this.transform.SetParent(GameObject.Find("PhysicalResizer").transform);
        this.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        this.gameObject.SetActive(false);
    }


    public void Supercell()
    {
        int a = (int)paraDict["a"].GetComponent<Slider>().value;
        int b = (int)paraDict["b"].GetComponent<Slider>().value;
        int c = (int)paraDict["c"].GetComponent<Slider>().value;

        if (a == 1 && b == 1 && c == 1) return;

        Structure struc = MainScr.S.CurrentStructure();
        ArcStructures newArc = Instantiate(MainScr.S.ArcStructuresPrefabVar).GetComponent<ArcStructures>();

        Structure newStruc = newArc.AddSturcture();

        float[] newAbc = new float[6] { struc.abc[0], struc.abc[1], struc.abc[2], struc.abc[3], struc.abc[4], struc.abc[5] };
        newAbc[0] *= a;
        newAbc[1] *= b;
        newAbc[2] *= c;
        newStruc.abc = newAbc;

        for (int i = 0; i < a; i++)
            for (int j = 0; j < b; j++)
                for (int k = 0; k < c; k++)
                {
                    Vector3 bias = struc.GetCellTransfer(i, j, k);
                    foreach (AtomScript at in struc.atomList)
                    {
                        newStruc.AddAtom(at.ele, at.cdnt+bias);
                    }

                }
        MainScr.S.arcList.Add(newArc);
        //Regex.Replace(struc.parent.filename, ".arc", "-" + (struc.parent.currentStr + 1) + '-' + a + '*' + b + '*' + c + ".arc");
        string filename = Regex.Replace(struc.parent.filename, @"\.arc", "-" + (struc.parent.currentStr + 1) + '-' + a + '*' + b + '*' + c + ".arc", RegexOptions.RightToLeft);
        if (filename == "") filename = (struc.parent.currentStr + 1).ToString() + '-' + a + '*' + b + '*' + c + ".arc";
        L_ProjectPanelSrc.S.ProjectListAddCell(filename);
        GuiMain.S.SetAllUIInteract(L_ProjectPanelSrc.S.gameObject, false);
        MainScr.S.DrawArc();

        paraDict["a"].GetComponent<Slider>().value = 1;
        paraDict["b"].GetComponent<Slider>().value = 1;
        paraDict["c"].GetComponent<Slider>().value = 1;

    }

}
