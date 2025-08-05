using UnityEngine;
using UnityEngine.UIElements;

public class OverlayUiController : MonoBehaviour
{
    public static OverlayUiController Instance;
    private ViewportController viewport;
    
    const string ACTIVE_BUTTON_CLASS_NAME = "active-title-button";
    
    private VisualElement root;
    private VisualElement centerPanel;
    
    // TOP BAR BEGINS
    private VisualElement filePanel;
    private VisualElement projectPanel;
    private VisualElement optionsPanel;
    private VisualElement editPanel;
    
    private Button fileButton;
    private Button projectButton;
    private Button optionsButton;
    private Button editButton;
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
        centerPanel = root.Q<VisualElement>("CenterPanel");
        
        filePanel = root.Q<VisualElement>("FilePanel");
        projectPanel = root.Q<VisualElement>("ProjectPanel");
        optionsPanel = root.Q<VisualElement>("OptionsPanel");
        // editPanel = root.Q<VisualElement>("EditPanel");
        
        fileButton = root.Q<Button>("FileButton");
        fileButton.RegisterCallback<ClickEvent>(OpenFilePanel);
        projectButton = root.Q<Button>("ProjectButton");
        projectButton.RegisterCallback<ClickEvent>(OpenProjectPanel);
        optionsButton = root.Q<Button>("OptionsButton");
        optionsButton.RegisterCallback<ClickEvent>(OpenOptionsPanel);
        editButton = root.Q<Button>("EditButton");
        
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
        OpenFilePanel(null);
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
    
    void OpenFilePanel(ClickEvent _)
    {
        CloseAllLeftPanels();
        filePanel.style.display = DisplayStyle.Flex;
        fileButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
    }

    void OpenProjectPanel(ClickEvent _)
    {
        CloseAllLeftPanels();
        projectPanel.style.display = DisplayStyle.Flex;
        projectButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
        
    }

    void OpenOptionsPanel(ClickEvent _)
    {
        CloseAllLeftPanels();
        optionsPanel.style.display = DisplayStyle.Flex;
        optionsButton.AddToClassList(ACTIVE_BUTTON_CLASS_NAME);
    }
}
