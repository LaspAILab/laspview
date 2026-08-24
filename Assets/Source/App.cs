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

[Serializable]
public struct SerializedColorData
{
    public string FixedColor;
    public string ReactColor;
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

    // LRU timestamp for files
    private int fileLruTimestamp = 0;

    void Awake()
    {
        UnityEngine.Analytics.Analytics.enabled = false;
        UnityEngine.Analytics.Analytics.deviceStatsEnabled = false;
        UnityEngine.Analytics.Analytics.initializeOnStartup = false;
        UnityEngine.Analytics.Analytics.limitUserTracking = false;
        UnityEngine.Analytics.PerformanceReporting.enabled = false;

        ReadJsonConfig();
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
        SendJSEvent("OnUnityLoad", "");
#else
        string lines = Resources.Load<TextAsset>("PeriodicTable").text;
        OpenArc(lines.Split('\n'), "PeriodicTable.arc", false, true);
#endif
    }

    private void ReadJsonConfig()
    {
#if UNITY_WEBGL
        var periodicTableJson = Resources.Load<TextAsset>("Elements").text;
        var colorConfigJson = Resources.Load<TextAsset>("Colors").text;
#else
        var periodicTableJson = File.ReadAllText(Path.Join(Application.dataPath, "Elements.json"));
        var colorConfigJson = File.ReadAllText(Path.Join(Application.dataPath, "Colors.json"));
#endif
        var rawElements = JsonUtility.FromJson<SerializedElementContainer>(periodicTableJson).Elements;
        Elements = rawElements.ToDictionary(e => e.Symbol, e => new ElementData(e));

        var colorConfig = JsonUtility.FromJson<SerializedColorData>(colorConfigJson);
        ColorUtility.TryParseHtmlString(colorConfig.FixedColor, out StructureRenderer.FixedRimColor);
        ColorUtility.TryParseHtmlString(colorConfig.ReactColor, out StructureRenderer.ReactRimColor);
    }

#if UNITY_WEBGL
    public void ScheduleTask(Action action)
    {
        action();
    }
#else
    public void ScheduleTask(Action action)
    {
        callbackQueue.Enqueue(action);
    }
#endif

#if UNITY_WEBGL
    // Write arc
    [DllImport("__Internal")]
    private static extern void SendJSEvent(string eventName, string result);

    private int arcStringInputIndex = 0;

    // For JavaScript invocation
    public void OpenArcString(string content)
    {
#if DEBUG
        Debug.Log("OpenArcString: " + content);
#endif
        OpenArc(content.Split('\n'), $"webin-{++arcStringInputIndex}.arc", false);
    }

    public void SaveCurrentStructure()
    {
        try
        {
            // File name not relevant here
            StructureFile newFile = new();
            newFile.Structures.Add(viewport.CurrentStructureView.Structure);
            string str = ICrystalFormat.GetFormatIO(".arc").Write(newFile);
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
            string str = ICrystalFormat.GetFormatIO(".arc").Write(CurrentFile);
            SendJSEvent("ArcFileRecv", str);
        }
        catch (Exception e)
        {
            SendJSEvent("ArcFileFail", e.Message);
        }
    }

    public void GetCurrentFileName()
    {
        try
        {
            string str = "";
            if (CurrentFile.HasPhysicalFile)
            {
                str = CurrentFile.Filename;
            }
            SendJSEvent("FileNameRecv", str);
        }
        catch (Exception e)
        {
            SendJSEvent("FileNameFail", e.Message);
        }
    }

    // Limit the open file count on WebGL
    private const int MAX_OPEN_FILE_COUNT = 4;
#endif

    void AddFile(StructureFile structureFile)
    {
        if (OpenFileList.ContainsValue(structureFile) || OpenFileList.ContainsKey(structureFile.Filename))
        {
            OpenFileList[structureFile.Filename] = structureFile;
            OpenFileList[structureFile.Filename].LruTimestamp = fileLruTimestamp++;
            return;
        }

        OpenFileList.Add(structureFile.Filename, structureFile);
#if UNITY_WEBGL
        ProjectPanelController.Instance.RegenerateProjectList();
#else
        ScheduleTask(() => ProjectPanelController.Instance.RegenerateProjectList());
#endif
    }

