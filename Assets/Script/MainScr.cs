using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using ViewerNetwork;
using Lasp4Win;
using JetBrains.Annotations;

public class MainScr : MonoBehaviour
{
    //static public Transform cameraCenter;//获取旋转目标
    //static public MainScr camera;

    static public MainScr S;
#if !UNITY_WEBGL
    public LocalServer localServer;
#endif
    public GameObject ArcStructuresPrefabVar;
    public List<ArcStructures> arcList;
    public int currentArc = -1;

    //Camera parameters;
    //private float m_fLastMousePosX = 0.0f;
    //private float m_fLastMousePosY = 0.0f;
    //private bool m_bMouseRightKeyDown = false;
    public float rotateSpeed = 0.2f;

    void Awake()
    {
        S = this;
#if !UNITY_WEBGL
        localServer = this.gameObject.AddComponent<LocalServer>();
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_WEBGL
        string filename = ConFun.S.globalPara["arc_default"];
        AddArc(filename);
#else
        string lines = 
@"!BIOSYM archive 2 
PBC = ON
     Energy           10          10  -129.895506
!DATE
PBC    7.6679    7.6679   15.0000   90.0000   90.0000  120.0000
H       0.008413248     4.455233640     8.566392459 CORE    1  H H   0.0000    1
H       6.257571388     4.243667943     6.666642087 CORE    2  H H   0.0000    2
H       3.815253288     0.753089160     5.124217983 CORE    3  H H   0.0000    3
H       1.342005797     4.028462789     7.451793384 CORE    4  H H   0.0000    4
H       0.086574563     2.814818537     7.852299561 CORE    5  H H   0.0000    5
H       2.572615996     5.887984031     5.112457470 CORE    6  H H   0.0000    6
C       0.279671901     3.881257765     7.667971729 CORE    7  C C   0.0000    7
O      -0.454394321     4.353730708     6.513752863 CORE    8  O O   0.0000    8
end
end
";
        AddArc("test.arc" ,lines);
#endif
    }

    private void LateUpdate()
    {


    }



    public void AddArc(string wholeFilename)
    {
#if !UNITY_WEBGL
        string[] lines = File.ReadAllLines(wholeFilename);
        string filename = Path.GetFileName(wholeFilename);
        this.AddArc(filename, lines);
#endif
    }

    public void AddArc(string filename, string[] lines)
    {
        ArcStructures newArc = Instantiate(ArcStructuresPrefabVar).GetComponent<ArcStructures>();
        try
        {
            //print(lines.Length);
            newArc.ArcRead(filename, lines);
        }
        catch(Exception)
        {
            GameObject.Destroy(newArc.gameObject);
            ConFun.S.printStatusInfo($"Err: Error in read file {filename}.Please check file.");
        }
        arcList.Add(newArc);
        L_ProjectPanelSrc.S.ProjectListAddCell(filename);
        DrawArc(arcList.Count - 1);
    }

    public void AddArc(string filename, string msg)
    {
        string[] lines = msg.Split("\r\n".ToCharArray());
        AddArc(filename, lines);
    }


    public void AddArcByCoroutine(string fileName, string msg)
    {
        StartCoroutine(AddArcCorou( fileName, msg));
    }

    IEnumerator AddArcCorou(string fileName, string msg)
    {
        while (true)
        {
            if (GuiMain.S.editEnable || B_ToolbarPanelSrc.S.onPlay)
            {
                yield return null;
            }
            else
            {
                AddArc(fileName, msg);
                break;
            }
        }
    }

    public void DrawArc(int arcIndex)
    {
        arcList[currentArc].DeDrawArc();
        arcList[currentArc].gameObject.SetActive(false);
        ArcStructures targetArc = arcList[arcIndex];
        targetArc.gameObject.SetActive(true);
        currentArc = arcIndex;
        L_DisplayPanelSrc.S.DisplayLoadModelPara();
        //print(arcList[currentArc].filename);
        (bool, bool) playerStatus = targetArc.DrawStructure(targetArc.currentStr);
        GuiMain.S.FocusCameraCenter();
        B_ToolbarPanelSrc.S.PlayerBtnChecked(playerStatus);
    }

    public ArcStructures CurrentArc()
    {
        return arcList[currentArc];
    }

    public Structure CurrentStructure()
    {
        return arcList[currentArc].CurrentStructure();
    }

#if !UNITY_WEBGL
    void OnApplicationQuit()
    {
        localServer.Quit1();
    }
#endif




}
