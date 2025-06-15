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

    [DllImport("__Internal")]
    private static extern void AskForSaveFormat(string gameObjectName, string methodName);

    // Called from browser
    public void OnFileUpload(string url)
    {
        StartCoroutine(OutputRoutine(url.Split(';')));
    }

    private IEnumerator OutputRoutine(string[] files)
    {
        foreach (var file in files)
        {
            // Url, filename
            var parts = file.Split('|');
            Debug.Log($"Loading file {parts[1]}, url: {parts[0]}");
            var loader = UnityWebRequest.Get(parts[0]);
            yield return loader.SendWebRequest();

            if (loader.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("WebGL upload error: " + loader.error);
            }
            else
            {
                App.Instance.OpenArc(loader.downloadHandler.text.Split('\n'), Path.GetFileName(parts[1]));
            }
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
        new ExtensionFilter("Structure Files", "txt", "arc", "car", "poscar", "cif"),
        new ExtensionFilter("Arc Files",  "txt", "arc", "car"),
        new ExtensionFilter("Poscar Files", "poscar"),
        new ExtensionFilter("Cif Files", "cif"),
        new ExtensionFilter("All Files", "*"),
    };

    public void FileOpen()
    {
        // Open file with filter
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Multiselect is ignored in editor, this is not a bug
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", Extensions, true, (paths) =>
        {
            foreach (var path in paths)
            {
                FileRead(path);
            }
        });
#elif UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUpload", ".txt,.arc,.car,.poscar,.cif", true);
#elif UNITY_STANDALONE_OSX
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", Extensions, true);
        foreach (var path in paths)
        {
            FileRead(path);
        }
#endif
    }
    
#if UNITY_WEBGL && !UNITY_EDITOR
    private StructureFile pendingFile = null;
    
    public void WebFileDownloadCallback(string filename)
    {
        string str = IFormat.GetFormatIO(filename).Write(pendingFile);
        var bytes = Encoding.UTF8.GetBytes(str);
        DownloadFile(gameObject.name, "OnFileDownload", filename, bytes, bytes.Length);
    }
#endif

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
        StructureFile newFile = new();
        newFile.Structures.Add(App.Instance.Viewport.CurrentStructureView.Structure);
        pendingFile = newFile;
        AskForSaveFormat(gameObject.name, "WebFileDownloadCallback");
#else
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", Extensions, (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            IFormat formatSaver = IFormat.GetFormatIO(path);
            if (formatSaver == null)
            {
                Debug.LogError($"Invalid format for file `{Path.GetFileName(path)}`, cannot save");
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
        pendingFile = App.Instance.CurrentFile;
        AskForSaveFormat(gameObject.name, "WebFileDownloadCallback");
#else
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", Extensions, (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            IFormat formatSaver = IFormat.GetFormatIO(path);
            if (formatSaver == null)
            {
                Debug.LogError($"Invalid format for file `{Path.GetFileName(path)}`, cannot save");
                return;
            }

            string str = formatSaver.Write(App.Instance.CurrentFile);
            File.WriteAllText(path, str);
        });
#endif
    }
}