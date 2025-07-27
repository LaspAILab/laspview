using UnityEngine;
using UnityEngine.UIElements;

public class OverlayUiController : MonoBehaviour
{
    public static OverlayUiController Instance;
    
    const string ACTIVE_BUTTON_CLASS_NAME = "active-title-button";
    
    private VisualElement root;
    
    private VisualElement filePanel;
    private VisualElement projectPanel;
    private VisualElement optionsPanel;
    private VisualElement editPanel;
    
    private Button fileButton;
    private Button projectButton;
    private Button optionsButton;
    private Button editButton;
    
    private Button statusLabel;

    void Awake()
    {
        Instance = this;
        
        root = GetComponent<UIDocument>().rootVisualElement;
        
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
        
        statusLabel = root.Q<Button>("StatusLabel");
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OpenFilePanel(null);
    }

    public void PrintStatusLog(string str, bool addToUnityLog = true)
    {
        statusLabel.text = str;
        if (addToUnityLog)
        {
            Debug.Log(str);
        }
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
