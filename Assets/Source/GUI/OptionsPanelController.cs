
using System;
using UnityEngine;
using UnityEngine.UIElements;

public enum BackgroundColorType
{
    Black,
    White,
    Grey,
    Blue,
}

public class OptionsPanelController : MonoBehaviour
{
    private VisualElement optionsPanelRoot;
    private ViewportController viewport;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        viewport = GameObject.Find("App Container").GetComponent<ViewportController>();
        optionsPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("OptionsPanel");

        var backgroundColorControl = optionsPanelRoot.Q<EnumField>("BackgroundColor");
        backgroundColorControl.RegisterValueChangedCallback(OnBackgroundColorChange);
        
        var playIntervalControl = optionsPanelRoot.Q<Slider>("PlayInterval");
        playIntervalControl.RegisterValueChangedCallback(OnPlayIntervalChange);
        
        var showLatticeControl = optionsPanelRoot.Q<Toggle>("ShowLattice");
        showLatticeControl.RegisterValueChangedCallback(OnShowLatticeChange);
        
        var latRangeAControl = optionsPanelRoot.Q<SliderInt>("LatRangeA");
        latRangeAControl.RegisterValueChangedCallback(OnLatRangeAChange);

        var latRangeBControl = optionsPanelRoot.Q<SliderInt>("LatRangeB");
        latRangeBControl.RegisterValueChangedCallback(OnLatRangeBChange);
        
        var latRangeCControl = optionsPanelRoot.Q<SliderInt>("LatRangeC");
        latRangeCControl.RegisterValueChangedCallback(OnLatRangeCChange);
        
        var lineWidthControl = optionsPanelRoot.Q<Slider>("LineWidth");
        lineWidthControl.RegisterValueChangedCallback(OnLineWidthChange);
        
        var displayStyleControl = optionsPanelRoot.Q<EnumField>("DisplayStyle");
        displayStyleControl.RegisterValueChangedCallback(OnDisplayStyleChange);
        
        var ballRadiusControl = optionsPanelRoot.Q<Slider>("BallRadius");
        ballRadiusControl.RegisterValueChangedCallback(OnBallRadiusChange);
        
        var stickRadiusControl = optionsPanelRoot.Q<Slider>("StickRadius");
        stickRadiusControl.RegisterValueChangedCallback(OnStickRadiusChange);
        
        var showHighlightControl = optionsPanelRoot.Q<Toggle>("ShowHighlight");
        showHighlightControl.RegisterValueChangedCallback(OnShowHighlightChange);
        
        var showHBondControl = optionsPanelRoot.Q<Toggle>("ShowHBonds");
        showHBondControl.RegisterValueChangedCallback(OnShowHBondChange);
        
        var bondToleranceControl = optionsPanelRoot.Q<Slider>("BondTolerance");
        bondToleranceControl.RegisterValueChangedCallback(OnBondToleranceChange);
        
        var bondPolicyControl = optionsPanelRoot.Q<EnumField>("BondPolicy");
        bondPolicyControl.RegisterValueChangedCallback(OnBondPolicyChange);
    }
    
    void OnBackgroundColorChange(ChangeEvent<Enum> e)
    {
        Camera mainCamera = Camera.main;
        switch (e.newValue)
        {
            case BackgroundColorType.Black:
                mainCamera.backgroundColor = Color.black;
                viewport.latticeRenderer.LineColor = Color.white;
                break;
            case BackgroundColorType.White:
                mainCamera.backgroundColor = Color.white;
                viewport.latticeRenderer.LineColor = Color.black;
                break;
            case BackgroundColorType.Grey:
                mainCamera.backgroundColor = Color.grey;
                viewport.latticeRenderer.LineColor = Color.white;
                break;
            case BackgroundColorType.Blue:
                mainCamera.backgroundColor = Color.darkBlue;
                viewport.latticeRenderer.LineColor = Color.white;
                break;
        }
    }

    void OnPlayIntervalChange(ChangeEvent<float> e)
    {
        StructureSequenceController.Instance.PlayInterval = e.newValue;
    }

    void OnShowLatticeChange(ChangeEvent<bool> e)
    {
        viewport.latticeRenderer.SetDisplay(e.newValue);
    }

    void OnLatRangeAChange(ChangeEvent<int> e)
    {
        viewport.SetLatticeRepeat(repeatA: e.newValue);
    }
    
    void OnLatRangeBChange(ChangeEvent<int> e)
    {
        viewport.SetLatticeRepeat(repeatB: e.newValue);
    }
    
    void OnLatRangeCChange(ChangeEvent<int> e)
    {
        viewport.SetLatticeRepeat(repeatC: e.newValue);
    }
    
    void OnLineWidthChange(ChangeEvent<float> e)
    {
        viewport.latticeRenderer.LineWidth = e.newValue;
    }

    void OnDisplayStyleChange(ChangeEvent<Enum> e)
    {
        switch (e.newValue)
        {
            case DisplayStyleType.BallsAndSticks:
                viewport.structureRenderer.SetDisplayType(true, true);
                break;
            case DisplayStyleType.OnlyBalls:
                viewport.structureRenderer.SetDisplayType(true, false);
                break;
            case DisplayStyleType.OnlySticks:
                viewport.structureRenderer.SetDisplayType(false, true);
                break;
        }
    }

    void OnBallRadiusChange(ChangeEvent<float> e)
    {
        viewport.structureRenderer.SetAtomRadius(e.newValue);
    }
    
    void OnStickRadiusChange(ChangeEvent<float> e)
    {
        viewport.structureRenderer.SetBondRadius(e.newValue);
    }

    void OnShowHighlightChange(ChangeEvent<bool> e)
    {
        viewport.structureRenderer.OutlineFrozenAtoms = e.newValue;
    }

    void OnShowHBondChange(ChangeEvent<bool> e)
    {
        viewport.hBondRenderer.SetDisplay(e.newValue);
    }

    void OnBondToleranceChange(ChangeEvent<float> e)
    {
        App.Instance.BondTolerance = e.newValue;
    }
    
    void OnBondPolicyChange(ChangeEvent<Enum> e)
    {
        App.Instance.BondCalculationPolicy = (CrossCellBondPolicy)e.newValue;
    }
}