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

    public static bool IsRunningOnMacOS = false;

    public static Dictionary<string, ElementData> Elements;

#if !UNITY_WEBGL
    // Queue for scheduling tasks on the main thread
    private readonly ConcurrentQueue<Action> callbackQueue = new();

    private bool loadedStructuresDirty = false, firstStructureLoaded = false;
#endif

    public ViewportController Viewport;

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
            Viewport.RecalculateBonds();
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
            Viewport.RecalculateBonds();
        }
    }

    public Dictionary<string, StructureFile> OpenFileList { get; } = new();

    public StructureFile CurrentFile { get; private set; }

    public int CurrentStructureIndex { get; private set; }

    void Awake()
    {
        ReadPeriodicTable();
        Instance = this;
        Viewport = GetComponent<ViewportController>();

        // We have to invert input axis on mac os
        IsRunningOnMacOS = SystemInfo.operatingSystem.Contains("Mac OS") || SystemInfo.operatingSystem.Contains("MacOS");
        Debug.Log("Running on OS: " + SystemInfo.operatingSystem);
        Debug.Log("Is Mac OS: "+IsRunningOnMacOS);
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
        // string lines = Resources.Load<TextAsset>("DefaultArc").text;
        // OpenArc(lines.Split('\n'));
        SendJSEvent("OnUnityLoad", "");
#else
        bool hasConfigFileName = ConFun.GlobalConfig.TryGetValue("arc_default", out string filename);
        if (hasConfigFileName && File.Exists(Path.Join(Application.dataPath, filename)))
        {
            OpenArc(File.ReadAllLines(Path.Join(Application.dataPath, filename)), "Default.arc", true);
        }
        else
        {
            string lines = Resources.Load<TextAsset>("DefaultArc").text;
            OpenArc(lines.Split('\n'), "Default.arc", true);
        }
#endif
    }

    // Elements per row in the periodic table
    public static readonly int[] ElementsPerRow =
    {
        2, 8, 8, 18, 18, 32, 32
    };

    // Sort elements by common valance, used for printing chemical formula
    public static (int, int) GetElementPeriodAndValance(string elem)
    {
        int elementNumber = Elements[elem].Number;

        int index = 0, period = 1;
        while (elementNumber > ElementsPerRow[index])
        {
            elementNumber -= ElementsPerRow[index++];
            period++;
        }

        int valance = elementNumber <= ElementsPerRow[period - 1] - 4 ? elementNumber : (elementNumber - 8);
        return (period, valance);
    }

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
            StructureFile newFile = new();
            newFile.Structures.Add(Viewport.CurrentStructureView.Structure);
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

    public bool SwitchStructure(int index)
    {
#if !UNITY_WEBGL
        CurrentFile.ListMutex.WaitOne();
#endif
        index = Mathf.Clamp(index, 0, CurrentFile.StructureCount - 1);
        if (index == CurrentStructureIndex)
        {
#if !UNITY_WEBGL
            CurrentFile.ListMutex.ReleaseMutex();
#endif
            return false;
        }

        CurrentStructureIndex = index;
        CrystalStructure currentStructure = CurrentFile.Structures[index];
#if !UNITY_WEBGL
        CurrentFile.ListMutex.ReleaseMutex();
#endif

#if UNITY_WEBGL
        CrystalStructureView structureView = new(currentStructure);
        Viewport.OpenStructureView(structureView);
        ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount, currentStructure.Energy);
#else
        Task.Run(() =>
        {
            CrystalStructureView structureView = new(currentStructure);
            ScheduleTask(() =>
            {
                Viewport.OpenStructureView(structureView);
                ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount, currentStructure.Energy);
            });
        });
#endif

        return true;
    }

    public void AddFile(StructureFile structureFile, string filename)
    {
        if (OpenFileList.ContainsValue(structureFile) || OpenFileList.ContainsKey(filename))
        {
            return;
        }

        OpenFileList.Add(filename, structureFile);
#if UNITY_WEBGL
        ProjectPanelController.Instance.AddProjectFile(filename);
#else
        ScheduleTask(() => ProjectPanelController.Instance.AddProjectFile(filename));
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
        CurrentFile = new();
        AddFile(CurrentFile, filename);

        formatReader.Read(CurrentFile, content);

        CurrentStructureIndex = 0;
        if (CurrentFile.StructureCount > 0)
        {
            ConFun.UpdateStatusLabel($"Reading arc file: {CurrentFile.StructureCount} structures loaded");
            ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount,
                CurrentFile.Structures[CurrentStructureIndex].Energy);

            CrystalStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex]);
            Viewport.OpenStructureView(structureView);
            Viewport.RefocusCamera();
        }
        else
        {
            ConFun.UpdateStatusLabel($"Cannot open empty file");
        }

#else
        firstStructureLoaded = false;
        Task.Run(() =>
        {
            StructureFile currentFile = new();
            if (openAfterLoad)
            {
                CurrentFile = currentFile;
            }
            try
            {
                AddFile(currentFile, filename);
                if (openAfterLoad)
                {
                    formatReader.Read(currentFile, content, _ => { loadedStructuresDirty = true; });
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
            ConFun.UpdateStatusLabel("Requested file {filename} not found");
            return;
        }

        CurrentFile = OpenFileList[filename];
        CurrentStructureIndex = 0;

        CrystalStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex]);
        Viewport.OpenStructureView(structureView);
        ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount,
            CurrentFile.Structures[CurrentStructureIndex].Energy);
        Viewport.RefocusCamera();
    }

    public void NewEmpty()
    {
        CurrentFile = new();
        CrystalStructure crystalStructure = new();
        crystalStructure.Energy = 0;
        crystalStructure.SetLattice(new[] { 10f, 10f, 10f, 90f, 90f, 90f });
        CurrentFile.Structures.Add(crystalStructure);

        if (CurrentFile.StructureCount > 0)
        {
            CrystalStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex]);
            Viewport.OpenStructureView(structureView);
            ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount,
                CurrentFile.Structures[CurrentStructureIndex].Energy);
        }
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

        if (loadedStructuresDirty)
        {
            loadedStructuresDirty = false;

            CurrentFile.ListMutex.WaitOne();
            int numLoadedStructures = CurrentFile.StructureCount;
            CrystalStructure currentStructure = CurrentFile.Structures[CurrentStructureIndex];
            CurrentFile.ListMutex.ReleaseMutex();

            if (!firstStructureLoaded)
            {
                CurrentStructureIndex = 0;

                CrystalStructureView structureView = new(currentStructure);
                Viewport.OpenStructureView(structureView);
                Viewport.RefocusCamera();
                firstStructureLoaded = true;
                ConFun.UpdateStructureInfo(CurrentStructureIndex, numLoadedStructures,
                    currentStructure.Energy);
            }
            else
            {
                ConFun.UpdateStructureInfo(null, numLoadedStructures, currentStructure.Energy);
            }

            OverlayUiController.Instance.PrintStatusLog($"Reading arc file: {numLoadedStructures} structures loaded");
        }
#endif
    }
}