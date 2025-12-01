using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SFB;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

public class UtilsPanelController : MonoBehaviour
{
    public static UtilsPanelController Instance;

    VisualElement utilsPanelRoot;

    private Button startServerButton;
    private Button runOptimizationButton;

    private Label serverStatusLabel;
    private Toggle realtimeEnergyToggle;
    private Toggle optimizeLatticeToggle;
    private Toggle overrideStructureToggle;

    private Button captureScreenshotButton;
    private MinMaxSlider frameRangeSlider;
    private Label frameRangeLabel;
    private Toggle encodeVideoToggle;
    private Button captureSeriesButton;

    private Process ffmpegProcess;

    void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        utilsPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("LaspPanel");

        startServerButton = utilsPanelRoot.Q<Button>("StartServerButton");
        startServerButton.RegisterCallback<ClickEvent>(OnStartServerClicked);
        runOptimizationButton = utilsPanelRoot.Q<Button>("RunOptimizationButton");
        runOptimizationButton.RegisterCallback<ClickEvent>(OnRunOptimizationClicked);

        serverStatusLabel = utilsPanelRoot.Q<Label>("ServerStatusLabel");
        realtimeEnergyToggle = utilsPanelRoot.Q<Toggle>("RealtimeEnergyToggle");
        optimizeLatticeToggle = utilsPanelRoot.Q<Toggle>("OptimizeLatticeToggle");
        overrideStructureToggle = utilsPanelRoot.Q<Toggle>("OverrideStructureToggle");

        captureScreenshotButton = utilsPanelRoot.Q<Button>("CaptureScreenshotButton");
        captureScreenshotButton.RegisterCallback<ClickEvent>(OnCaptureScreenshotClicked);
        frameRangeSlider = utilsPanelRoot.Q<MinMaxSlider>("FrameRangeSlider");
        frameRangeSlider.RegisterValueChangedCallback(OnFrameRangeSliderChanged);
        frameRangeLabel = utilsPanelRoot.Q<Label>("FrameRangeLabel");
        encodeVideoToggle = utilsPanelRoot.Q<Toggle>("EncodeVideoToggle");
        captureSeriesButton = utilsPanelRoot.Q<Button>("CaptureSeriesButton");
        captureSeriesButton.RegisterCallback<ClickEvent>(OnCaptureSeriesClicked);

