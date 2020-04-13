using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.UI;
using System.IO;
using System.Text.RegularExpressions;
using Lasp4Win;
public class R_LASPPanelSrc : MonoBehaviour
{
    static public R_LASPPanelSrc S;

    static public syfPanelPara laspPanelPara;
    GameObject laspInEditorpanel;
    InputField laspInEditorText;

    string workDirName;
    string prefabDirName;
    GameObject laspParaSet;
    bool laspParaSetActive;
    string laspInText;

    void Awake()
    {
        S = this;
        laspPanelPara = this.GetComponent<syfPanelPara>();
        laspPanelPara.SetParaDict();
        laspParaSetActive = false;
        laspParaSet = this.transform.GetChild(this.transform.childCount - 1).gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LaspSetPanel()
    {
        laspParaSet.SetActive(laspParaSetActive);
        //Set Lasp Panel, Mainly Selectable Item in Prefav dir
        Dropdown prefabDirDp = laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>();
        //print(laspPanelPara.paraDict["Prefab dir"].transform);
        List<string> prefabNames = new List<string>(Directory.GetDirectories("./LaspPrefab"));
        List<Dropdown.OptionData> prefabDrop = new List<Dropdown.OptionData>();
        foreach (string str in prefabNames)
        {
            string[] spl = str.Split('\\');
            prefabDrop.Add(new Dropdown.OptionData(spl[spl.Length - 1]));
        }
        prefabDirDp.options = prefabDrop;
        //this.LaspChangePrefab();
    }

    public void LaspCreateNewTask()
    {
        laspParaSetActive = true;
        laspParaSet.SetActive(laspParaSetActive);

        prefabDirName = laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().captionText.text;
        laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
        laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().interactable = false;

        //Open target lasp.in
        string[] lines = File.ReadAllLines("./LaspPrefab/" + prefabDirName + "/lasp.in");
        //Read lasp.in
        Dictionary<string, int> uiPara = new Dictionary<string, int>
                { { "potential", 0 } ,{ "explore_type",0 },{ "%block netinfo",0 },{ "run_type",0 }, {"ssw.sswsteps",0 } };
        int i = 0;
        StringBuilder otherPara = new StringBuilder { };

        for (i = 0; i < lines.Length; i++)
        {
            string[] para = ConFun.S.PySplit(lines[i].ToLower());
            if (uiPara.ContainsKey(para[0]) || (para[0] == "%block" && para[1] == "netinfo"))
            {
                if (para[0] == "potential")
                {
                    Dropdown dp = laspPanelPara.paraDict["potential"].GetComponent<Dropdown>();
                    if (para[1] == "nn")
                    {
                    }
                    else
                    {
                        ConFun.S.printStatusInfo("Only NN is allowed for potential");
                    }
                }
                else if (para[0] == "explore_type")
                {
                    Dropdown dp = laspPanelPara.paraDict["explore_type"].GetComponent<Dropdown>();
                    if (para[1] == "ssw")
                    {
                    }
                    else
                    {
                        ConFun.S.printStatusInfo("Only SSW is allowed for explore_type");
                    }
                }
                else if (para[0] == "run_type")
                {
                    Dropdown dp = laspPanelPara.paraDict["run_type"].GetComponent<Dropdown>();
                    Dictionary<string, int> available = new Dictionary<string, int> { { "5", 0 }, { "15", 1 }, { "2", 2 }, { "12", 3 }, { "1", 4 }, { "11", 5 } };
                    if (available.ContainsKey(para[1]))
                    {
                        dp.value = available[para[1]];
                    }
                    else
                    {
                        ConFun.S.printStatusInfo("Only 5, 15, 2, 12, 1, 11 is allowed for run_type");
                    }
                }
                else if (para[0] == "%block" && para[1] == "netinfo")
                {
                    LaspSetNNPot();
                    Dropdown dp = laspPanelPara.paraDict["nn pot"].GetComponent<Dropdown>();
                    i++;

                    para = ConFun.S.PySplit(lines[i].ToLower());
                    int j = 0;
                    for (; j < dp.options.Count; j++)
                    {
                        if (dp.options[j].text.ToLower() == para[2])
                        {
                            dp.value = j;
                            break;
                        }
                    }
                    if (j == dp.options.Count)
                    {
                        ConFun.S.printStatusInfo("NN pot not available in ./LaspNNPot");
                    }
                    while (!Regex.IsMatch(lines[i], @"%endblock netinfo") && i < lines.Length)
                    {
                        i++;
                    }
                }
                else if (para[0] == "ssw.sswsteps")
                {
                    InputField fld = laspPanelPara.paraDict["ssw.sswsteps"].GetComponent<InputField>();
                    fld.text = para[1];
                }
            }
            else
            {
                if (!Regex.IsMatch(lines[i], @"^\#\#\#(.)*\#\#\#$"))
                {
                    otherPara.AppendLine(lines[i]);
                }
            }
        }

        laspInText = LaspWriteLaspin();

    }

    public void LaspResetTask()
    {
        laspParaSetActive = false;
        laspParaSet.SetActive(laspParaSetActive);

        laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
        laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().interactable = true;
    }

    public void LaspSetNNPot()
    {
        //Set Lasp Panel, Mainly Selectable Item in Prefav dir
        Dropdown prefabDirDp = laspPanelPara.paraDict["nn pot"].transform.GetComponentInParent<Dropdown>();
        print(prefabDirDp);
        //print(laspPanelPara.paraDict["NN Pot"]);
        List<string> prefabNames = new List<string>(Directory.GetFiles("./LaspNNPot"));
        List<Dropdown.OptionData> prefabDrop = new List<Dropdown.OptionData>();
        foreach (string str in prefabNames)
        {
            string[] spl = str.Split('\\');
            prefabDrop.Add(new Dropdown.OptionData(spl[spl.Length - 1]));
        }
        prefabDirDp.options = prefabDrop;
    }

    public string LaspWriteLaspin(StringBuilder otherPara = null)
    {

        if (otherPara == null)
        {
            string dirName = laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().captionText.text;
            string[] lines = File.ReadAllLines("./LaspPrefab/" + dirName + "/lasp.in");
            otherPara = new StringBuilder { };
            //Read lasp.in
            Dictionary<string, int> uiPara = new Dictionary<string, int>
                { { "potential", 0 } ,{ "explore_type",0 },{ "%block netinfo",0 },{ "run_type",0 }, {"ssw.sswsteps",0 } };
            int i = 0;
            for (i = 0; i < lines.Length; i++)
            {
                string[] para = ConFun.S.PySplit(lines[i].ToLower());
                para[0] = para[0];
                if (uiPara.ContainsKey(para[0]) || (para[0] == "%block" && para[1] == "netinfo"))
                {
                    if (para[0] == "%block" && para[1] == "netinfo")
                    {
                        while (!Regex.IsMatch(lines[i], @"%endblock netinfo") && i < lines.Length)
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    if (!Regex.IsMatch(lines[i], @"^\#(.)*\#$"))
                    {
                        otherPara.AppendLine(lines[i]);
                        //print(lines[i]);
                    }
                }
            }
        }

        StringBuilder outputlines = new StringBuilder { };
        outputlines.AppendLine("######AutoGernerate######");
        foreach (string key in laspPanelPara.paraDict.Keys)
        {
            if (key == "prefab directory" || key == "running directory") continue;
            else if (key == "nn pot")
            {
                outputlines.AppendLine("%block netinfo");
                string name = laspPanelPara.paraDict[key].GetComponentInChildren<Dropdown>().captionText.text;
                foreach (Match ele in Regex.Matches(name.Replace(".pot", ""), @"([A-Z][a-z]?)"))
                {
                    outputlines.Append("    " + ele + "   " + name + "\n");
                }
                outputlines.AppendLine("%endblock netinfo");
            }
            else if (laspPanelPara.paraDict[key].GetComponentInChildren<Dropdown>() != null)
            {
                outputlines.Append(key + "    " + laspPanelPara.paraDict[key].GetComponentInChildren<Dropdown>().captionText.text + "\n");
            }
            else if (laspPanelPara.paraDict[key].GetComponentInChildren<InputField>() != null)
            {
                outputlines.Append(key + "    " + laspPanelPara.paraDict[key].GetComponentInChildren<InputField>().text + "\n");
                //print(outputlines.Append(key + "    " + laspPanelPara.paraDict[key].GetComponentInChildren<InputField>().text));
            }
        }
        outputlines.Append("######EndGernerate######\n");

        outputlines.Append(otherPara);

        return outputlines.ToString(); 
        //if (filename == null)
        //{
        //    filename = "./LaspPrefab/" + laspPanelPara.paraDict["prefab directory"].GetComponentInChildren<InputField>().text + "/lasp.in";
        //}
        //File.WriteAllText(filename, outputlines.ToString());
    }

    public void LaspEditLaspIn()
    {
        //Button btn = this.transform.GetChild(2).GetChild(4).gameObject.GetComponent<Button>();
        //laspInEditorpanel = Instantiate(GuiMain.S.M_LaspInWidgetPrefab);
        //laspInEditorpanel.transform.SetParent(GameObject.Find("Canvas").transform);
        //laspInEditorpanel.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);

        //laspInEditorText = laspInEditorpanel.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<InputField>();
        //string dirName = laspPanelPara.paraDict["prefab directory"].GetComponentInParent<Dropdown>().captionText.text;

        //LaspWriteLaspin("./LaspPrefab/" + dirName + "/lasp.in");
        //string lines = File.ReadAllText("./LaspPrefab/" + dirName + "/lasp.in");


        //btn.image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
        //laspInEditorText.text = lines;
        //laspInEditorpanel.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "./" + dirName + "/lasp.in";
        //laspInEditorpanel.transform.GetChild(0).GetChild(1).GetComponent<Button>().onClick.AddListener(() =>
        //{
        //    File.WriteAllText("./LaspPrefab/" + dirName + "/lasp.in", laspInEditorText.text);
        //});
        //laspInEditorpanel.transform.GetChild(0).GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
        //{
        //    btn.interactable = true;
        //    btn.image.color = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));
        //    Object.Destroy(laspInEditorpanel);
        //});
    }

    public void LaspRun()
    {
        //creak work directory
        if (false == System.IO.Directory.Exists("./LaspRun"))
        {
            Directory.CreateDirectory("./LaspRun");
        }

        //creat LASP Work Directory
        workDirName = string.Format("{0:d5}-{1:d}", Random.Range(10000, 99999),
                System.DateTime.Now.ToString("MMddhhmmss")) + "/";
        string wholeWorkDir = "./LaspRun/" + workDirName;
        Directory.CreateDirectory(wholeWorkDir);

        //write lasp.in
        File.WriteAllText(wholeWorkDir+"/lasp.in", laspInText);

        //Copy prefab except
        string[] fileList = Directory.GetFiles("./LaspPrefab/" + prefabDirName, "*");
        foreach (string f in fileList)
        {
            // Remove path from the file name.
            string fName = f.Substring(("./LaspPrefab/" + prefabDirName).Length + 1);
            if (fName != ("lasp.in")) 
                File.Copy(Path.Combine(prefabDirName, fName), Path.Combine(wholeWorkDir, fName), true);
        }


        //cp NN Pot
        string pot = laspPanelPara.paraDict["nn pot"].GetComponent<Dropdown>().captionText.text;
        //string[] lines = File.ReadAllLines(prefabDir + "/lasp.in");
        //for (int i = 0; i < lines.Length; i++)
        //{
        //    string[] para = ConFun.S.PySplit(lines[i]);
        //    if (para[0] == "%block" && para[1] == "netinfo")
        //    {
        //        i++;
        //        pot = ConFun.S.PySplit(lines[i])[2];
        //        break;
        //    }
        //}
        File.Copy(Path.Combine("./LaspNNPot", pot), Path.Combine(wholeWorkDir, pot), true);

        //Write current arc
        string arc = MainScr.S.CurrentArc().ArcWrite();
        File.WriteAllText(Path.Combine(wholeWorkDir, "input.arc"), arc);

        //run lasp
        LaspWinProgress pro = new LaspWinProgress();
        pro.ExecuteProgram(args: wholeWorkDir + "> output.txt 2>&1");

    }
}
