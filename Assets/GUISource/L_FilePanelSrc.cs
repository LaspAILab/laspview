using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using SFB;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class L_FilePanelSrc : MonoBehaviour
{
    public static L_FilePanelSrc S;

#if UNITY_WEBGL
    //
    // WebGL
    //
    private string arcText;

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray,
        int byteArraySize);

    // Called from browser
    public void OnFileUpload(string url)
    {
        StartCoroutine(OutputRoutine(url));
    }

    private IEnumerator OutputRoutine(string url)
    {
        var loader = UnityWebRequest.Get(url);
        yield return loader.SendWebRequest();

        if (loader.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("WebGL upload error: " + loader.error);
        }
        else
        {
            App.Instance.OpenArcString(loader.downloadHandler.text);
        }
    }

    // Called from browser
    public void OnFileDownload()
    {
        ConFun.UpdateStatusLabel("File Successfully Downloaded");
    }
#endif

    void Awake()
    {
        S = this;

        //Set FilePanel
        transform.SetParent(GuiMain.S.leftCanvas.transform, false);
        transform.SetAsFirstSibling();
        transform.localScale = Vector3.one;
        Button flButton = GameObject.Find("FileBtn").GetComponent<Button>();
        flButton.onClick.AddListener(() =>
        {
            if (!gameObject.activeSelf)
            {
                GuiMain.S.ClearLeftPanel();
            }
            
            GuiMain.S.BtnHideObject(flButton, gameObject);
        });
        // gameObject.SetActive(false);
    }

    public static ExtensionFilter[] Extensions = new[]
    {
        new ExtensionFilter("Structure Files", "arc", "car", "poscar", "cif"),
        new ExtensionFilter("Arc Files", "arc", "car"),
        new ExtensionFilter("Poscar Files", "poscar"),
        new ExtensionFilter("Cif Files", "cif"),
        new ExtensionFilter("All Files", "*"),
    };

    public void FileOpen()
    {
        // Open file with filter
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", Extensions, false, (paths) =>
        {
            if (paths.Length > 0)
            {
                FileRead(paths[0]);
            }
        });
#elif UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUpload", ".arc,.car", false);
#elif UNITY_STANDALONE_OSX
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", Extensions, false);
        FileRead(paths[0]);
#endif
    }

    private void FileRead(string path)
    {
        App.Instance.OpenArc(File.ReadLines(path), Path.GetFileName(path));
    }

    public void NewStructure()
    {
        App.Instance.NewEmpty();
    }

    public void FileSaveSingle()
    {
        if (App.Instance.Viewport.CurrentStructureView == null)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        ArcFile newArcFile = new();
        newArcFile.Structures.Add(App.Instance.Viewport.CurrentStructureView.Structure);
        var bytes = Encoding.UTF8.GetBytes(newArcFile.WriteArc());
        DownloadFile(gameObject.name, "OnFileDownload", "result.arc", bytes, bytes.Length);
#else
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", Extensions, (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            string format = Path.GetExtension(path).Substring(1);
            IFormat formatSaver = IFormat.GetFormatIO(format);
            if (formatSaver == null)
            {
                Debug.LogError($"Invalid format `{format}`, cannot save");
                return;
            }
                
            StructureFile newStructureFile = new();
            newStructureFile.Structures.Add(App.Instance.Viewport.CurrentStructureView.Structure);
            string str = formatSaver.Write(newStructureFile);
            File.WriteAllText(path, str);
        });
#endif
    }

    public void FileSaveAll()
    {
        if (App.Instance.CurrentFile == null)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        string str = App.Instance.CurrentFile.WriteArc();
        var bytes = Encoding.UTF8.GetBytes(str);
        DownloadFile(gameObject.name, "OnFileDownload", "result.arc", bytes, bytes.Length);
#else
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", Extensions, (string path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string format = Path.GetExtension(path).Substring(1);
            IFormat formatSaver = IFormat.GetFormatIO(format);
            if (formatSaver == null)
            {
                Debug.LogError($"Invalid format `{format}`, cannot save");
                return;
            }

            string str = formatSaver.Write(App.Instance.CurrentFile);
            File.WriteAllText(path, str);
        });
#endif
    }
}