        LaspServer.Instance.OnServerStatusChangedEvent += OnServerStatusChanged;
        LaspServer.Instance.OnEnergyReceivedEvent += EnergyCalculationCallback;
        LaspServer.Instance.OnOptimizeReceivedEvent += OptimizeCalculationCallback;
    }

    string ServerStatusToText(ServerStatusType status)
    {
        return status switch
        {
            ServerStatusType.Unstarted => "Offline",
            ServerStatusType.Idle => "Online",
            ServerStatusType.WaitingForStartup => "Starting...",
            ServerStatusType.WaitingForResponse => "Busy...",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    void OnServerStatusChanged(ServerStatusType status)
    {
        string statusText = ServerStatusToText(status);
        serverStatusLabel.text = $"Server Status: <b>{statusText}</b>";

        if (LaspServer.Instance.ServerStatus == ServerStatusType.Unstarted)
        {
            startServerButton.text = "Start server";
        }
        else
        {
            startServerButton.text = "Shutdown server";
        }
    }

    void OnStartServerClicked(ClickEvent evt)
    {
        if (LaspServer.Instance.ServerStatus == ServerStatusType.Unstarted)
        {
            LaspServer.Instance.StartServer(5009);
        }
        else
        {
            LaspServer.Instance.ShutdownServer();
        }
    }

    public void OnOperationPerformed(IOperation op)
    {
        if (realtimeEnergyToggle.value)
        {
            LaspServer.Instance.ScheduleCalculation(new CalculationRequest
            {
                type = CalculationType.Energy,
                structureFile = new(App.Instance.CurrentFile),
                structureIndex = StructureSequenceController.Instance.CurrentStructureIndex,
                filedTime = Time.time
            });

            OverlayUiController.Instance.UpdateStructureInfo(StructureSequenceController.Instance.CurrentStructure,
                true);
        }
    }

    void OnRunOptimizationClicked(ClickEvent evt)
    {
        CalculationType type = optimizeLatticeToggle.value
            ? CalculationType.OptimizeWithCell
            : CalculationType.OptimizeNoCell;

        LaspServer.Instance.ScheduleCalculation(new CalculationRequest
        {
            type = type,
            structureFile = new(App.Instance.CurrentFile),
            structureIndex = StructureSequenceController.Instance.CurrentStructureIndex,
            filedTime = Time.time
        });
    }

    void EnergyCalculationCallback(CalculationRequest energyRequest, float newEnergy)
    {
        if (energyRequest.structureFile.TryGetTarget(out var file))
        {
            file.Structures[energyRequest.structureIndex].Energy = newEnergy;

            // Update UI display if this is the current structure
            if (App.Instance.CurrentFile == file &&
                StructureSequenceController.Instance.CurrentStructureIndex == energyRequest.structureIndex)
            {
                OverlayUiController.Instance.UpdateStructureInfo(StructureSequenceController.Instance.CurrentStructure);
            }
        }
    }

    void OptimizeCalculationCallback(CalculationRequest optimizeRequest, CrystalStructure newStructure)
    {
        if (optimizeRequest.structureFile.TryGetTarget(out var file))
        {
            if (overrideStructureToggle.value)
            {
                file.Structures[optimizeRequest.structureIndex] = newStructure;
                OverlayUiController.Instance.PrintStatusLog("Optimization result received. ");

                // Reload structure if needed
                if (App.Instance.CurrentFile == file &&
                    StructureSequenceController.Instance.CurrentStructureIndex == optimizeRequest.structureIndex)
                {
                    StructureSequenceController.Instance.SwitchStructure(optimizeRequest.structureIndex, true);
                }
            }
            else
            {
                file.Structures.Add(newStructure);
                OverlayUiController.Instance.PrintStatusLog("Optimization result received. ");

                if (App.Instance.CurrentFile == file)
                {
                    OverlayUiController.Instance.UpdateStructureIndex(null, file.StructureCount);
                }
            }
        }
    }

    public void UpdateFrameRange(int frameCount)
    {
        frameRangeSlider.lowLimit = 1;
        frameRangeSlider.highLimit = frameCount;
        frameRangeSlider.value = new(frameRangeSlider.lowLimit, frameRangeSlider.highLimit);
    }

    void OnFrameRangeSliderChanged(ChangeEvent<Vector2> evt)
    {
        int minFrame = Mathf.RoundToInt(evt.newValue.x);
        int maxFrame = Mathf.RoundToInt(evt.newValue.y);
        UpdateFrameRangeLabel(minFrame, maxFrame);
    }

    void UpdateFrameRangeLabel(int minFrame, int maxFrame)
    {
        frameRangeLabel.text = $"Frame range: {minFrame} - {maxFrame}";
    }

    void OnCaptureScreenshotClicked(ClickEvent evt)
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "screenshot.png",
            new[] { new ExtensionFilter("PNG Files", "png") });

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        StructureSequenceController.Instance.SaveCurrentFrame(path);
    }

    static string GetFFmpegExecutivePath()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
                return Path.Combine(Application.dataPath, "ffmpeg.exe");
            case RuntimePlatform.WindowsEditor:
                return Path.Combine(Application.dataPath, "Editor/FFmpeg/Windows/ffmpeg.exe");
            case RuntimePlatform.OSXPlayer:
                return Path.Combine(Application.dataPath, "ffmpeg");
            case RuntimePlatform.OSXEditor:
                return Path.Combine(Application.dataPath, "Editor/FFmpeg/macOS/ffmpeg");
            case RuntimePlatform.LinuxPlayer:
                return Path.Combine(Application.dataPath, "ffmpeg");
            case RuntimePlatform.LinuxEditor:
                return Path.Combine(Application.dataPath, "Editor/FFmpeg/Linux/ffmpeg");
            default:
                return null;
        }
    }
    
    private static string RandomString(int length)
    {
        var random = new System.Random();
        const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
        var builder = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            var c = pool[random.Next(0, pool.Length)];
            builder.Append(c);
        }

        return builder.ToString();
    }

    void OnCaptureSeriesClicked(ClickEvent evt)
    {
        int minFrame = Mathf.RoundToInt(frameRangeSlider.value.x) - 1;
        int maxFrame = Mathf.RoundToInt(frameRangeSlider.value.y) - 1;
        bool encodeVideo = encodeVideoToggle.value;

        string path = StandaloneFileBrowser.OpenFolderPanel("Pick folder to save frame sequence", "", false)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
        string structureFilename = Path.GetFileNameWithoutExtension(App.Instance.CurrentFile.Filename);
        string prefix = $"{structureFilename}_{currentTime}_";

        Action callback = encodeVideo
            ? () =>
            {
                string outputFile = Path.Combine(path, $"{prefix}frames.mp4");
                string inputFiles = Path.Combine(path, $"{prefix}%05d.png");

                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }
                
                string ffmpegPath = GetFFmpegExecutivePath();
                if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
                {
                    OverlayUiController.Instance.PrintStatusLog(
                        "FFmpeg executable not found, cannot encode video.");
                }

                float frameRate = 1f / StructureSequenceController.Instance.PlayInterval;

                ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = ffmpegPath;
                ffmpegProcess.StartInfo.Arguments =
                    $"-framerate {frameRate:F2} -i \"{inputFiles}\" -c:v libx264 -vf \"crop=trunc(iw/2)*2:trunc(ih/2)*2\" -pix_fmt yuv420p \"{outputFile}\"";
                Debug.Log(ffmpegProcess.StartInfo.Arguments);
                ffmpegProcess.StartInfo.WorkingDirectory = path;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        Debug.Log(args.Data);
                    }
                };
                ffmpegProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        Debug.LogError(args.Data);
                    }
                };
                
                ffmpegProcess.Start();
            }
            : null;

        StructureSequenceController.Instance.SaveFrameSequence(path, minFrame, maxFrame, prefix, callback);
    }

    void Update()
    {
        if (ffmpegProcess != null && ffmpegProcess.HasExited)
        {
            if (ffmpegProcess.ExitCode == 0)
            {
                OverlayUiController.Instance.PrintStatusLog("Video encoded to `frames.mp4`.");
            }
            else
            {
                OverlayUiController.Instance.PrintStatusLog("FFmpeg exited with error code: " + ffmpegProcess.ExitCode);
            }

            ffmpegProcess = null;
        }
    }
}