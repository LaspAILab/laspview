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
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEditor;
using System.Runtime.ConstrainedExecution;
using UnityEngine.UI;

public class MainScr : MonoBehaviour
{
    //static public Transform cameraCenter;//获取旋转目标
    //static public MainScr camera;

    static public MainScr S;
    public static GameObject structureText;
#if !UNITY_WEBGL
    public LocalServer localServer;
#endif
    
    public float bondTolerance = 1.35f;

    void Awake()
    {
        S = this;
        structureText = GameObject.Find("StructureText");
#if !UNITY_WEBGL
        localServer = this.gameObject.AddComponent<LocalServer>();
#endif
    }

    public void OnChangeArc()
    {
        int index = int.Parse(structureText.transform.GetChild(1).gameObject.GetComponent<InputField>().text) - 1;
        index = Mathf.Min(App.Instance.CurrentFile.Structures.Count - 1, index);
        App.Instance.SwitchStructure(index);
    }

#if !UNITY_WEBGL
    void OnApplicationQuit()
    {
        localServer.Quit1();
    }
#endif
}
