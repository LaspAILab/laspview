using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UIElements;

public class FilePanelController : MonoBehaviour
{
    private VisualElement filePanelRoot;
    
    public static ExtensionFilter[] Extensions = new[]
    {
        new ExtensionFilter("Structure Files", "txt", "arc", "car", "poscar", "cif"),
        new ExtensionFilter("Arc Files", "txt", "arc", "car"),
        new ExtensionFilter("Poscar Files", "poscar"),
        new ExtensionFilter("Cif Files", "cif"),
        new ExtensionFilter("All Files", "*"),
    };
    
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
    
    void OnSaveAllButtonClick(ClickEvent _)
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