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

    public static Dictionary<string, ElementData> Elements;

#if !UNITY_WEBGL
    // Queue for scheduling tasks on main thread
    private readonly ConcurrentQueue<Action> callbackQueue = new();
    
    private bool loadedStructuresDirty = false, firstStructureLoaded = false;
#endif

    public ViewportController Viewport;

    private float bondTolerance = 1.26f;

    public float BondTolerance
    {
        get => bondTolerance;
        set
        {
            if (Mathf.Approximately(bondTolerance, value))
            {
                return;
            }

            bondTolerance = value;
            Viewport.CurrentStructureView.BondTolerance = bondTolerance;
            Viewport.RecalculateBonds();
        }
    }

    public Dictionary<string, ArcFile> OpenFileList { get; } = new ();
    
    public ArcFile CurrentFile { get; private set; }

    public int CurrentStructureIndex { get; private set; }

    void Awake()
    {
        ReadPeriodicTable();
        Instance = this;
        Viewport = Camera.main.GetComponent<ViewportController>();
    }

    private void Start()
    {
#if !UNITY_WEBGL
        string filename = Path.Join(Application.dataPath, ConFun.GlobalConfig["arc_default"]);
        if (File.Exists(filename))
        {
            OpenArcFile(filename);
        }
        else
        {
            string lines = Resources.Load<TextAsset>("DefaultArc").text;
            OpenArc(lines.Split('\n'));
        }
#else
        // string lines = Resources.Load<TextAsset>("DefaultArc").text;
        // OpenArc(lines.Split('\n'));
#endif
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
    private static extern void SendResultToJS(int errCode, string result);

    // For javascript invocation
    public void OpenArcString(string content)
    {
        Debug.Log("OpenArcString: " + content);
        OpenArc(content.Split('\n'), OpenFileList.Count.ToString());
    }

    public void SaveCurrentStructure()
    {
        int errCode = 0;
        string str;
        try
        {
            ArcFile newArcFile = new();
            newArcFile.Structures.Add(Viewport.CurrentStructureView.Structure);
            str = newArcFile.WriteArc();
        }
        catch (Exception e)
        {
            str = e.Message;
            errCode = -1;
        }
        
        SendResultToJS(errCode, str);
    }

    public void SaveAllStructures()
    {
        int errCode = 0;
        string str;
        try
        {
            str = CurrentFile.WriteArc();
        }
        catch (Exception e)
        {
            str = e.Message;
            errCode = -1;
        }
        SendResultToJS(errCode, str);
    }
#endif

    public void SwitchStructure(int index)
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
            return;
        }

        CurrentStructureIndex = index;
        ArcStructure currentStructure = CurrentFile.Structures[index];
#if !UNITY_WEBGL
        CurrentFile.ListMutex.ReleaseMutex();
#endif

#if UNITY_WEBGL
        ArcStructureView structureView = new(currentStructure, bondTolerance);
        Viewport.OpenStructureView(structureView);
        ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount, currentStructure.Energy);
#else
        Task.Run(() =>
        {
            ArcStructureView structureView = new(currentStructure, bondTolerance);
            ScheduleTask(() =>
            {
                Viewport.OpenStructureView(structureView);
                ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount, currentStructure.Energy);
            });
        });
#endif
    }

    public void AddFile(ArcFile arc, string filename)
    {
        if (OpenFileList.ContainsValue(arc) || OpenFileList.ContainsKey(filename))
        {
            return;
        }
        
        OpenFileList.Add(filename, arc);
        L_ProjectPanelSrc.S.ProjectListAddCell(filename);
    }

    public void OpenArc(IEnumerable<string> content, string filename)
    {
#if UNITY_WEBGL
        CurrentFile = new();
        AddFile(CurrentFile, filename);
        
        bool readFinished = CurrentFile.ReadArc(content);
        if (!readFinished)
        {
            Debug.Log("File too large, truncated");
        }

        CurrentStructureIndex = 0;

        if (CurrentFile.StructureCount > 0)
        {
            ConFun.UpdateStatusLabel($"Reading arc file: {CurrentFile.StructureCount} structures loaded");
            ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount,
                CurrentFile.Structures[CurrentStructureIndex].Energy);

            ArcStructureView structureView =
                new(CurrentFile.Structures[CurrentStructureIndex], bondTolerance);
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
            CurrentFile = new();
            AddFile(CurrentFile, filename);
            CurrentFile.ReadArc(content, _ => { loadedStructuresDirty = true; });
        });
#endif
    }

    public void SetArcFile(string filename)
    {
        if (!OpenFileList.ContainsKey(filename))
        {
            ConFun.UpdateStatusLabel("Requested file {filename} not found");
            return;
        }
        
        CurrentFile = OpenFileList[filename];
        CurrentStructureIndex = 0;
        
        ArcStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex], bondTolerance);
        Viewport.OpenStructureView(structureView);
        ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount,
            CurrentFile.Structures[CurrentStructureIndex].Energy);
        Viewport.RefocusCamera();
    }

    public void NewEmpty()
    {
        CurrentFile = new();
        ArcStructure arcStructure = new();
        arcStructure.Energy = 0;
        arcStructure.SetLattice(new[] { 10f, 10f, 10f, 90f, 90f, 90f });
        CurrentFile.Structures.Add(arcStructure);

        if (CurrentFile.StructureCount > 0)
        {
            ArcStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex], bondTolerance);
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
            ArcStructure currentStructure = CurrentFile.Structures[CurrentStructureIndex];
            CurrentFile.ListMutex.ReleaseMutex();

            if (!firstStructureLoaded)
            {
                CurrentStructureIndex = 0;

                ArcStructureView structureView =
                    new(currentStructure, bondTolerance);
                Viewport.OpenStructureView(structureView);
                Viewport.RefocusCamera();
                firstStructureLoaded = true;
            }

            ConFun.UpdateStatusLabel($"Reading arc file: {numLoadedStructures} structures loaded");
            ConFun.UpdateStructureInfo(CurrentStructureIndex, numLoadedStructures,
                currentStructure.Energy);
        }
#endif
    }
}