using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class StructureSequenceController : MonoBehaviour
{
    public static StructureSequenceController Instance;

    private bool isPlaying = false, isCapturingSequence = false;
    private string captureSequenceBaseDir = "", captureFilename = "";
    private Action captureSequenceCallback = null;
    private int captureStartIndex = -1, captureEndIndex = -1;
    private int captureFinishesAtFrame = int.MaxValue, saveImageAtFrame = int.MaxValue;
    private Color? backgroundColor = null;
    private float timer = 0;
    private Coroutine playCoroutine;
    private ViewportController viewport;

    private Camera mainCamera;

    private Texture2D tempTexture;

    public float PlayInterval { get; set; } = 0.1f;

    public int CurrentStructureIndex { get; private set; }

    public CrystalStructure CurrentStructure => App.Instance.CurrentFile.Structures[CurrentStructureIndex];

    public int StructureCount => App.Instance.CurrentFile.StructureCount;

    void Awake()
    {
        Instance = this;
        viewport = GetComponent<ViewportController>();
        mainCamera = Camera.main;
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

    public bool ImportMolecule(IEnumerable<string> content, string filename)
    {
        if (App.Instance.CurrentFile == null || CurrentStructure == null)
        {
            return false;
        }

        // Put molecule at lattice center
        Vector3 latticeCenter = CurrentStructure.FractionalToCartesian(new(0.5f, 0.5f, 0.5f));

        IMoleculeFormat format = IMoleculeFormat.GetFormatIO(filename);
        var atoms = format.ReadAtoms(content, latticeCenter);

        App.Instance.operationManager.SubmitOperation(new AddAtomsOperation(atoms.ToList()));
        return true;
    }

    public void SaveCurrentFrame(string filename, int width, int height, bool transparentBackground = false)
    {
        string dir = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (transparentBackground)
        {
            backgroundColor = mainCamera.backgroundColor;
            mainCamera.backgroundColor = Color.clear;
        }

        OverlayUiController.Instance.SetUIDisplay(false);

        // Reshow UI on next frame
        captureFinishesAtFrame = Time.frameCount + 1;
        captureSequenceBaseDir = Path.GetDirectoryName(filename);
        captureFilename = Path.GetFileNameWithoutExtension(filename);
        saveImageAtFrame = Time.frameCount + 1;

        OverlayUiController.Instance.PrintStatusLog(
            $"Saved screenshot to {captureSequenceBaseDir}\\{captureFilename}.png");
        RedirectCameraToRenderTexture(width, height);
    }

    // Capture screenshots of all structures in sequence
    public void SaveFrameSequence(string baseDir, int width, int height, bool transparentBackground = false,
        int startIndex = 0, int endIndex = int.MaxValue, string filenamePrefix = "", Action onComplete = null)
    {
        if (App.Instance.CurrentFile == null || CurrentStructure == null)
        {
            return;
        }

        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        // Cancel if already capturing
        if (isCapturingSequence)
        {
            return;
        }

        SwitchStructure(startIndex);

        if (transparentBackground)
        {
            backgroundColor = mainCamera.backgroundColor;
            mainCamera.backgroundColor = Color.clear;
        }

        OverlayUiController.Instance.SetUIDisplay(false);

        captureSequenceBaseDir = baseDir;
        captureStartIndex = startIndex;
        captureEndIndex = endIndex;
        captureFilename = filenamePrefix;
        captureSequenceCallback = onComplete;
        saveImageAtFrame = Time.frameCount + 1;
        isCapturingSequence = true;
        RedirectCameraToRenderTexture(width, height);
    }

    void RedirectCameraToRenderTexture(int width, int height)
    {
        RenderTexture newRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB64);
        mainCamera.targetTexture = newRT;
    }

    void SaveCameraRenderTargetToFile(string filename)
    {
        if (mainCamera.targetTexture == null)
        {
            throw new InvalidOperationException("Camera should have a non-screen render texture");
        }

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = mainCamera.targetTexture;

        if (tempTexture is null || tempTexture.width != mainCamera.targetTexture.width ||
            tempTexture.height != mainCamera.targetTexture.height)
        {
            if (tempTexture is not null)
            {
                Destroy(tempTexture);
            }

            tempTexture = new Texture2D(mainCamera.targetTexture.width, mainCamera.targetTexture.height);
        }

        tempTexture.ReadPixels(new Rect(0, 0, mainCamera.targetTexture.width, mainCamera.targetTexture.height), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = currentRT;

        var bytes = tempTexture.EncodeToPNG();

#if UNITY_WEBGL && !UNITY_EDITOR
        FilePanelController.Instance.RequestDownloadFile(bytes, Path.GetFileName(filename));
#else
        File.WriteAllBytes(filename, bytes);
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount >= saveImageAtFrame)
        {
            string filename = isCapturingSequence
                ? $"{captureFilename}{CurrentStructureIndex - captureStartIndex + 1:00000}.png"
                : $"{captureFilename}.png";
            SaveCameraRenderTargetToFile(Path.Join(captureSequenceBaseDir, filename));

            if (Time.frameCount < captureFinishesAtFrame && isCapturingSequence)
            {
                // Interrupt if finished or Esc pressed
                if (CurrentStructureIndex >= Math.Min(StructureCount - 1, captureEndIndex) ||
                    Input.GetKeyDown(KeyCode.Escape))
                {
                    OverlayUiController.Instance.PrintStatusLog(
                        $"Saved screenshot sequence to {captureSequenceBaseDir}");
                    captureFinishesAtFrame = Time.frameCount + 1;
                }
                else
                {
                    Next();
                    captureFinishesAtFrame = int.MaxValue;
                }

                saveImageAtFrame = Time.frameCount + 1;
            }
            else
            {
                saveImageAtFrame = int.MaxValue;
            }
        }

        if (Time.frameCount >= captureFinishesAtFrame)
        {
            if (mainCamera.targetTexture != null)
            {
                mainCamera.targetTexture = null;
            }

            if (tempTexture is not null)
            {
                Destroy(tempTexture);
                tempTexture = null;
            }

            if (backgroundColor != null)
            {
                mainCamera.backgroundColor = backgroundColor.Value;
                backgroundColor = null;
            }

            if (isCapturingSequence)
            {
                isCapturingSequence = false;
                captureSequenceCallback?.Invoke();
            }

            OverlayUiController.Instance.SetUIDisplay(true);
            // Cancel reshow
            captureFinishesAtFrame = int.MaxValue;
        }
    }
}