using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class ProjectPanelController : MonoBehaviour
{
    public static ProjectPanelController Instance;
    
    private VisualElement projectPanelRoot;
    private ScrollView scrollView;
    
    void Start()
    {
        Instance = this;
        projectPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ProjectPanel");
        
        scrollView = projectPanelRoot.Q<ScrollView>();
        
#if UNITY_WEBGL
        // Enable the format selector in that WebGL doesn't have a save file dialog
        var noticeLabel = projectPanelRoot.Q<Label>("MemoryNoticeLabel");
        noticeLabel.style.display = DisplayStyle.Flex;
#endif
    }

    void OnProjectItemClick(VisualElement _, string itemKey)
    {
        App.Instance.SwitchArcFile(itemKey);
    }
    
    void OnProjectItemRemoveClick(VisualElement _, string itemKey)
    {
        App.Instance.CloseOpenFile(itemKey);
    }

    public void RegenerateProjectList()
    {
        scrollView.Clear();

        foreach (var item in App.Instance.OpenFileList)
        {
            ProjectItemControl itemControl = new ProjectItemControl
            {
                itemKey = item.Key,
                text = item.Value.Filename,
            };
            
            itemControl.BodyClickedEvent += OnProjectItemClick;
            itemControl.RemoveClickedEvent += OnProjectItemRemoveClick;
            scrollView.Add(itemControl);
        }
    }
}