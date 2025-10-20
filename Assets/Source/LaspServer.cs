using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public enum ServerStatusType
{
    Unstarted,
    WaitingForStartup,
    Idle,
    WaitingForResponse
}

public enum CalculationType
{
    Energy,
    OptimizeNoCell,
    OptimizeWithCell
}

public class CalculationRequest
{
    public CalculationType type;
    public WeakReference<StructureFile> structureFile;
    public int structureIndex;
    public float filedTime;
}

public class LaspServer : MonoBehaviour
{
    public static LaspServer Instance;

    private Process serverProcess;

    public ServerStatusType ServerStatus { get; private set; } = ServerStatusType.Unstarted;

    private string serverUrl = "";
    private CalculationRequest queuedEnergyRequest, queuedOptimizeRequest;
    
    private const string pythonInterpreterPath = "LaspServer/.venv/Scripts/python.exe";
    private const string pythonScriptPath = "LaspServer/server_main.py";
    private const string laspModelPath = "LaspServer/batch_lasp_model.pth";

    public delegate void OnServerStatusChanged(ServerStatusType serverStatus);

    public event OnServerStatusChanged OnServerStatusChangedEvent;

    public delegate void OnEnergyReceived(CalculationRequest energyRequest, float newEnergy);
    public delegate void OnOptimizeReceived(CalculationRequest optimizeRequest, CrystalStructure newStructure);
    
    public event OnEnergyReceived OnEnergyReceivedEvent;
    public event OnOptimizeReceived OnOptimizeReceivedEvent;

    void Awake()
    {
        Instance = this;
    }

    void UpdateServerStatus(ServerStatusType serverStatus)
    {
        if (ServerStatus == serverStatus)
        {
            return;
        }

        ServerStatus = serverStatus;
        OnServerStatusChangedEvent?.Invoke(serverStatus);
    }

    public void StartServer(int port)
    {
#if UNITY_WEBGL
        // WebGL cannot start a local server
        Debug.LogWarning("WebGL cannot start a local server.");
        ServerStatus = ServerStatusType.Unstarted;
#else
        serverUrl = $"http://localhost:{port}";

        Debug.Log(Directory.GetCurrentDirectory());
        try
        {
            serverProcess = new Process();
            serverProcess.StartInfo.FileName = pythonInterpreterPath;
            serverProcess.StartInfo.Arguments =
                $"\"{pythonScriptPath}\" --port {port} --model-path \"{laspModelPath}\"";
            serverProcess.StartInfo.UseShellExecute = false;
            serverProcess.StartInfo.RedirectStandardOutput = true;
            serverProcess.StartInfo.RedirectStandardError = true;
            serverProcess.StartInfo.CreateNoWindow = true;

            serverProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.Log($"Python Output: {args.Data}");
                }
            };

            serverProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.LogWarning($"Python Error: {args.Data}");
                }
            };

            serverProcess.Start();
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
            OverlayUiController.Instance.PrintStatusLog($"Launched LASP server on port {port}");
            UpdateServerStatus(ServerStatusType.WaitingForStartup);

            // 等待服务器启动
            StartCoroutine(WaitForServerStartup());
        }
        catch (Exception e)
        {
            OverlayUiController.Instance.PrintStatusLog($"Failed to start Python server: {e.Message}");
            UpdateServerStatus(ServerStatusType.Unstarted);
        }
