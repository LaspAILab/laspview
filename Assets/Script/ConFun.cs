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

// Unity cannot serialize raw json arrays, so we wrap it with a class as a workaround
[Serializable]
public struct ElementContainer
{
    public SerializableElement[] Elements;

    [Serializable]
    public struct SerializableElement
    {
        public int Number;
        public string Symbol;
        public float Radius;
        public int[] Color;
    }
}

public struct ElementData
{
    public int Number;
    public float Radius;
    public Color Color;

    public ElementData(ElementContainer.SerializableElement serializableElement)
    {
        Number = serializableElement.Number;
        Radius = serializableElement.Radius;
        Color = new Color(serializableElement.Color[0] / 255.0f, serializableElement.Color[1] / 255.0f,
            serializableElement.Color[2] / 255.0f, serializableElement.Color[3] / 255.0f);
    }
}

public class ConFun : MonoBehaviour
{
    public static Dictionary<string, ElementData> elements;
    public static Dictionary<string, string> GlobalConfig;
    public List<string> NecessaryConfigKeys;
    public static readonly Vector3[] LatticeCorners = { new(0,0,0), new(1,1,0), new(1,0,1), new(0,1,1),
                                        new(1,1,1), new(0,0,1), new(0,1,0), new(1,0,0) };
    public float bondBoundry;

    string[] noteInfoText = new string[]
    {
        "Drag Right Mouse: rotate camera",
        "Drag Middle Mouse: translate camera",
        "Press Left Mouse: select atoms",
        "LCtrl + Drag Right Mouse: rotate selected atoms",
        "LCtrl + Drag Middle Mouse: translate selected atoms",
        "LCtrl + C: copy selected atoms",
        "LCtrl + V: paste copied atoms",
        "LCtrl + Z: Undo last modification of the structure",

        "Middle Mouse can be replaced by LShift",

        "CHANGE \"user\" in LaspViewConsole.log FIRST!",

        "Click [Server]-[Start Listening] to prepare for receive file",
        "Shell: \"Easy.py xx.arc\" to display .arc here",
        "Shell: \"Easy.py -cl\" to copy latest sent file",
        "Shell: \"Easy.py -ls\" to list dir of your temp dir",
        "Shell: \"Easy.py -clear\" to rm all file in your temp dir",

    };

    public static GameObject structureText;
    public static Text statusText;

    void Awake()
    {
        structureText = GameObject.Find("StructureText");
        statusText = GameObject.Find("StatusText").GetComponent<Text>();
#if !UNITY_WEBGL
        NecessaryConfigKeys = new List<string> { "user", "remote_ip", "remote_port", "local_port" };
#else
        necessaryGlobalPara = new List<string>();
#endif
        ReadPeriodicTable();
        ReadGlobalConfig();
        GlobalConfig.Add("model_type", "scale");
        bondBoundry = 0.63f;
    }

    public static string[] PySplit(string line)
    {
        return Regex.Split(line, "\\s+", RegexOptions.IgnoreCase);
    }

    private void ReadPeriodicTable()
    {
        var periodicTableJson = Resources.Load<TextAsset>("Elements");
        var rawElements = JsonUtility.FromJson<ElementContainer>(periodicTableJson.text).Elements;
        elements = rawElements.ToDictionary(e => e.Symbol, e => new ElementData(e));
    }

    public void ReadGlobalConfig()
    {
        GlobalConfig = new Dictionary<string, string>();
#if !UNITY_WEBGL
        // printStatusInfo(Application.dataPath);
        string[] lines = File.ReadAllLines(Application.dataPath + "/LaspViewConsole.log");
#else
        string[] lines = { };
        //string[] lines = { " arc_default = lowestpath - 1.arc "};
#endif
        foreach (string lin in lines)
        {
            if (lin.Trim() == "") continue;
            string[] kv = lin.Split('=');
            GlobalConfig.Add(kv[0], kv[1]);
        }
        foreach (string para in NecessaryConfigKeys)
        {
            if (!GlobalConfig.ContainsKey(para))
            {
                printStatusInfo("NOT Cantain KEY " + para + " ,plz check allkey.log and RESTART the program");
            }
        }
    }

    public void OnStructureIndexChange()
    {
        int index = int.Parse(structureText.transform.GetChild(1).gameObject.GetComponent<InputField>().text) - 1;
        index = Mathf.Min(MainScr.S.CurrentArc().structureList.Count - 1, index);
        B_ToolbarPanelSrc.S.PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure(index));
    }

    public static void printStructureInfo(int currentStruc, int totalStrucs, float energy)
    {
        //print(structureText);
        structureText.transform.GetChild(1).gameObject.GetComponent<InputField>().SetTextWithoutNotify((currentStruc + 1).ToString());
        structureText.transform.GetChild(3).GetComponent<Text>().text = totalStrucs.ToString();
        structureText.transform.GetChild(5).GetComponent<Text>().text = energy.ToString("f3");
    }

    public static void printStatusInfo(string str)
    {
        statusText.text = str;
#if UNITY_EDITOR
        Debug.Log(str);
#endif
    }

    public static void printNoteInfo()
    {
        //string str = noteInfoText[Random.Range(0, noteInfoText.Length)];
        //statusText.text = str;
    }

    public static void printErrorInfo()
    {
        //print to File
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
