// Unity cannot serialize raw json arrays, so we wrap it with a class as a workaround

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using Application = UnityEngine.Application;

[Serializable]
public struct SerializedElementContainer
{
    public SerializableElement[] Elements;

    [Serializable]
    public struct SerializableElement
    {
        public int Number;
        public string Symbol;
        public float Radius;
        public int[] Color;
    }
}

public struct ElementData
{
    public int Number;
    public float Radius;
    public Color Color;

    public ElementData(SerializedElementContainer.SerializableElement serializableElement)
    {
        Number = serializableElement.Number;
        Radius = serializableElement.Radius;
        Color = new Color(serializableElement.Color[0] / 255.0f, serializableElement.Color[1] / 255.0f,
            serializableElement.Color[2] / 255.0f, serializableElement.Color[3] / 255.0f);
    }
}

public class App : MonoBehaviour
{
    public static App Instance;

    [NonSerialized] public static bool IsRunningOnMacOS = false;

    [NonSerialized] public static Dictionary<string, ElementData> Elements;

    [NonSerialized] public ViewportController viewport;

    [NonSerialized] public OperationManager operationManager;

#if !UNITY_WEBGL
    // Queue for scheduling tasks on the main thread
    private readonly ConcurrentQueue<Action> callbackQueue = new();
#endif

    public float BondTolerance
    {
        get => CrystalStructureView.BondTolerance;
        set
        {
            if (Mathf.Approximately(CrystalStructureView.BondTolerance, value))
            {
                return;
            }

            CrystalStructureView.BondTolerance = value;
            viewport.RecalculateBonds();
        }
    }

    public CrossCellBondPolicy BondCalculationPolicy
    {
        get => CrystalStructureView.CrossCellPolicy;
        set
        {
            if (CrystalStructureView.CrossCellPolicy == value)
            {
                return;
            }

            CrystalStructureView.CrossCellPolicy = value;
            viewport.RecalculateBonds();
        }
    }

    public Dictionary<string, StructureFile> OpenFileList { get; } = new();

    public StructureFile CurrentFile { get; private set; }

    void Awake()
    {
        ReadPeriodicTable();
        Instance = this;
        viewport = GetComponent<ViewportController>();

        // We have to invert input axis on mac os
        IsRunningOnMacOS = SystemInfo.operatingSystem.Contains("Mac OS") ||
                           SystemInfo.operatingSystem.Contains("MacOS");
        Debug.Log("Running on OS: " + SystemInfo.operatingSystem);
        Debug.Log("Is Mac OS: " + IsRunningOnMacOS);
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
        // string lines = Resources.Load<TextAsset>("DefaultArc").text;
        // OpenArc(lines.Split('\n'));
        SendJSEvent("OnUnityLoad", "");
#else
        string lines = Resources.Load<TextAsset>("PeriodicTable").text;
        OpenArc(lines.Split('\n'), "PeriodicTable.arc", true);
#endif
    }

    // Elements per row in the periodic table
    public static readonly int[] ElementsPerRow =
    {
        2, 8, 8, 18, 18, 32, 32
    };

    private void ReadPeriodicTable()
    {
#if UNITY_WEBGL
        var periodicTableJson = Resources.Load<TextAsset>("Elements").text;
#else
        var periodicTableJson = File.ReadAllText(Path.Join(Application.dataPath, "Elements.json"));
#endif
        var rawElements = JsonUtility.FromJson<SerializedElementContainer>(periodicTableJson).Elements;
        Elements = rawElements.ToDictionary(e => e.Symbol, e => new ElementData(e));
    }

#if !UNITY_WEBGL
    public void ScheduleTask(Action action)
    {
        callbackQueue.Enqueue(action);
    }
#endif

#if UNITY_WEBGL
    // Write arc
    [DllImport("__Internal")]
    private static extern void SendJSEvent(string eventName, string result);

    // For javascript invocation
    public void OpenArcString(string content)
    {
        Debug.Log("OpenArcString: " + content);
        OpenArc(content.Split('\n'), $"{OpenFileList.Count}.arc");
    }

    public void SaveCurrentStructure()
    {
        try
        {
            // File name not relevant here
            StructureFile newFile = new("");
            newFile.Structures.Add(viewport.CurrentStructureView.Structure);
            string str = IFormat.GetFormatIO(".arc").Write(newFile);
            SendJSEvent("ArcFileRecv", str);
        }
        catch (Exception e)
        {
            SendJSEvent("ArcFileFail", e.Message);
        }
    }

