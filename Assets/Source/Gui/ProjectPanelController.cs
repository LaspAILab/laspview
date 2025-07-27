using System;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UIElements;

public class ProjectPanelController : MonoBehaviour
{
    private static BackgroundPosition ButtonBackgroundPosition =
        new(BackgroundPositionKeyword.Left, new Length(20, LengthUnit.Pixel));
    private static BackgroundSize ButtonBackgroundSize =
        new(new Length(20, LengthUnit.Pixel), new Length(20, LengthUnit.Pixel));
    
    public static ProjectPanelController Instance;
    
    private VisualElement projectPanelRoot;
    private ScrollView scrollView;
    private Dictionary<string, Button> buttons =new();
    
    void Start()
    {
        Instance = this;
        projectPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ProjectPanel");
        
        scrollView = projectPanelRoot.Q<ScrollView>();
    }

    void OnProjectItemClick(ClickEvent e)
    {
        if (e.target is Button b)
        {
            App.Instance.SwitchArcFile(b.text);
        }
    }
    
    public void AddProjectFile(string itemName)
    {
        var newButton = new Button
        {
            text = itemName,
            style =
            {
                // This should have been handled by CSS, but there's a bug when it comes to background. 
                backgroundPositionX = new StyleBackgroundPosition(ButtonBackgroundPosition),
                backgroundSize = new StyleBackgroundSize(ButtonBackgroundSize)
            }
        };

        newButton.AddToClassList("transparent-button");
        newButton.AddToClassList("project-item-button");
        newButton.RegisterCallback<ClickEvent>(OnProjectItemClick);
        
        buttons.Add(itemName, newButton);
        scrollView.Add(newButton);
    }

    public void RemoveProjectFile(string itemName)
    {
        if (buttons.TryGetValue(itemName, out Button button))
        {
            scrollView.Remove(button);
            buttons.Remove(itemName);
        }
        else
        {
            Debug.LogError($"Project file {itemName} cannot be removed since it doesn't exist.");
        }
    }
}