    public void OpenArc(IEnumerable<string> content, string filename, bool hasPhysicalFile, bool openAfterLoad = true)
    {
        ICrystalFormat formatReader = ICrystalFormat.GetFormatIO(filename);
        if (formatReader == null)
        {
            Debug.LogError($"Invalid format for file `{Path.GetFileName(filename)}`, cannot open");
            return;
        }

#if UNITY_WEBGL
        CurrentFile = new(filename, hasPhysicalFile, fileLruTimestamp++);
        AddFile(CurrentFile);

        try
        {
            formatReader.Read(CurrentFile, content);
        }
        catch (Exception e)
        {
            OverlayUiController.Instance.PrintStatusLog($"Read failed: {e.Message}");
            Debug.LogError($"Failed to read structure file: {e.Message}\n{e.StackTrace}");
        }

        if (openAfterLoad && CurrentFile.StructureCount > 0)
        {
            OverlayUiController.Instance.PrintStatusLog(
                $"Displaying file `{CurrentFile.Filename}` ({CurrentFile.StructureCount} items)");
            StructureSequenceController.Instance.ResetStructure();
        }

        if (OpenFileList.Count > MAX_OPEN_FILE_COUNT)
        {
            // Remove the least recently used file
            var minTimestamp = OpenFileList.Values.Min(f => f.LruTimestamp);
            var leastUsedFileKey = OpenFileList
                .FirstOrDefault(e => e.Value.LruTimestamp == minTimestamp).Key;
            OpenFileList.Remove(leastUsedFileKey);
            ProjectPanelController.Instance.RegenerateProjectList();
            OverlayUiController.Instance.PrintStatusLog($"Closed file `{leastUsedFileKey}`");
        }

#else
        OverlayUiController.Instance.PrintStatusLog($"Loading `{filename}` in background");
        Task.Run(() =>
        {
            StructureFile currentFile = new(filename, fileLruTimestamp++);
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
                OverlayUiController.Instance.PrintStatusLog($"Read failed: {e.Message}");
                Debug.LogError($"Failed to read structure file: {e.Message}\n{e.StackTrace}");
            }

            callbackQueue.Enqueue(()=>
            {
                OverlayUiController.Instance.PrintStatusLog($"Displaying file `{filename}` ({currentFile.StructureCount} items)");
            });
        });
#endif
    }

    // Switches to an already opened (present in project list) structure file
    public void SwitchArcFile(string fileKey)
    {
        if (!OpenFileList.ContainsKey(fileKey))
        {
            OverlayUiController.Instance.PrintStatusLog($"Requested file `{fileKey}` not found");
            return;
        }

        CurrentFile = OpenFileList[fileKey];
        CurrentFile.LruTimestamp = fileLruTimestamp++;
        StructureSequenceController.Instance.ResetStructure();
        OverlayUiController.Instance.PrintStatusLog($"Displaying file `{fileKey}`");
    }

    public void CloseOpenFile(string fileKey)
    {
        if (!OpenFileList.ContainsKey(fileKey))
        {
            OverlayUiController.Instance.PrintStatusLog($"Requested file `{fileKey}` not found");
            return;
        }

        if (OpenFileList.Count == 1)
        {
            OverlayUiController.Instance.PrintStatusLog($"Cannot close last file");
            return;
        }

        if (OpenFileList[fileKey] == CurrentFile)
        {
            // Find first file other than the current one
            var newFile = OpenFileList.First(x => x.Key != fileKey);
            SwitchArcFile(newFile.Key);
        }

        OpenFileList.Remove(fileKey);
        ProjectPanelController.Instance.RegenerateProjectList();
    }

    public void NewEmpty()
    {
        CurrentFile = new("Empty", false, fileLruTimestamp++);
        CrystalStructure crystalStructure = new();
        crystalStructure.Energy = 0;
        crystalStructure.SetLattice(new[] { 10f, 10f, 10f, 90f, 90f, 90f });
        CurrentFile.Structures.Add(crystalStructure);
        StructureSequenceController.Instance.ResetStructure();
        OverlayUiController.Instance.PrintStatusLog("New file created");
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
        }
#endif
    }
}