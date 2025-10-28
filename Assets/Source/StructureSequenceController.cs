using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class StructureSequenceController : MonoBehaviour
{
    public static StructureSequenceController Instance;

    private int reshowUiOnFrame = -1;
    private bool isPlaying = false, isCapturingSequence = false;
    private string captureSequenceBaseDir = "", captureSequencePrefix = "";
    private Action captureSequenceCallback = null;
    private int captureStartIndex = -1, captureEndIndex = -1;
    private float timer = 0;
    private Coroutine playCoroutine;
    private ViewportController viewport;

    public float PlayInterval
    {
        get;
        set;
    } = 0.1f;

    public int CurrentStructureIndex
    {
        get;
        private set;
    }
    
    public CrystalStructure CurrentStructure => App.Instance.CurrentFile.Structures[CurrentStructureIndex];
    
    public int StructureCount => App.Instance.CurrentFile.StructureCount;
    
    void Awake()
    {
        Instance = this;
        viewport = GetComponent<ViewportController>();
    }

    // Reset active structure to the first and refocus the camera, call this when file changes
    public void ResetStructure()
    {
        CurrentStructureIndex = 0;
        var currentFile = App.Instance.CurrentFile;
        CrystalStructureView structureView = new(currentFile.Structures[CurrentStructureIndex]);
        OverlayUiController.Instance.UpdateStructureIndex(0, currentFile.StructureCount);
        App.Instance.operationManager = new(structureView);
        viewport.OpenStructureView(structureView);
        viewport.RefocusCamera();
    }
    
    public bool SwitchStructure(int index, bool forceReload = false)
    {
        var currentFile = App.Instance.CurrentFile;
        if (currentFile == null)
        {
            return false;
        }

        if (!forceReload)
        {
#if !UNITY_WEBGL
            currentFile.ListMutex.WaitOne();
#endif
            index = Mathf.Clamp(index, 0, currentFile.StructureCount - 1);
            if (index == CurrentStructureIndex)
            {
#if !UNITY_WEBGL
                currentFile.ListMutex.ReleaseMutex();
#endif
                return false;
            }
        }

        CurrentStructureIndex = index;
        CrystalStructure currentStructure = currentFile.Structures[index];
#if !UNITY_WEBGL
        currentFile.ListMutex.ReleaseMutex();
#endif

#if UNITY_WEBGL
        CrystalStructureView structureView = new(currentStructure);
        OverlayUiController.Instance.UpdateStructureIndex(CurrentStructureIndex, currentFile.StructureCount);
        App.Instance.operationManager = new(structureView);
        viewport.OpenStructureView(structureView);
#else
        Task.Run(() =>
        {
            CrystalStructureView structureView = new(currentStructure);
            App.Instance.ScheduleTask(() =>
            {
                OverlayUiController.Instance.UpdateStructureIndex(CurrentStructureIndex, currentFile.StructureCount);
                App.Instance.operationManager = new(structureView);
                viewport.OpenStructureView(structureView);
            });
        });
#endif

        return true;
    }

    public bool Next()
    {
        return SwitchStructure(CurrentStructureIndex + 1);
    }
    
    public bool Previous()
    {
        return SwitchStructure(CurrentStructureIndex - 1);
    }

    public bool Play()
    {
        if (isPlaying)
        {
            return false;
        }
        
        playCoroutine = StartCoroutine(PlayStructure());
        isPlaying = true;
        return true;
    }

    public bool Stop()
    {
        if (!isPlaying)
        {
            return false;
        }
        
        StopCoroutine(playCoroutine);
        isPlaying = false;
        return true;
    }
    
    IEnumerator<object> PlayStructure()
    {
        while (true)
        {
            timer += Time.deltaTime;
            yield return null;
            if (timer >= PlayInterval)
            {
                if (CurrentStructureIndex == StructureCount - 1)
                {
                    SwitchStructure(0);
                }
                else
                {
                    SwitchStructure(CurrentStructureIndex + 1);
                }

                timer = 0;
            }
        }
    }

    public void SaveCurrentFrame(string filename)
    {
        string dir = Path.GetDirectoryName(filename);
        if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        OverlayUiController.Instance.SetUIDisplay(false);
        ScreenCapture.CaptureScreenshot(filename);
        OverlayUiController.Instance.PrintStatusLog($"Saved screenshot to {filename}");
        // Reshow UI on next frame
        reshowUiOnFrame = Time.frameCount + 1;
    }
    
    // Capture screenshots of all structures in sequence
    public void SaveFrameSequence(string baseDir, int startIndex = 0, int endIndex = int.MaxValue, string filenamePrefix = "", Action onComplete = null)
    {
        if(App.Instance.CurrentFile == null || CurrentStructure == null)
        {
            return;
        }
        
        if(!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }
        
        // Cancel if already capturing
        if (isCapturingSequence)
        {
            return;
        }

        SwitchStructure(startIndex);
        OverlayUiController.Instance.SetUIDisplay(false);
        captureSequenceBaseDir = baseDir;
        captureStartIndex = startIndex;
        captureEndIndex = endIndex;
        captureSequencePrefix = filenamePrefix;
        captureSequenceCallback = onComplete;
        isCapturingSequence = true;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount >= reshowUiOnFrame)
        {
            OverlayUiController.Instance.SetUIDisplay(true);
            // Cancel reshow
            reshowUiOnFrame = Int32.MaxValue;
        }

        if (isCapturingSequence)
        {
            ScreenCapture.CaptureScreenshot(Path.Join(captureSequenceBaseDir, $"{captureSequencePrefix}{CurrentStructureIndex - captureStartIndex + 1:00000}.png"));
            
            // Interrupt if finished or Esc pressed
            if (CurrentStructureIndex >= Math.Min(StructureCount - 1, captureEndIndex) || Input.GetKeyDown(KeyCode.Escape))
            {
                isCapturingSequence = false;
                OverlayUiController.Instance.PrintStatusLog($"Saved screenshot sequence to {captureSequenceBaseDir}");
                reshowUiOnFrame = Time.frameCount + 1;
                captureSequenceCallback?.Invoke();
            }
            else
            {
                Next();
            }
        }
    }
}
