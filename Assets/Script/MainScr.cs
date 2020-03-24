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
        string filename = ConFun.S.globalPara["arc_default"];
        AddArc(filename);
    }
    //public void Start()
    //{
    //    Dictionary<string, string> attr = new Dictionary<string, string>();
    //    localServer = new LocalServer();
    //    string tag = "";
    //    localServer.RmTag("<laspview user=shiyf><file>3.arc</file></laspview>",out tag, attr);
    //}
    public void AddArc(string filename)
    {
        string[] lines = File.ReadAllLines(filename);
        ArcStructures newArc = Instantiate(ArcStructuresPrefabVar).GetComponent<ArcStructures>();
        newArc.ArcRead(filename,lines);
        arcList.Add(newArc);
        GuiLogic.S.FileListAddCell(filename);
        DrawArc(arcList.Count - 1);
    }

    public void AddArc(string filename, string[] lines)
    {
        ArcStructures newArc = Instantiate(ArcStructuresPrefabVar).GetComponent<ArcStructures>();
        newArc.ArcRead(filename, lines);
        arcList.Add(newArc);
        GuiLogic.S.FileListAddCell(filename);
        DrawArc(arcList.Count - 1);
    }

    public void AddArc(string filename, string msg)
    {
        string[] lines = msg.Split("\r\n".ToCharArray());
        AddArc(filename, lines);
    }

    public void DrawArc(int arcIndex)
    {
        ArcStructures targetArc = arcList[arcIndex];
        arcList[currentArc].gameObject.SetActiveRecursively(false);
        targetArc.gameObject.SetActive(true);
        currentArc = arcIndex;
        print(arcList[currentArc].filename);
        (bool, bool) playerStatus = targetArc.DrawStructure(targetArc.currentStr);
        GuiLogic.S.PlayerBtnChecked(playerStatus);
    }

    public ArcStructures CurrentArc()
    {
        return arcList[currentArc];
    }

    public Structure CurrentStructure()
    {
        return arcList[currentArc].CurrentStructure();
    }
    void Update()
    {

    }
#if !UNITY_WEBGL
    void OnApplicationQuit()
    {
        localServer.Quit1();
    }
#endif
}
