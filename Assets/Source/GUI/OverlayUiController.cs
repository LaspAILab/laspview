using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class OverlayUiController : MonoBehaviour
{
    const string VERSION_NUMBER = "2.0.1";
    
    public static OverlayUiController Instance;
    private ViewportController viewport;
    private UIDocument document;
    
    const string ACTIVE_BUTTON_CLASS_NAME = "active-title-button";
    
    private VisualElement root;
    private VisualElement centerPanel;
    private VisualElement aboutPanel;
    private VisualElement leftInfoPanel;
    private VisualElement periodicTablePanel;
    private PeriodicTableControl periodicTableControl;
    
    private VisualElement filePanel;
    private VisualElement projectPanel;
    private VisualElement optionsPanel;
    private VisualElement editPanel;
    
    // TOP BAR BEGINS
    private Button fileButton;
    private Button projectButton;
    private Button optionsButton;
    private Button editButton;

    private Button aboutButton;
    // TOP BAR ENDS
    
    // BOTTOM BAR BEGINS
    private Button structureMinusButton;
    private Button structurePlusButton;
    private IntegerField currentStructureInput;
    private Label totalStructuresLabel;
    
    private Button focusButton;
    private Button playButton;
    private Button pauseButton;
    private Button nextButton;
    private Button previousButton;
    private Button firstButton;
    private Button lastButton;
    
    private Button statusLabel;
    // BOTTOM BAR ENDS
    
    // INFO PANEL BEGINS
    private Label energyLabel;
    private Label atomCountLabel;
    private Label selectionLabel;
    private Label additionalInfoLabel;
    // INFO PANEL ENDS

    void Awake()
    {
        Instance = this;
        viewport = GameObject.Find("App Container").GetComponent<ViewportController>();

        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        centerPanel = root.Q<VisualElement>("CenterPanel");
        
        aboutPanel = root.Q<VisualElement>("AboutPanel");
        // Close the about panel if empty parts are clicked
        aboutPanel.RegisterCallback<ClickEvent>(ToggleAboutPanel);
        // Avoid duplicate handling of the same click event
        aboutPanel.Children().First().RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        leftInfoPanel = root.Q<VisualElement>("LeftInfoPanel");
        
        // Same as above
        periodicTablePanel = root.Q<VisualElement>("PeriodicTablePanel");
        periodicTablePanel.RegisterCallback<ClickEvent>(ClosePeriodicTable);
        periodicTablePanel.Children().First().RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        periodicTableControl = periodicTablePanel.Q<PeriodicTableControl>();
        // Use `GetComponent` over `EditPanelController.Instance` in case the latter hasn't been inited yet
        periodicTableControl.ElementClickedEvent += GetComponent<EditPanelController>().OnElementSubmit;
        
        var exitAboutButton = aboutPanel.Q<Button>("ExitAboutButton");
        exitAboutButton.RegisterCallback<ClickEvent>(ToggleAboutPanel);
        
        var exitPeriodicButton = root.Q<Button>("ExitPeriodicButton");
        exitPeriodicButton.RegisterCallback<ClickEvent>(ClosePeriodicTable);
        
        filePanel = root.Q<VisualElement>("FilePanel");
        projectPanel = root.Q<VisualElement>("ProjectPanel");
        optionsPanel = root.Q<VisualElement>("OptionsPanel");
        editPanel = root.Q<VisualElement>("EditPanel");
        
        fileButton = root.Q<Button>("FileButton");
        fileButton.RegisterCallback<ClickEvent>(ToggleFilePanel);
        projectButton = root.Q<Button>("ProjectButton");
        projectButton.RegisterCallback<ClickEvent>(ToggleProjectPanel);
        optionsButton = root.Q<Button>("OptionsButton");
        optionsButton.RegisterCallback<ClickEvent>(ToggleOptionsPanel);
        editButton = root.Q<Button>("EditButton");
        editButton.RegisterCallback<ClickEvent>(ToggleEditPanel);

        var uiScalePlusButton = root.Q<Button>("UIScalePlus");
        uiScalePlusButton.RegisterCallback<ClickEvent>(OnUIScaleAdjust);
        var uiScaleMinusButton = root.Q<Button>("UIScaleMinus");
        uiScaleMinusButton.RegisterCallback<ClickEvent>(OnUIScaleAdjust);
        aboutButton = root.Q<Button>("AboutButton");
        aboutButton.RegisterCallback<ClickEvent>(ToggleAboutPanel);
        
        var aboutVersionLabel = aboutPanel.Q<Label>("VersionLabel");
        aboutVersionLabel.text = aboutVersionLabel.text.Replace("%v", VERSION_NUMBER);
        var aboutOsLabel = aboutPanel.Q<Label>("OSLabel");
        aboutOsLabel.text = aboutOsLabel.text.Replace("%os", SystemInfo.operatingSystem);
        
        structureMinusButton = root.Q<Button>("StructureMinus");
        structureMinusButton.RegisterCallback<ClickEvent>(OnPreviousStructureClicked);
        structurePlusButton = root.Q<Button>("StructurePlus");
        structurePlusButton.RegisterCallback<ClickEvent>(OnNextStructureClicked);
        currentStructureInput = root.Q<IntegerField>("CurrentStructure");
        totalStructuresLabel = root.Q<Label>("TotalStructures");
        
        focusButton = root.Q<Button>("FocusButton");
        focusButton.RegisterCallback<ClickEvent>(OnFocusClicked);
        
        playButton = root.Q<Button>("PlayButton");
        playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
        pauseButton = root.Q<Button>("PauseButton");
        pauseButton.RegisterCallback<ClickEvent>(OnPauseClicked);
        nextButton = root.Q<Button>("NextButton");
        nextButton.RegisterCallback<ClickEvent>(OnNextStructureClicked);
        previousButton = root.Q<Button>("PreviousButton");
        previousButton.RegisterCallback<ClickEvent>(OnPreviousStructureClicked);
        firstButton = root.Q<Button>("FirstButton");
        firstButton.RegisterCallback<ClickEvent>(OnFirstStructureClicked);
        lastButton = root.Q<Button>("LastButton");
        lastButton.RegisterCallback<ClickEvent>(OnLastStructureClicked);
        
        statusLabel = root.Q<Button>("StatusLabel");
        
        energyLabel = root.Q<Label>("EnergyLabel");
        atomCountLabel = root.Q<Label>("AtomCountLabel");
        selectionLabel = root.Q<Label>("SelectionLabel");
        additionalInfoLabel = root.Q<Label>("AdditionalInfoLabel");
    }

    public void PrintStatusLog(string str, bool addToUnityLog = true)
    {
        statusLabel.text = str;
        statusLabel.tooltip = str;
        if (addToUnityLog)
        {
            Debug.Log(str);
        }
    }

    public void OnToolChange(ToolType newTool)
    {
        switch (newTool)
        {
            case ToolType.MoveView:
                centerPanel.style.opacity = 1;
                centerPanel.RemoveFromClassList("center-panel-rotate");
                centerPanel.AddToClassList("center-panel-pan");
                break;
            case ToolType.RotateView:
                centerPanel.style.opacity = 1;
                centerPanel.RemoveFromClassList("center-panel-pan");
                centerPanel.AddToClassList("center-panel-rotate");
                break;
            default:
                centerPanel.style.opacity = 0;
                break;
        }
    }

    public void UpdateStructureIndex(int? current, int? total)
    {
        if (current.HasValue)
        {
            currentStructureInput.SetValueWithoutNotify(current.Value + 1);
        }

        if (total.HasValue)
        {
            totalStructuresLabel.text = $"of {total.Value}";
        }
    }

    public void UpdateStructureInfo(CrystalStructure structure)
    {
        energyLabel.text = $"Energy: {structure.Energy} eV";
        
        string formula = ChemicalFormulaUtils.GetChemicalFormula(structure.Atoms, 50);
        
        atomCountLabel.text = $"Atom Count: {structure.Atoms.Count} ({formula})";
    }

    public void UpdateSelectionInfo(List<SelectionItem> selection)
    {
        var distinctSelection = selection.Distinct(new SelectionItemComparator()).ToList();
        EditPanelController.Instance.UpdateSelectionInfo(distinctSelection);

        if (selection.Count > 0)
        {
            leftInfoPanel.style.display = DisplayStyle.Flex;
            string formula = ChemicalFormulaUtils.GetChemicalFormula(distinctSelection.Select(x => x.Atom.SourceAtom).ToList());
            selectionLabel.text = $"Selected: {distinctSelection.Count} ({formula})";
        }
        else
        {
            leftInfoPanel.style.display = DisplayStyle.None;
        }
        
        if (selection.Count is 0 or > 4)
        {
            additionalInfoLabel.text = "";
            return;
        }

        var sortedSelection = selection.OrderBy(x => x.SelectIndex).ToList();
        if (selection.Count == 1)
        {
            string fixedState = sortedSelection[0].Atom.SourceAtom.Flags switch
            {
                AtomFlags.Fixed => "Fixed",
                AtomFlags.React => "React",
                _ => "Free"
            };
            additionalInfoLabel.text =
                $"{sortedSelection[0].Atom.Symbol} {sortedSelection[0].Atom.SourceAtom.Coordinates} {fixedState}";
        }
        else if (selection.Count == 2)
        {
            var length = Vector3.Distance(sortedSelection[0].Position, sortedSelection[1].Position);
            additionalInfoLabel.text =
                $"{sortedSelection[0].Atom.Symbol}-{sortedSelection[1].Atom.Symbol} dist: {length} A";
        }
        else if (selection.Count == 3)
        {
            Vector3 vec1 = sortedSelection[0].Position - sortedSelection[1].Position;
            Vector3 vec2 = sortedSelection[2].Position - sortedSelection[1].Position;

            var angle = Vector3.Angle(vec1, vec2);
            additionalInfoLabel.text =
                $"{sortedSelection[0].Atom.Symbol}-{sortedSelection[1].Atom.Symbol}-{sortedSelection[2].Atom.Symbol} angle: {angle:f3} degrees";
        }
        else if (selection.Count == 4)
        {
            Vector3 vec1 = sortedSelection[0].Position - sortedSelection[1].Position;
            Vector3 vec2 = sortedSelection[2].Position - sortedSelection[1].Position;
            Vector3 vec3 = sortedSelection[3].Position - sortedSelection[2].Position;

            Vector3 normal1 = Vector3.Cross(vec1, vec2);
            Vector3 normal2 = Vector3.Cross(-vec2, vec3);

            var angle = Vector3.Angle(normal1, normal2);
            additionalInfoLabel.text =
                $"{sortedSelection[0].Atom.Symbol}-{sortedSelection[1].Atom.Symbol}-{sortedSelection[2].Atom.Symbol}-{sortedSelection[3].Atom.Symbol} dihedral angle: {angle:f3} degrees";
        }
    }

    void OnUIScaleAdjust(ClickEvent evt)
    {
        if (evt.target is Button button)
        {
            float delta = button.text == "+" ? 0.1f : -0.1f;
            float newValue = Mathf.Clamp(document.panelSettings.scale + delta, 0.1f, 3f);
            Debug.Log($"Set UI Scale: {newValue}");
            document.panelSettings.scale = newValue;
        }
        
    }
    
    void OnFocusClicked(ClickEvent evt)
    {
        viewport.RefocusCamera();
    }
    
    void OnPlayClicked(ClickEvent evt)
    {
        if (StructureSequenceController.Instance.Play())
        {
            playButton.SetEnabled(false);
            previousButton.SetEnabled(false);
            nextButton.SetEnabled(false);
            firstButton.SetEnabled(false);
            lastButton.SetEnabled(false);
            
            structureMinusButton.SetEnabled(false);
            structurePlusButton.SetEnabled(false);
        }
    }

    void OnPauseClicked(ClickEvent evt)
    {
        if (StructureSequenceController.Instance.Stop())
        {
            playButton.SetEnabled(true);
            previousButton.SetEnabled(true);
            nextButton.SetEnabled(true);
            firstButton.SetEnabled(true);
            lastButton.SetEnabled(true);
            
            structureMinusButton.SetEnabled(true);
            structurePlusButton.SetEnabled(true);
        }
    }
    
    void OnNextStructureClicked(ClickEvent evt)
    {
        StructureSequenceController.Instance.Next();
    }
    
    void OnPreviousStructureClicked(ClickEvent evt)
    {
        StructureSequenceController.Instance.Previous();
    }

    void OnFirstStructureClicked(ClickEvent evt)
    {
        StructureSequenceController.Instance.SwitchStructure(0);
    }
    
    void OnLastStructureClicked(ClickEvent evt)
    {
        StructureSequenceController.Instance.SwitchStructure(StructureSequenceController.Instance.StructureCount - 1);
    }

    void CloseAllLeftPanels()
    {
        filePanel.style.display = DisplayStyle.None;
        projectPanel.style.display = DisplayStyle.None;
        optionsPanel.style.display = DisplayStyle.None;
        
        fileButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        projectButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        optionsButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        editButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
    }
    
    void ToggleFilePanel(ClickEvent _)
    {
        if (filePanel.resolvedStyle.display == DisplayStyle.None)
        {
            CloseAllLeftPanels();
            filePanel.style.display = DisplayStyle.Flex;
            fileButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
        else
        {
            filePanel.style.display = DisplayStyle.None;
            fileButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
    }

    void ToggleProjectPanel(ClickEvent _)
    {
        if (projectPanel.resolvedStyle.display == DisplayStyle.None)
        {
            CloseAllLeftPanels();
            projectPanel.style.display = DisplayStyle.Flex;
            projectButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
        else
        {
            projectPanel.style.display = DisplayStyle.None;
            projectButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
    }

    void ToggleOptionsPanel(ClickEvent _)
    {
        if (optionsPanel.resolvedStyle.display == DisplayStyle.None)
        {
            CloseAllLeftPanels();
            optionsPanel.style.display = DisplayStyle.Flex;
            optionsButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
        else
        {
            optionsPanel.style.display = DisplayStyle.None;
            optionsButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
    }
    
    void ToggleEditPanel(ClickEvent _)
    {
        if (editPanel.resolvedStyle.display == DisplayStyle.None)
        {
            editPanel.style.display = DisplayStyle.Flex;
            editButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
        else
        {
            editPanel.style.display = DisplayStyle.None;
            editButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
    }
    
    void ToggleAboutPanel(ClickEvent _)
    {
        if (aboutPanel.resolvedStyle.display == DisplayStyle.None)
        {
            aboutPanel.style.display = DisplayStyle.Flex;
            aboutButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
        else
        {
            aboutPanel.style.display = DisplayStyle.None;
            aboutButton.RemoveFromClassList(ACTIVE_BUTTON_CLASS_NAME);
        }
    }
    
    public void ShowPeriodicTable(ClickEvent _)
    {
        periodicTablePanel.style.display = DisplayStyle.Flex;
    }
    
    public void ClosePeriodicTable(ClickEvent _)
    {
        periodicTablePanel.style.display = DisplayStyle.None;
    }
}
