using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ProjectItemControl : VisualElement
{
    private const string TRANSPARENT_BUTTON_CLASS = "-project-transparent-button";
    private const string MAIN_BUTTON_CLASS = "-project-item-button";
    private const string REMOVE_BUTTON_CLASS = "-project-item-remove-button";
    private const string ICON_CLASS = "-project-item-icon";
    
    public delegate void ClickedEventHandler(VisualElement sender, string itemKey);
    
    public event ClickedEventHandler BodyClickedEvent;
    public event ClickedEventHandler RemoveClickedEvent;
    
    [UxmlAttribute]
    public string text
    {
        get => mainButton.text;
        set => mainButton.text = value;
    }
    
    [UxmlAttribute]
    public string itemKey
    {
        get => _itemKey;
        set => _itemKey = value;
    }

    private string _itemKey;
    private Button mainButton;
    private Button removeButton;

    public ProjectItemControl()
    {
        mainButton = new Button
        {
            style =
            {
                // This should have been handled by CSS, but there's a bug when it comes to background. 
                // backgroundPositionX = new StyleBackgroundPosition(ButtonBackgroundPosition),
                // backgroundSize = new StyleBackgroundSize(ButtonBackgroundSize)
            }
        };
        
        mainButton.AddToClassList(TRANSPARENT_BUTTON_CLASS);
        mainButton.AddToClassList(MAIN_BUTTON_CLASS);
        
        var icon = new VisualElement();
        icon.AddToClassList(ICON_CLASS);
        
        mainButton.Add(icon);
        Add(mainButton);

        removeButton = new Button();
        removeButton.AddToClassList(TRANSPARENT_BUTTON_CLASS);
        removeButton.AddToClassList(REMOVE_BUTTON_CLASS);
        Add(removeButton);
        
        // Register for a callback to handle attribute changes or panel attachment
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }
    
    private void BodyClickedCallback(ClickEvent evt)
    {
        BodyClickedEvent?.Invoke(this, _itemKey);
    }

    private void RemoveClickedCallback(ClickEvent evt)
    {
        RemoveClickedEvent?.Invoke(this, _itemKey);
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        mainButton.text = text;
        mainButton.RegisterCallback<ClickEvent>(BodyClickedCallback);
        removeButton.RegisterCallback<ClickEvent>(RemoveClickedCallback);
    }
    
    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        mainButton.UnregisterCallback<ClickEvent>(BodyClickedCallback);
        removeButton.UnregisterCallback<ClickEvent>(RemoveClickedCallback);
    }
}
