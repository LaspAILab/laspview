using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ColorPickerController : MonoBehaviour
{
    private Action<Color> onColorChanged;
    private RawImage colorImage;
    private Color currentColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        colorImage = GetComponentInChildren<RawImage>();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetColor(Color color)
    {
        currentColor = color;
        colorImage.color = color;
    }

    public void RegisterCallback(Action<Color> callback)
    {
        onColorChanged = callback;
    }

    private void ChangeColorCallback(Color newColor)
    {
        SetColor(newColor);
        onColorChanged?.Invoke(newColor);
    }

    public void OnClick()
    {
        ColorPickerManager.Instance.OpenPicker(currentColor, ChangeColorCallback);
    }
}