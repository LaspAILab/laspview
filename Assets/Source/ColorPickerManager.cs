using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColorPickerManager : MonoBehaviour
{
    public FlexibleColorPicker ColorPickerWidget;
    public static ColorPickerManager Instance;

    private Action<Color> onColorChanged;
    private Camera uiCamera;
    private RectTransform rectTransform;

    private bool isDialogOpen = false;

    void Awake()
    {
        Instance = this;
        uiCamera = Camera.main;
        rectTransform = ColorPickerWidget.GetComponent<RectTransform>();
    }

    void Start()
    {
        ClosePicker();
    }

    public void OpenPicker(Color initialColor, Action<Color> callback = null)
    {
        onColorChanged = callback;
        Debug.Log($"Opening color picker");
        ColorPickerWidget.gameObject.SetActive(true);
        ColorPickerWidget.SetColorNoAlpha(initialColor);
    }

    public void ClosePicker()
    {
        ColorPickerWidget.gameObject.SetActive(false);
        isDialogOpen = false;
    }

    public void OnColorChanged(Color color)
    {
        onColorChanged?.Invoke(color);
    }

    void Update()
    {
        if (isDialogOpen && Input.GetKeyUp(KeyCode.Mouse0) &&
            !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, uiCamera))
        {
            ClosePicker();
        }

        // Assign the state after checking left mouse button, or the dialog will close immediately after it opens
        if (!isDialogOpen && ColorPickerWidget.gameObject.activeSelf)
        {
            isDialogOpen = true;
        }
    }
}