using UnityEngine;
using UnityEngine.UIElements;

public class OverlayUiController : MonoBehaviour
{
    public static OverlayUiController Instance;
    private ViewportController viewport;
    
    const string ACTIVE_BUTTON_CLASS_NAME = "active-title-button";
    
    private VisualElement root;
    private VisualElement centerPanel;
    private VisualElement aboutPanel;
    
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
    // INFO PANEL ENDS

    void Awake()
    {
        Instance = this;
        viewport = GameObject.Find("App Container").GetComponent<ViewportController>();
        
        root = GetComponent<UIDocument>().rootVisualElement;
        aboutPanel = root.Q<VisualElement>("AboutPanel");
        centerPanel = root.Q<VisualElement>("CenterPanel");
        
        var exitAboutButton = aboutPanel.Q<Button>("ExitButton");
        exitAboutButton.RegisterCallback<ClickEvent>(ToggleAboutPanel);
        
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
        
        aboutButton = root.Q<Button>("AboutButton");
        aboutButton.RegisterCallback<ClickEvent>(ToggleAboutPanel);
        
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
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
        energyLabel.text = $"Energy: {structure.Energy} kJ";
        
        string formula = ChemicalFormulaUtils.GetChemicalFormula(structure.Atoms, 50);
        
        atomCountLabel.text = $"Atom Count: {structure.Atoms.Count} ({formula})";
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
}
