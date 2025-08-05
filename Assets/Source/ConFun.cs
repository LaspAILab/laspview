//basic constant & function, Singleton Pattern
//including:    1. Periodic table, mess, and radius 周期表内容
//              2. String processing C#的字符串不好使，写了些函数
//              3. rgb Color: Int to Float 颜色格式转换

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine.UI;
using System;
using System.Linq;

public class ConFun : MonoBehaviour
{
    public static Dictionary<string, string> GlobalConfig;

    public static GameObject structureText;
    static Text statusLabel;

    void Awake()
    {
        structureText = GameObject.Find("StructureText");
        statusLabel = GameObject.Find("StatusText").GetComponent<Text>();

        ReadGlobalConfig();
        GlobalConfig.Add("model_type", "scale");
    }

    public void ReadGlobalConfig()
    {
        GlobalConfig = new Dictionary<string, string>();
#if !UNITY_WEBGL
        // printStatusInfo(Application.dataPath);
        string[] lines = File.ReadAllLines(Application.dataPath + "/LaspViewConsole.cfg");
#else
        string[] lines = { };
        //string[] lines = { " arc_default = lowestpath - 1.arc "};
#endif
        foreach (string lin in lines)
        {
            if (lin.Trim() == "")
            {
                continue;
            }

            string[] kv = lin.Split('=');
            GlobalConfig.Add(kv[0], kv[1]);
        }
    }

    public static void UpdateStatusLabel(string str, bool addLog = true)
    {
        statusLabel.text = str;
        if (addLog)
        {
            Debug.Log(str);
        }
    }

    public void OnStructureIndexSubmit()
    {
        var indexInput = structureText.transform.GetChild(1).GetComponent<InputField>();
        if (int.TryParse(indexInput.text, out int index) && App.Instance != null)
        {
            StructureSequenceController.Instance.SwitchStructure(index - 1);
            indexInput.SetTextWithoutNotify((StructureSequenceController.Instance.CurrentStructureIndex + 1).ToString());
        }
    }
}