#endif
    }

    public void ShutdownServer()
    {
        if (serverProcess != null && !serverProcess.HasExited)
        {
            serverProcess.Kill();
            serverProcess.WaitForExit();
            OverlayUiController.Instance.PrintStatusLog("LASP server process terminated.");
            UpdateServerStatus(ServerStatusType.Unstarted);
        }
    }

    /// <summary>
    /// Wait for server startup
    /// </summary>
    private IEnumerator WaitForServerStartup()
    {
        yield return new WaitForSeconds(2f);

        if (serverProcess != null)
        {
            if (serverProcess.HasExited)
            {
                OverlayUiController.Instance.PrintStatusLog(
                    $"LASP server process exited with code {serverProcess.ExitCode}");
                UpdateServerStatus(ServerStatusType.Unstarted);
            }
            else
            {
                // Ping to server to ensure it has started
                PingServer();
            }
        }
        else
        {
            Debug.LogError("Waiting for LASP server startup, LASP server process shouldn't be null.");
            UpdateServerStatus(ServerStatusType.Unstarted);
        }
    }

    /// <summary>
    /// Ping server to check whether it is healthy
    /// </summary>
    private void PingServer()
    {
        string pingUrl = $"{serverUrl}/ping";

        StartCoroutine(SendGetRequest(pingUrl, 2, (result, status, response) =>
        {
            if (result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Server not started yet, will retry in 2 seconds. ");
                UpdateServerStatus(ServerStatusType.WaitingForStartup);
                StartCoroutine(WaitForServerStartup());
            }
            else if (result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Server is healthy and ready");
                UpdateServerStatus(ServerStatusType.Idle);
            }
            else
            {
                Debug.LogError($"Server returned unexpected result ({status}), shutting down! ");
                ShutdownServer();
                UpdateServerStatus(ServerStatusType.Unstarted);
            }
        }));
    }
    
    public void ScheduleCalculation(CalculationRequest request)
    {
        if (ServerStatus == ServerStatusType.Unstarted)
        {
            return;
        }
        
        if(!request.structureFile.TryGetTarget(out var structureFile))
        {
            Debug.LogError("Structure file reference lost, cannot schedule energy calculation.");
            return;
        }
        
        if (ServerStatus != ServerStatusType.Idle)
        {
            switch (request.type)
            {
                case CalculationType.Energy:
                    queuedEnergyRequest = request;
                    break;
                case CalculationType.OptimizeNoCell:
                case CalculationType.OptimizeWithCell:
                    queuedOptimizeRequest = request;
                    break;
            }
            return;
        }

        StructureFile newStructureFile = new("");
        newStructureFile.Structures.Add(structureFile.Structures[request.structureIndex]);
        var cifFormat = new CifFormat();
        var cif = cifFormat.Write(newStructureFile);

        string url = request.type switch
        {
            CalculationType.Energy => $"{serverUrl}/energy",
            CalculationType.OptimizeNoCell => $"{serverUrl}/optimize_fixed",
            CalculationType.OptimizeWithCell => $"{serverUrl}/optimize",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        StartCoroutine(SendPostRequest($"{serverUrl}/energy", cif, 20, (result, status, response) =>
        {
            if (result == UnityWebRequest.Result.Success)
            {
                if (request.type == CalculationType.Energy)
                {
                    float energy = float.Parse(response);
                    OnEnergyReceivedEvent?.Invoke(request, energy);
                }
                else
                {
                    string cifResponse = response;
                    StructureFile optimizedStructure = new("");
                    cifFormat.Read(optimizedStructure, cifResponse.Split('\n'));
                    OnOptimizeReceivedEvent?.Invoke(request, optimizedStructure.Structures[0]);
                }
            }
            else
            {
                Debug.LogError($"Energy calculation request failed with status {status}");
            }

            RunQueuedRequests();
        }));
    }
    
    public void RunQueuedRequests()
    {
        if (queuedEnergyRequest != null && queuedOptimizeRequest != null)
        {
            if(queuedEnergyRequest.filedTime <= queuedOptimizeRequest.filedTime)
            {
                ScheduleCalculation(queuedEnergyRequest);
                queuedEnergyRequest = null;
            }
            else
            {
                ScheduleCalculation(queuedOptimizeRequest);
                queuedOptimizeRequest = null;
            }

            return;
        }
        
        if(queuedEnergyRequest != null)
        {
            ScheduleCalculation(queuedEnergyRequest);
            queuedEnergyRequest = null;
        }
        
        if(queuedOptimizeRequest != null)
        {
            ScheduleCalculation(queuedOptimizeRequest);
            queuedOptimizeRequest = null;
        }
    }

    private IEnumerator SendGetRequest(string url, int timeout, Action<UnityWebRequest.Result, long, string> callback)
    {
        var request = UnityWebRequest.Get(url);
        request.timeout = timeout;

        UpdateServerStatus(ServerStatusType.WaitingForResponse);
        yield return request.SendWebRequest();
        UpdateServerStatus(ServerStatusType.Idle);
        callback(request.result, request.responseCode, request.downloadHandler.text);
    }

    private IEnumerator SendPostRequest(string url, string payload, int timeout,
        Action<UnityWebRequest.Result, long, string> callback)
    {
        // Post as plain text
        var request = UnityWebRequest.Post(url, payload, "text/plain");
        request.timeout = timeout;

        UpdateServerStatus(ServerStatusType.WaitingForResponse);
        yield return request.SendWebRequest();
        UpdateServerStatus(ServerStatusType.Idle);
        callback(request.result, request.responseCode, request.downloadHandler.text);
    }
}