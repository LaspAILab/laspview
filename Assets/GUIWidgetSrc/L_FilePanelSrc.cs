using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class L_FilePanelSrc : MonoBehaviour
{
    static public L_FilePanelSrc S;
    static public InputField openbFilePath;
#if !UNITY_WEBGL
    void Awake()
    {
        S = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        openbFilePath = GameObject.Find("OpenFileInputField").GetComponent<InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FileOpen()
    {
        MainScr.S.AddArc(openbFilePath.text);
    }

    public void FileSave()
    {
        string str = MainScr.S.CurrentArc().ArcWrite();
        File.WriteAllText(openbFilePath.text, str);
    }
#endif
}
