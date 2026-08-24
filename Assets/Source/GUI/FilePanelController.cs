using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SFB;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class FilePanelController : MonoBehaviour
{
    public static FilePanelController Instance;

    private VisualElement filePanelRoot;

    static ExtensionFilter[] CrystalExtensions =
    {
        new ("Structure Files", "txt", "arc", "car", "poscar", "cif", "cub", "extxyz"),
        new ("Arc Files", "txt", "arc", "car"),
        new ("Poscar Files", "poscar"),
        new ("Cif Files", "cif"),
        new ("Cube Files", "cub"),
        new ("Extxyz Files", "extxyz"),
        new ("All Files", "*"),
    };

    static ExtensionFilter[] MoleculeExtensions =
    {
        new ("Molecule Files", "mol", "xyz"),
        new ("Mol Files", "mol"),
        new ("Xyz Files", "xyz"),
        new ("All Files", "*"),
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
        bool isFirst = true;
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
                if (IsMoleculeFile(parts[1]))
                {
                    // Import molecule
                    StructureSequenceController.Instance.ImportMolecule(loader.downloadHandler.text.Split('\n'), Path.GetFileName(parts[1]));
                }
                else
                {
                    // Open structure file
                    App.Instance.OpenArc(loader.downloadHandler.text.Split('\n'), Path.GetFileName(parts[1]), true, isFirst);
                    isFirst = false;
                }
            }
        }
    }

    // Called from browser
    public void OnFileDownload()
    {
        OverlayUiController.Instance.PrintStatusLog("File successfully downloaded");
    }

    void DownloadStructureFile(StructureFile structure, string filename)
    {
        string str = ICrystalFormat.GetFormatIO(filename).Write(structure);
        RequestDownloadFile(Encoding.UTF8.GetBytes(str), filename);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestDownloadFile(byte[] bytes, string filename)
    {
        DownloadFile(gameObject.name, "OnFileDownload", filename, bytes, bytes.Length);
    }
#endif

    public static bool IsMoleculeFile(string filename)
    {
        string ext = Path.GetExtension(filename).ToLower();
        return MoleculeExtensions[0].Extensions.Contains(ext.TrimStart('.'));
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        filePanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("FilePanel");

        var newEmptyButton = filePanelRoot.Q<Button>("NewEmptyButton");
        newEmptyButton.RegisterCallback<ClickEvent>(OnNewEmptyButtonClick);

        var openFileButton = filePanelRoot.Q<Button>("OpenFileButton");
        openFileButton.RegisterCallback<ClickEvent>(OnOpenFileButtonClick);

        var importMoleculeButton = filePanelRoot.Q<Button>("ImportMoleculeButton");
        importMoleculeButton.RegisterCallback<ClickEvent>(OnImportMoleculeButtonClick);

        var saveCurrentButton = filePanelRoot.Q<Button>("SaveCurrentButton");
        saveCurrentButton.RegisterCallback<ClickEvent>(OnSaveCurrentButtonClick);

        var saveAllButton = filePanelRoot.Q<Button>("SaveAllButton");
        saveAllButton.RegisterCallback<ClickEvent>(OnSaveAllButtonClick);

#if UNITY_WEBGL
        // Enable the format selector in that WebGL doesn't have a save file dialog
        formatSelector = filePanelRoot.Q<DropdownField>("WebFormatSelector");
        formatSelector.style.display = DisplayStyle.Flex;
        formatSelector.choices = CrystalExtensions[0].Extensions.ToList();
        formatSelector.value = "txt";
#endif
    }

    public static void FilesRead(string[] paths)
    {
        // Automatically open the first file after loaded
        bool isFirst = true;
        foreach (var path in paths)
        {
            App.Instance.OpenArc(File.ReadLines(path), Path.GetFileName(path), true, isFirst);
            isFirst = false;
        }
    }

    public static void MoleculeImport(string[] paths)
    {
        if (paths.Length == 0)
        {
            return;
        }

        var moleculeFile = paths.First();
        StructureSequenceController.Instance.ImportMolecule(File.ReadLines(moleculeFile), Path.GetFileName(moleculeFile));
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
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", CrystalExtensions, true, FilesRead);
#elif UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUpload", ".txt,.arc,.car,.poscar,.cif", true);
#elif UNITY_STANDALONE_OSX
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", CrystalExtensions, true);
        FilesRead(paths);
#endif
    }

    void OnImportMoleculeButtonClick(ClickEvent _)
    {
        // Open file with filter
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Multiselect is ignored in editor, this is not a bug
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", MoleculeExtensions, false, MoleculeImport);
#elif UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUpload", ".mol,.xyz", false);
#elif UNITY_STANDALONE_OSX
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", MoleculeExtensions, false);
        MoleculeImport(paths);
#endif
    }

    void OnSaveCurrentButtonClick(ClickEvent _)
    {
        if (App.Instance.viewport.CurrentStructureView == null)
        {
            return;
        }

#if UNITY_WEBGL
        StructureFile newFile = new();
        newFile.Structures.Add(App.Instance.viewport.CurrentStructureView.Structure);
        DownloadStructureFile(newFile, "result." + formatSelector.value);
#else
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", CrystalExtensions, (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ICrystalFormat formatSaver = ICrystalFormat.GetFormatIO(path);
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
        StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", CrystalExtensions, (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ICrystalFormat formatSaver = ICrystalFormat.GetFormatIO(path);
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