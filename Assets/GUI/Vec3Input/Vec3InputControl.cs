using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Vec3InputControl : VisualElement, INotifyValueChanged<Vector3>
{
    private const string INPUT_FIELD_CLASS = "-vec3-input-float-field";
    private const string LABEL_CLASS = "-vec3-input-label";
    
    public delegate void ValueChangedEventHandler(VisualElement sender, Vector3 newValue);

    public event ValueChangedEventHandler ValueChangedEvent;
    
    [UxmlAttribute]
    public string text
    {
        get => textLabel.text;
        set => textLabel.text = value;
    }

    public void SetValueWithoutNotify(Vector3 newValue)
    {
        xInput.SetValueWithoutNotify(newValue.x);
        yInput.SetValueWithoutNotify(newValue.y);
        zInput.SetValueWithoutNotify(newValue.z);
    }

    [UxmlAttribute]
    public Vector3 value
    {
        get => new(xInput.value, yInput.value, zInput.value);
        set
        {
            Vector3 previousValue = this.value;
            if(value == previousValue)
            {
                return;
            }
            
            SetValueWithoutNotify(value);
            if (panel != null)
            {
                ValueChangedEvent?.Invoke(this, value);
            }
        }
    }
    
    private Label textLabel;
    private FloatField xInput, yInput, zInput;

    public Vec3InputControl()
    {
        style.flexDirection = FlexDirection.Row;
        
        textLabel = new Label
        {
            style =
            {
                alignSelf = Align.Center
            }
        };
        textLabel.AddToClassList(LABEL_CLASS);
        Add(textLabel);
        
        var whitespace = new VisualElement
        {
            style =
            {
                flexGrow = 1
            }
        };
        Add(whitespace);

        xInput = new FloatField
        {
            maxLength = 5
        };
        xInput.AddToClassList(INPUT_FIELD_CLASS);
        Add(xInput);
        
        yInput = new FloatField
        {
            maxLength = 5
        };;
        yInput.AddToClassList(INPUT_FIELD_CLASS);
        Add(yInput);
        
        zInput = new FloatField
        {
            maxLength = 5
        };;
        zInput.AddToClassList(INPUT_FIELD_CLASS);
        Add(zInput);
        
        // Register for a callback to handle attribute changes or panel attachment
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private void ValueChangedCallback(ChangeEvent<float> _)
    {
        ValueChangedEvent?.Invoke(this, value);
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        textLabel.text = text;
        xInput.RegisterValueChangedCallback(ValueChangedCallback);
        yInput.RegisterValueChangedCallback(ValueChangedCallback);
        zInput.RegisterValueChangedCallback(ValueChangedCallback);
    }
    
    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        xInput.UnregisterValueChangedCallback(ValueChangedCallback);
        yInput.UnregisterValueChangedCallback(ValueChangedCallback);
        zInput.UnregisterValueChangedCallback(ValueChangedCallback);
    }
}
