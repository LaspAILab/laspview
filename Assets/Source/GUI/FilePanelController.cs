using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SFB;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class FilePanelController : MonoBehaviour
{
    private VisualElement filePanelRoot;

    public static ExtensionFilter[] Extensions =
    {
        new ExtensionFilter("Structure Files", "txt", "arc", "car", "poscar", "cif"),
        new ExtensionFilter("Arc Files", "txt", "arc", "car"),
        new ExtensionFilter("Poscar Files", "poscar"),
        new ExtensionFilter("Cif Files", "cif"),
        new ExtensionFilter("All Files", "*"),
    };

#if UNITY_WEBGL
    //
    // WebGL
    //
    private DropdownField formatSelector;

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray,
        int byteArraySize);

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
        OverlayUiController.Instance.PrintStatusLog("File successfully downloaded");
    }

    public void DownloadStructureFile(StructureFile structure, string filename)
    {
        string str = IFormat.GetFormatIO(filename).Write(structure);
        var bytes = Encoding.UTF8.GetBytes(str);
        DownloadFile(gameObject.name, "OnFileDownload", filename, bytes, bytes.Length);
    }
#endif

    void Start()
    {
        filePanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("FilePanel");

        var newEmptyButton = filePanelRoot.Q<Button>("NewEmptyButton");
        newEmptyButton.RegisterCallback<ClickEvent>(OnNewEmptyButtonClick);

        var openFileButton = filePanelRoot.Q<Button>("OpenFileButton");
        openFileButton.RegisterCallback<ClickEvent>(OnOpenFileButtonClick);

        var saveCurrentButton = filePanelRoot.Q<Button>("SaveCurrentButton");
        saveCurrentButton.RegisterCallback<ClickEvent>(OnSaveCurrentButtonClick);

        var saveAllButton = filePanelRoot.Q<Button>("SaveAllButton");
        saveAllButton.RegisterCallback<ClickEvent>(OnSaveAllButtonClick);

#if UNITY_WEBGL
        // Enable the format selector in that WebGL doesn't have a save file dialog
        formatSelector = filePanelRoot.Q<DropdownField>("WebFormatSelector");
        formatSelector.style.display = DisplayStyle.Flex;
        formatSelector.choices = Extensions[0].Extensions.ToList();
        formatSelector.value = "txt";
#endif
    }

    public static void FilesRead(string[] paths)
    {
        // Automatically open the first file after loaded
        bool isFirst = true;
        foreach (var path in paths)
        {
            App.Instance.OpenArc(File.ReadLines(path), Path.GetFileName(path), isFirst);
            isFirst = false;
        }
    }

    void OnNewEmptyButtonClick(ClickEvent _)
    {
        App.Instance.NewEmpty();
    }

    void OnOpenFileButtonClick(ClickEvent _)
    {
        // Open file with filter
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Multiselect is ignored in editor, this is not a bug
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", Extensions, true, FilesRead);
#elif UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUpload", ".txt,.arc,.car,.poscar,.cif", true);
#elif UNITY_STANDALONE_OSX
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", Extensions, true);
        FilesRead(paths);
#endif
    }

    void OnSaveCurrentButtonClick(ClickEvent _)
    {
        if (App.Instance.viewport.CurrentStructureView == null)
        {
            return;
        }

#if UNITY_WEBGL
        StructureFile newFile = new("");
        newFile.Structures.Add(App.Instance.viewport.CurrentStructureView.Structure);
        DownloadStructureFile(newFile, "result." + formatSelector.value);
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

            // File name is irrevalent here, needn't set
            StructureFile newStructureFile = new("");
            newStructureFile.Structures.Add(App.Instance.viewport.CurrentStructureView.Structure);
            string str = formatSaver.Write(newStructureFile);
            File.WriteAllText(path, str);
        });
#endif
    }

    void OnSaveAllButtonClick(ClickEvent _)
    {
        if (App.Instance.CurrentFile == null)
        {
            return;
        }

#if UNITY_WEBGL
        DownloadStructureFile(App.Instance.CurrentFile, "result." + formatSelector.value);
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