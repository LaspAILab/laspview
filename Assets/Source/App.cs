
// Unity cannot serialize raw json arrays, so we wrap it with a class as a workaround

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    // Queue for scheduling tasks on main thread
    private readonly ConcurrentQueue<Action> callbackQueue = new();
    
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

    public ArcFile CurrentFile
    {
        get;
        private set;
    }

    public int CurrentStructureIndex
    {
        get;
        private set;
    }

    void Awake()
    {
        ReadPeriodicTable();
        Instance = this;
        Viewport = Camera.main.GetComponent<ViewportController>();
    }

    private void Start()
    {
#if !UNITY_WEBGL
        string filename = ConFun.GlobalConfig["arc_default"];
        OpenArcFile(Path.Join(Application.dataPath, filename));
#else
        string lines = 
@"!BIOSYM archive 2 
PBC = ON
     Energy           10          10  -129.895506
!DATE
PBC    7.6679    7.6679   15.0000   90.0000   90.0000  120.0000
H       0.008413248     4.455233640     8.566392459 CORE    1  H H   0.0000    1
H       6.257571388     4.243667943     6.666642087 CORE    2  H H   0.0000    2
H       3.815253288     0.753089160     5.124217983 CORE    3  H H   0.0000    3
H       1.342005797     4.028462789     7.451793384 CORE    4  H H   0.0000    4
H       0.086574563     2.814818537     7.852299561 CORE    5  H H   0.0000    5
H       2.572615996     5.887984031     5.112457470 CORE    6  H H   0.0000    6
C       0.279671901     3.881257765     7.667971729 CORE    7  C C   0.0000    7
O      -0.454394321     4.353730708     6.513752863 CORE    8  O O   0.0000    8
end
end
";
        OpenArc(lines.Split('\n'));
#endif
    }

    private void ReadPeriodicTable()
    {
        var periodicTableJson = Resources.Load<TextAsset>("Elements");
        var rawElements = JsonUtility.FromJson<SerializedElementContainer>(periodicTableJson.text).Elements;
        Elements = rawElements.ToDictionary(e => e.Symbol, e => new ElementData(e));
    }

    public void ScheduleTask(Action action)
    {
        callbackQueue.Enqueue(action);
    }

    public void OpenArcFile(string filename)
    {
        OpenArc(File.ReadLines(filename));
    }
    
    public void SwitchStructure(int index)
    {
        index = Mathf.Clamp(index, 0, CurrentFile.StructureCount - 1);
        if (index == CurrentStructureIndex)
        {
            return;
        }
        CurrentStructureIndex  = index;

        Task.Run(() =>
        {
            ArcStructureView structureView = new(CurrentFile.Structures[index], bondTolerance);
            ScheduleTask(() =>
            {
                Viewport.OpenStructureView(structureView);
                ConFun.UpdateStructureInfo(index, CurrentFile.StructureCount, CurrentFile.Structures[index].Energy);
            });
        });
    }
    
    public void OpenArc(IEnumerable<string> content)
    {
        bool firstStructureLoaded = false;
        Task.Run(() =>
        {
            CurrentFile = new();
            CurrentFile.ReadArc(content, count =>
            {
                if (!firstStructureLoaded)
                {
                    
                }
                ScheduleTask(() =>
                {
                    ConFun.UpdateStatusLabel($"Reading arc file: {count} structures loaded");

                    if (!firstStructureLoaded)
                    {
                        CurrentStructureIndex = 0;
                        ArcStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex], bondTolerance);
                        Viewport.OpenStructureView(structureView);
                        firstStructureLoaded = true;
                    }

                    ConFun.UpdateStructureInfo(CurrentStructureIndex, count,
                        CurrentFile.Structures[CurrentStructureIndex].Energy);
                });
            });
        });
    }

    public void NewEmpty()
    {
        CurrentFile = new();
        ArcStructure arcStructure = new();
        arcStructure.Energy = 0;
        arcStructure.SetLattice(new []{10f, 10f, 10f, 90f, 90f, 90f});
        CurrentFile.Structures.Add(arcStructure);

        if (CurrentFile.StructureCount > 0)
        {
            ArcStructureView structureView = new(CurrentFile.Structures[CurrentStructureIndex], bondTolerance);
            Viewport.OpenStructureView(structureView);
            ConFun.UpdateStructureInfo(CurrentStructureIndex, CurrentFile.StructureCount, CurrentFile.Structures[CurrentStructureIndex].Energy);
        }
    }
    
    void Update()
    {
        // Run queued actions
        if (callbackQueue.TryDequeue(out var action))
        {
            action();
        }
    }
}