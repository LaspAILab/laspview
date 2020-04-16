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

public class ConFun : MonoBehaviour
{
    //All constant and basic function used, including  PeriodicTable and Matrix operator
    static public ConFun S;
    public Dictionary<string, float> eleRadius;
    public Dictionary<string, int> eleTable;
    public Dictionary<string, Color> eleColor;
    public Dictionary<string, string> globalPara;
    public List<string> necessaryGlobalPara;
    public Vector3[] latCorner = { new Vector3 (0, 0, 0),new Vector3(1,1,0),new Vector3(1,0,1),new Vector3(0,1,1),
                                        new Vector3(1,1,1),new Vector3(0,0,1),new Vector3(0,1,0),new Vector3(1,0,0) };
    public float bondBoundry;

    public Text structureText;
    public Text statusText;

    void Awake()
    {   
        S = this;
        necessaryGlobalPara = new List<string>{ "user", "remote_ip", "remote_port","local_port" };
        this.SetPeriodicTable();
        this.readGlobalPara();
        globalPara.Add("model_type", "scale");
        bondBoundry = 0.63f;

        structureText = GameObject.Find("StructureText").GetComponent<Text>(); 
        statusText = GameObject.Find("StatusText").GetComponent<Text>();
    }

    public string[] PySplit(string line)
    {
        return Regex.Split(line, "\\s+", RegexOptions.IgnoreCase);
    }

