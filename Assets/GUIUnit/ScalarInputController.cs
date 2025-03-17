using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScalarInputController : MonoBehaviour
{
    private InputField inputField;
    private Action onChange;
    
    private string newValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputField = GetComponentInChildren<InputField>();
        UnityAction<string> action = (_) => OnChange();
        inputField.onSubmit.AddListener(action);
    }
    
    private void OnEnable()
    {
        if (newValue != null)
        {
            SetValue(newValue);
            newValue = null;
        }
    }

    public string GetValue()
    {
        if (!gameObject.activeInHierarchy)
        {
            return null;
        }
        
        return inputField.text;
    }

    public void SetValue(string value)
    {
        if (!gameObject.activeInHierarchy)
        {
            newValue = value;
            return;
        }
        
        inputField.text = value;
    }

    public void SetValue(float value)
    {
        if (!gameObject.activeInHierarchy)
        {
            newValue = value.ToString("0.0##");
            return;
        }
        
        inputField.text = value.ToString("0.0##");
    }

    public void SetOnChangeCallback(Action action)
    {
        onChange = action;
    }

    public void OnChange()
    {
        onChange?.Invoke();
    }
}