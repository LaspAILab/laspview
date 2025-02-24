using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScalarInputController : MonoBehaviour
{
    private InputField inputField;
    public Action onChange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputField = GetComponentInChildren<InputField>();
        UnityAction<string> action = (_) => OnChange();
        inputField.onSubmit.AddListener(action);
    }

    public string GetValue()
    {
        return inputField.text;
    }

    public void SetValue(string value)
    {
        inputField.text = value;
    }

    public void SetValue(float value)
    {
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