using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using SFB;
public class L_FilePanelSrc : MonoBehaviour
{
    static public L_FilePanelSrc S;

#if !UNITY_WEBGL
    void Awake()
    {
        S = this;
    }

    public void FileOpen()
    {
        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("arc Files", "arc", "car"),
            new ExtensionFilter("All Files", "*" ),
            };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, true, (string[] paths) =>
        {
            foreach(string str in paths)
            {
                MainScr.S.AddArc(str);
            }
        });
    }

    public void FileSaveSingle()
    {
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", ".arc", "", (string path) => 
        { 
            string str = MainScr.S.CurrentArc().ArcWrite();
            File.WriteAllText(path, str);
        });
    }

    public void FileSaveAll()
    {
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", ".arc", "", (string path) =>
        {
            string str = MainScr.S.CurrentArc().ArcWriteAll();
            File.WriteAllText(path, str);
        });
    }
#endif
}