    public void SaveAllStructures()
    {
        try
        {
            string str = IFormat.GetFormatIO(".arc").Write(CurrentFile);
            SendJSEvent("ArcFileRecv", str);
        }
        catch (Exception e)
        {
            SendJSEvent("ArcFileFail", e.Message);
        }
    }
#endif

    void AddFile(StructureFile structureFile)
    {
        if (OpenFileList.ContainsValue(structureFile) || OpenFileList.ContainsKey(structureFile.Filename))
        {
            return;
        }

        OpenFileList.Add(structureFile.Filename, structureFile);
#if UNITY_WEBGL
        ProjectPanelController.Instance.AddProjectFile(structureFile.Filename);
#else
        ScheduleTask(() => ProjectPanelController.Instance.AddProjectFile(structureFile.Filename));
#endif
    }

    public void OpenArc(IEnumerable<string> content, string filename, bool openAfterLoad = false)
    {
        IFormat formatReader = IFormat.GetFormatIO(filename);
        if (formatReader == null)
        {
            Debug.LogError($"Invalid format for file `{Path.GetFileName(filename)}`, cannot open");
            return;
        }

#if UNITY_WEBGL
        CurrentFile = new(filename);
        AddFile(CurrentFile);

        formatReader.Read(CurrentFile, content);

        if (CurrentFile.StructureCount == 0)
        {
            OverlayUiController.Instance.PrintStatusLog("Cannot open empty file");
        }
        else
        {
            OverlayUiController.Instance.PrintStatusLog(
                $"Reading {CurrentFile.Filename}: {CurrentFile.StructureCount} structures loaded");
            StructureSequenceController.Instance.ResetStructure();
        }

#else
        Task.Run(() =>
        {
            StructureFile currentFile = new(filename);
            if (openAfterLoad)
            {
                CurrentFile = currentFile;
                currentFile.IsOpenStructureQueued = true;
            }

            try
            {
                AddFile(currentFile);
                if (openAfterLoad)
                {
                    formatReader.Read(currentFile, content, _ => { currentFile.NewStructureLoadedNotifier = true; });
                }
                else
                {
                    formatReader.Read(currentFile, content);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read structure file: {e.Message}\n{e.StackTrace}");
            }
        });
#endif
    }

    // Switches to an already opened (present in project list) structure file
    public void SwitchArcFile(string filename)
    {
        if (!OpenFileList.ContainsKey(filename))
        {
            OverlayUiController.Instance.PrintStatusLog($"Requested file {filename} not found");
            return;
        }

        CurrentFile = OpenFileList[filename];
        StructureSequenceController.Instance.ResetStructure();
    }

    public void NewEmpty()
    {
        CurrentFile = new("Empty");
        CrystalStructure crystalStructure = new();
        crystalStructure.Energy = 0;
        crystalStructure.SetLattice(new[] { 10f, 10f, 10f, 90f, 90f, 90f });
        CurrentFile.Structures.Add(crystalStructure);
        StructureSequenceController.Instance.ResetStructure();
    }

    void Update()
    {
        // WebGL build is single-threaded, these multi-threading stuff isn't needed
#if !UNITY_WEBGL
        // Run queued actions
        if (callbackQueue.TryDequeue(out var action))
        {
            action();
        }

        if (CurrentFile != null && CurrentFile.NewStructureLoadedNotifier)
        {
            CurrentFile.NewStructureLoadedNotifier = false;

            CurrentFile.ListMutex.WaitOne();
            int numLoadedStructures = CurrentFile.StructureCount;
            CurrentFile.ListMutex.ReleaseMutex();

            if (CurrentFile.IsOpenStructureQueued)
            {
                if (CurrentFile.StructureCount == 0)
                {
                    OverlayUiController.Instance.PrintStatusLog("Cannot read empty file");
                }
                else
                {
                    StructureSequenceController.Instance.ResetStructure();
                    CurrentFile.IsOpenStructureQueued = false;
                }
            }
            else
            {
                OverlayUiController.Instance.UpdateStructureIndex(null, numLoadedStructures);
            }

            OverlayUiController.Instance.PrintStatusLog(
                $"Reading {CurrentFile.Filename}: {numLoadedStructures} structures loaded");
        }
#endif
    }
}