    private void SetPeriodicTable()
    {
        eleRadius = new Dictionary<string, float>()
        {
            { "H", 0.31f }, { "He", 0.28f },
            { "Li", 1.28f }, { "Be", 0.96f }, { "B", 0.85f }, { "C", 0.76f }, { "N", 0.71f },
            { "O", 0.66f }, { "F", 0.57f }, { "Ne", 0.58f }, { "Na", 1.66f }, { "Mg", 1.41f },
            { "Al", 1.21f }, { "Si", 1.11f }, { "P", 1.07f }, { "S", 1.05f }, { "Cl", 1.02f },
            { "Ar", 1.06f }, { "K", 2.03f }, { "Ca", 1.76f }, { "Sc", 1.70f }, { "Ti", 1.60f },
            { "V", 1.53f }, { "Cr", 1.39f }, { "Mn", 1.39f }, { "Fe", 1.32f }, { "Co", 1.26f },
            { "Ni", 1.24f }, { "Cu", 1.32f }, { "Zn", 1.22f }, { "Ga", 1.22f }, { "Ge", 1.20f },
            { "As", 1.19f }, { "Se", 1.20f }, { "Br", 1.20f }, { "Kr", 1.16f }, { "Rb", 2.20f },
            { "Sr", 1.95f }, { "Y", 1.90f }, { "Zr", 1.75f }, { "Nb", 1.64f }, { "Mo", 1.54f },
            { "Tc", 1.47f }, { "Ru", 1.46f }, { "Rh", 1.42f }, { "Pd", 1.39f }, { "Ag", 1.45f },
            { "Cd", 1.44f }, { "In", 1.42f }, { "Sn", 1.39f }, { "Sb", 1.39f }, { "Te", 1.38f },
            { "I", 1.39f }, { "Xe", 1.40f }, { "Cs", 2.44f }, { "Ba", 2.15f }, { "La", 2.07f },
            { "Ce", 2.04f }, { "Pr", 2.03f }, { "Nd", 2.01f }, { "Pm", 1.99f }, { "Sm", 1.98f },
            { "Eu", 1.98f }, { "Gd", 1.96f }, { "Tb", 1.94f }, { "Dy", 1.92f }, { "Ho", 1.92f },
            { "Er", 1.89f }, { "Tm", 1.90f }, { "Yb", 1.87f }, { "Lu", 1.87f }, { "Hf", 1.75f },
            { "Ta", 1.70f }, { "W", 1.62f }, { "Re", 1.51f }, { "Os", 1.44f }, { "Ir", 1.41f },
            { "Pt", 1.36f }, { "Au", 1.36f }, { "Hg", 1.32f }, { "Tl", 1.45f }, { "Pb", 1.46f },
            { "Bi", 1.48f }, { "Po", 1.40f }, { "At", 1.50f }, { "Rn", 1.50f }, { "Fr", 2.60f },
            { "Ra", 2.21f }, { "Ac", 2.15f }, { "Th", 2.06f }, { "Pa", 2.00f }, { "U", 1.96f },
            { "Np", 1.90f }, { "Pu", 1.87f }, { "Am", 1.80f }, { "Cm", 1.69f }, { "Bk", 2.50f },
            { "Cf", 2.50f }, { "Es", 2.50f }, { "Fm", 2.50f }, { "Md", 2.50f }, { "No", 2.50f },
            { "Lr", 2.50f }, { "Rf", 2.50f }, { "Db", 2.50f }, { "Sg", 2.50f }, { "Bh", 2.50f },
            { "Hs", 2.50f }, { "Mt", 2.50f }, { "Ds", 2.50f }, { "Rg", 2.50f }, { "Cn", 2.50f },
            { "Uut", 2.50f }, { "Fl", 2.50f }, { "Uup", 2.50f }, { "Lv", 2.50f }, { "Uus", 2.50f },
            { "UUo", 2.50f }
        };

        eleTable = new Dictionary<string, int>()
        {
                { "H",1},{ "He",2},{ "Li",3},{ "Be",4},
                { "B",5},{ "C",6},{ "N",7},{ "O",8},{ "F",9},
                { "Ne",10},{ "Na",11},{ "Mg",12},{ "Al",13},{ "Si",14},
                { "P",15},{ "S",16},{ "Cl",17},{ "Ar",18},{ "K",19},
                { "Ca",20},{ "Sc",21},{ "Ti",22},{ "V",23},{ "Cr",24},
                { "Mn",25},{ "Fe",26},{ "Co",27},{ "Ni",28},{ "Cu",29},
                { "Zn",30},{ "Ga",31},{ "Ge",32},{ "As",33},{ "Se",34},
                { "Br",35},{ "Kr",36},{ "Rb",37},{ "Sr",38},{ "Y",39},
                { "Zr",40},{ "Nb",41},{ "Mo",42},{ "Tc",43},{ "Ru",44},
                { "Rh",45},{ "Pd",46},{ "Ag",47},{ "Cd",48},{ "In",49},
                { "Sn",50},{ "Sb",51},{ "Te",52},{ "I",53},{ "Xe",54},
                { "Cs",55},{ "Ba",56},{ "La",57},{ "Ce",58},{ "Pr",59},
                { "Nd",60},{ "Pm",61},{ "Sm",62},{ "Eu",63},{ "Gd",64},
                { "Tb",65},{ "Dy",66},{ "Ho",67},{ "Er",68},{ "Tm",69},
                { "Yb",70},{ "Lu",71},{ "Hf",72},{ "Ta",73},{ "W",74},
                { "Re",75},{ "Os",76},{ "Ir",77},{ "Pt",78},{ "Au",79},
                { "Hg",80},{ "Tl",81},{ "Pb",82},{ "Bi",83},{ "Po",84},
                { "At",85},{ "Rn",86},{ "Fr",87},{ "Ra",88},{ "Ac",89},
                { "Th",90},{ "Pa",91},{ "U",92},{ "Np",93},{ "Pu",94},
                { "Am",95},{ "Cm",96},{ "Bk",97},{ "Cf",98},{ "Es",99},
                { "Fm",100},{ "Md",101},{ "No",102},{ "Lr",103},{ "Rf",104},
                { "Db",105},{ "Sg",106},{ "Bh",107},{ "Hs",108},{ "Mt",109},
                { "Ds",110},{ "Rg",111},{ "Cn",112},{ "Uut",113},{ "Fl",114},
                { "Uup",115},{ "Lv",116},{ "Uus",117},{ "UUo",118}
        };
        eleColor = new Dictionary<string,Color> () 
        {
            { "H" ,rgb2float(255, 255, 255)},{ "He",rgb2float(217, 255, 255)},{ "Li",rgb2float(204, 128, 255)},{ "Be",rgb2float(194, 255, 0)},{ "B" ,rgb2float(255, 181, 181)},
            { "C" ,rgb2float(144, 144, 144)},{ "N" ,rgb2float(48, 80, 248)},{ "O" ,rgb2float(255, 13, 13)},{ "F" ,rgb2float(144, 224, 80)},{ "Ne",rgb2float(179, 227, 245)},
            { "Na",rgb2float(171, 92, 242)},{ "Mg",rgb2float(138, 255, 0)},{ "Al",rgb2float(191, 166, 166)},{ "Si",rgb2float(240, 200, 160)},{ "P" ,rgb2float(255, 128, 0)},
            { "S" ,rgb2float(255, 255, 48)},{ "Cl",rgb2float(31, 240, 31)},{ "Ar",rgb2float(128, 209, 227)},{ "K" ,rgb2float(143, 64, 212)},{ "Ca",rgb2float(61, 255, 0)},
            { "Sc",rgb2float(230, 230, 230)},{ "Ti",rgb2float(191, 194, 199)},{ "V" ,rgb2float(166, 166, 171)},{ "Cr",rgb2float(138, 153, 199)},{ "Mn",rgb2float(156, 122, 199)},
            { "Fe",rgb2float(224, 102, 51)},{ "Co",rgb2float(240, 144, 160)},{ "Ni",rgb2float(80, 208, 80)},{ "Cu",rgb2float(200, 128, 51) },{ "Zn",rgb2float(125, 128, 176)},
            { "Ga",rgb2float(194, 143, 143)},{ "Ge",rgb2float(102, 143, 143)},{ "As",rgb2float(189, 128, 227)},{ "Se",rgb2float(255, 161, 0)},{ "Br",rgb2float(166, 41, 41)},
            { "Kr",rgb2float(92, 184, 209)},{ "Rb",rgb2float(112, 46, 176)},{ "Sr",rgb2float(0, 255, 0)},{ "Y" ,rgb2float(148, 255, 255)},{ "Zr",rgb2float(148, 224, 224)},
            { "Nb",rgb2float(115, 194, 201)},{ "Mo",rgb2float(84, 181, 181)},{ "Tc",rgb2float(59, 158, 158)},{ "Ru",rgb2float(36, 143, 143)},{ "Rh",rgb2float(10, 125, 140)},
            { "Pd",rgb2float(0, 105, 133)},{ "Ag",rgb2float(192, 192, 192)},{ "Cd",rgb2float(255, 217, 143)},{ "In",rgb2float(166, 117, 115)},{ "Sn",rgb2float(102, 128, 128)},
            { "Sb",rgb2float(158, 99, 181)},{ "Te",rgb2float(212, 122, 0)},{ "I" ,rgb2float(148, 0, 148)},{ "Xe",rgb2float(66, 158, 176)},{ "Cs",rgb2float(87, 23, 143)},
            { "Ba",rgb2float(0, 201, 0)},{ "La",rgb2float(112, 212, 255)},{ "Ce",rgb2float(255, 255, 199)},{ "Pr",rgb2float(217, 255, 199)},{ "Nd",rgb2float(199, 255, 199)},
            { "Pm",rgb2float(163, 255, 199)},{ "Sm",rgb2float(163, 255, 199)},{ "Eu",rgb2float(97, 255, 199)},{ "Gd",rgb2float(69, 255, 199)},{ "Tb",rgb2float(69, 255, 199)},
            { "Dy",rgb2float(69, 255, 199)},{ "Ho",rgb2float(69, 255, 199)},{ "Er",rgb2float(0, 230, 117)},{ "Tm",rgb2float(0, 212, 82)},{ "Yb",rgb2float(0, 191, 56)},
            { "Lu",rgb2float(0, 171, 36)},{ "Hf",rgb2float(77, 194, 255)},{ "Ta",rgb2float(77, 194, 255)},{ "W" ,rgb2float(33, 148, 214)},{ "Re",rgb2float(38, 125, 171)},
            { "Os",rgb2float(38, 102, 150)},{ "Ir",rgb2float(23, 84, 135)},{ "Pt",rgb2float(208, 208, 224)},{ "Au",rgb2float(255, 209, 35)},{ "Hg",rgb2float(184, 184, 208)},
            { "Tl",rgb2float(166, 84, 77)},{ "Pb",rgb2float(87, 89, 97)},{ "Bi",rgb2float(158, 79, 181)},{ "Po",rgb2float(171, 92, 0)},{ "At",rgb2float(117, 79, 69)},
            { "Rn",rgb2float(66, 130, 150)},{ "Fr",rgb2float(66, 0, 102)},{ "Ra",rgb2float(0, 125, 0)},{ "Ac",rgb2float(112, 171, 250)},{ "Th",rgb2float(0, 186, 255)},
            { "Pa",rgb2float(0, 161, 255)},{ "U" ,rgb2float(0, 143, 255)},{ "Np",rgb2float(0, 128, 255)},{ "Pu",rgb2float(0, 107, 255)},{ "Am",rgb2float(84, 92, 242)},
            { "Cm",rgb2float(120, 92, 227)},{ "Bk",rgb2float(138, 79, 227)},{ "Cf",rgb2float(161, 54, 212)},{ "Es",rgb2float(179, 31, 212)},{ "Fm",rgb2float(179, 31, 186)},
            { "Md",rgb2float(179, 13, 166)},{ "No",rgb2float(189, 13, 135)},{ "Lr",rgb2float(199, 0, 102)},{ "Rf",rgb2float(204, 0, 89)},{ "Db",rgb2float(209, 0, 79)},
            { "Sg",rgb2float(217, 0, 69)},{ "Bh",rgb2float(224, 0, 56)},{ "Hs",rgb2float(230, 0, 46)},{ "Mt",rgb2float(235, 0, 38)}
        };
    }

