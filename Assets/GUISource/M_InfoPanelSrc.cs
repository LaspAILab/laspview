using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ViewerNetwork;
using System.Threading.Tasks;

public class M_InfoPanelSrc : MonoBehaviour
{
    public static M_InfoPanelSrc S;
    public Text descriptionText;
    public Button closeButton;
    
    private InputField mainText;
    private Button openButton;
    private RectTransform rectTransform;
    private Camera uiCamera;

    void Awake()
    {
        S = this;

        uiCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        descriptionText.text = descriptionText.text.Replace("%%os", SystemInfo.operatingSystem);
        
        mainText = gameObject.GetComponentInChildren<InputField>();
        mainText.text = Resources.Load<TextAsset>("HelpText").text;
        
        closeButton.onClick.AddListener(CloseInfoPanel);
        
        openButton = GameObject.Find("InfoButton").GetComponent<Button>();
        openButton.onClick.AddListener(OpenInfoPanel);
        gameObject.SetActive(false);
    }

    void OpenInfoPanel()
    {
        var canvasHeight = GameObject.Find("Canvas").GetComponent<RectTransform>().rect.height;
        
        rectTransform.sizeDelta = new Vector2(450, canvasHeight * 0.8f);
        GuiMain.S.SetUIInteract(openButton, false);

        gameObject.SetActive(true);
    }

    void CloseInfoPanel()
    {
        GuiMain.S.SetUIInteract(openButton, true);
        gameObject.SetActive(false);
    }
    
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0) &&
            !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, uiCamera))
        {
            CloseInfoPanel();
        }
    }
}