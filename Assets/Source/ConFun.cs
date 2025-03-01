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

    public static void UpdateStructureInfo(int currentStruct, int totalStructs, float energy)
    {
        //print(structureText);
        structureText.transform.GetChild(1).gameObject.GetComponent<InputField>().SetTextWithoutNotify((currentStruct + 1).ToString());
        structureText.transform.GetChild(3).GetComponent<Text>().text = totalStructs.ToString();
        structureText.transform.GetChild(5).GetComponent<Text>().text = energy.ToString("f3");
    }

    public static void UpdateStatusLabel(string str)
    {
        statusLabel.text = str;
        // Debug.Log(str);
    }

    //public void UnityToWeb(string str)
    //{
    //    //参数一：Web里面接收数据的方法，参数二：Unity传递的数据（我这里是输入就传递）
    //    Application.ExternalCall("GetFromUnity", str);
    //}

    public string WebToUnity(string s)
    {
        return s;
    }
}
