using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using SFB;
public class L_FilePanelSrc : MonoBehaviour
{
    static public L_FilePanelSrc S;

    public GameObject M_LaspInWidgetPrefab;

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    private string arcFile;

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif

    void Awake()
    {
        S = this;

        //Set FilePanel
        this.transform.SetParent(GuiMain.S.leftCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button flButton = GameObject.Find("FileBtn").GetComponent<Button>();
        flButton.onClick.AddListener(() =>
        {
            GuiMain.S.BtnHideObject(flButton, this.gameObject);
        });
        this.gameObject.SetActive(false);
    }
    public void FileOpen()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "FileOnFileUpload", ".arc", true);
#else

        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("arc Files", "arc", "car"),
            new ExtensionFilter("All Files", "*" ),
            };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, true, (string[] paths) =>
        {
            FileRead(paths);
        });
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    public void FileOnFileUpload(string urls)
    {
        StartCoroutine(OutputRoutine(urls.Split(',')));
    }


    private IEnumerator OutputRoutine(string[] urlArr)
    {
        var outputText = "";
        for (int i = 0; i < urlArr.Length; i++)
        {
            ConFun.S.printStatusInfo(urlArr[i]);
            var loader = new WWW(urlArr[i]);
            yield return loader;
            outputText += loader.text;
        }
        ////MainScr.S.AddArc(outputText);
        //GameObject m_LaspInWidget = Instantiate(M_LaspInWidgetPrefab);
        //M_LaspInWidgetSrc.S.mainText.text = outputText;
    }
#endif
    private void FileRead(string[] paths)
    {
        foreach (string str in paths)
        {
            MainScr.S.AddArc(str);
        }
    }

    public void FileSaveSingle()
    {

        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", ".arc", "", (string path) => 
        { 
            string str = MainScr.S.CurrentArc().ArcWrite();
#if !UNITY_WEBGL
            File.WriteAllText(path, str);
#endif
        });
    }

    //public void FileSaveAll()
    //{
    //    StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", ".arc", "", (string path) =>
    //    {
    //        string str = MainScr.S.CurrentArc().ArcWriteAll();
    //        File.WriteAllText(path, str);
    //    });
    //}

}
