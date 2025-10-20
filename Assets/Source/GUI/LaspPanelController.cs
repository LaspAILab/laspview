
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class LaspPanelController : MonoBehaviour
{
    public static LaspPanelController Instance;
    
    VisualElement laspPanelRoot;
    
    private Button startServerButton;
    private Button runOptimizationButton;
    
    private Label serverStatusLabel;
    private Toggle realtimeEnergyToggle;
    private Toggle optimizeLatticeToggle;
    private Toggle overrideStructureToggle;
    
    private bool updateEnergyRealtime = false;

    void Awake()
    {
        Instance = this;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        laspPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("LaspPanel");

        startServerButton = laspPanelRoot.Q<Button>("StartServerButton");
        startServerButton.RegisterCallback<ClickEvent>(OnStartServerClicked);
        runOptimizationButton = laspPanelRoot.Q<Button>("RunOptimizationButton");
        runOptimizationButton.RegisterCallback<ClickEvent>(OnRunOptimizationClicked);
        
        serverStatusLabel = laspPanelRoot.Q<Label>("ServerStatusLabel");
        realtimeEnergyToggle = laspPanelRoot.Q<Toggle>("RealtimeEnergyToggle");
        optimizeLatticeToggle = laspPanelRoot.Q<Toggle>("OptimizeLatticeToggle");
        overrideStructureToggle = laspPanelRoot.Q<Toggle>("OverrideStructureToggle");
        
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
            
            OverlayUiController.Instance.UpdateStructureInfo(StructureSequenceController.Instance.CurrentStructure, true);
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
}