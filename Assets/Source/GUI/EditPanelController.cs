
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class EditPanelController : MonoBehaviour
{
    private VisualElement editPanelRoot;
    private ViewportController viewport;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        viewport = GameObject.Find("App Container").GetComponent<ViewportController>();
        editPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("EditPanel");

        var coordsControl = editPanelRoot.Q<Vec3InputControl>("CoordsControl");
        coordsControl.ValueChangedEvent += OnCoordsChange;
    }

    void OnCoordsChange(VisualElement sender, Vector3 newValue)
    {
        Debug.Log(newValue);
    }
}