    private Color rgb2float(int r,int g,int b)
    {
        return new Color(r / 255.0f, g / 255.0f, b / 255.0f);
    }

    public void readGlobalPara()
    {
        globalPara = new Dictionary<string, string>();
#if !UNITY_WEBGL
        string[] lines = File.ReadAllLines("allkey.log");
#else

#endif
        foreach (string lin in lines)
        {
            if (lin.Trim() == "") continue;
            string[] kv = lin.Split('=');
            globalPara.Add(kv[0], kv[1]);
        }
        foreach(string para in necessaryGlobalPara)
        {
            if( !globalPara.ContainsKey(para))
            {
                printStatusInfo("NOT Cantain KEY " + para+" ,plz check allkey.log and RESTART the program");
            }
        }
    }

    public void printStructureInfo(string str)
    {
        structureText.text = str;
        Debug.Log(str);
    }

    public void printStatusInfo(string str)
    {
        statusText.text = str;
        Debug.Log(str);
    }

    public void printErrorInfo()
    {
        //print to File
    }


    public void UnityToWeb(string str)
    {
        //参数一：Web里面接收数据的方法，参数二：Unity传递的数据（我这里是输入就传递）
        Application.ExternalCall("GetFromUnity", str);
    }

    public string WebToUnity(string s)
    {
        return s;
    }
}
