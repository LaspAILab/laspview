using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

public class VectorInputController : MonoBehaviour
{
    private InputField[] inputFields;
    private Action onChange;

    private Vector3? newValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputFields = GetComponentsInChildren<InputField>();
        Assert.AreEqual(inputFields.Length, 3);

        UnityAction<string> action = _ => OnChange();
        foreach (var item in inputFields)
        {
            item.onSubmit.AddListener(action);
        }
    }

    private void OnEnable()
    {
        if (newValue != null)
        {
            SetValue(newValue.Value);
            newValue = null;
        }
    }

    public Vector3? GetValue()
    {
        if (!gameObject.activeInHierarchy)
        {
            return null;
        }

        if (!float.TryParse(inputFields[0].text, out float a))
        {
            return null;
        }

        if (!float.TryParse(inputFields[1].text, out float b))
        {
            return null;
        }

        if (!float.TryParse(inputFields[2].text, out float c))
        {
            return null;
        }

        return new Vector3(a, b, c);
    }

    public void SetValue(Vector3 value)
    {
        if (!gameObject.activeInHierarchy)
        {
            newValue = value;
            return;
        }

        inputFields[0].text = value.x.ToString("0.0##");
        inputFields[1].text = value.y.ToString("0.0##");
        inputFields[2].text = value.z.ToString("0.0##");
    }

    public void SetValue(float x, float y, float z)
    {
        if (!gameObject.activeInHierarchy)
        {
            newValue = new Vector3(x, y, z);
            return;
        }

        inputFields[0].text = x.ToString("0.0##");
        inputFields[1].text = y.ToString("0.0##");
        inputFields[2].text = z.ToString("0.0##");
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