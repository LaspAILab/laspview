
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

    private EnumField lineColorControl;
    private Slider dashDensityControl;
    private SliderInt cameraAzimuthControl;
    private SliderInt cameraAltitudeControl;
    
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
        
        lineColorControl = optionsPanelRoot.Q<EnumField>("LineColor");
        lineColorControl.RegisterValueChangedCallback(OnLineColorChange);
        
        var useDottedLinesControl = optionsPanelRoot.Q<Toggle>("DottedLines");
        useDottedLinesControl.RegisterValueChangedCallback(OnUseDottedLinesChange);
        
        dashDensityControl = optionsPanelRoot.Q<Slider>("DashDensity");
        dashDensityControl.RegisterValueChangedCallback(OnDashDensityChange);
        
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
        
        var showSurfaceControl = optionsPanelRoot.Q<Toggle>("ShowSurface");
        showSurfaceControl.RegisterValueChangedCallback(OnShowSurfaceChange);
        
        var surfaceThresholdControl = optionsPanelRoot.Q<Slider>("SurfaceThreshold");
        surfaceThresholdControl.RegisterValueChangedCallback(OnSurfaceThresholdChange);
        
        cameraAzimuthControl = optionsPanelRoot.Q<SliderInt>("CameraAzimuth");
        cameraAzimuthControl.RegisterValueChangedCallback(OnCameraParametersChange);
        
        cameraAltitudeControl = optionsPanelRoot.Q<SliderInt>("CameraAltitude");
        cameraAltitudeControl.RegisterValueChangedCallback(OnCameraParametersChange);
    }
    
    void OnBackgroundColorChange(ChangeEvent<Enum> e)
    {
        Camera mainCamera = Camera.main;
        switch (e.newValue)
        {
            case BackgroundColorType.Black:
                mainCamera.backgroundColor = Color.black;
                lineColorControl.value = BackgroundColorType.White;
                break;
            case BackgroundColorType.White:
                mainCamera.backgroundColor = Color.white;
                lineColorControl.value = BackgroundColorType.Black;
                break;
            case BackgroundColorType.Grey:
                mainCamera.backgroundColor = Color.grey;
                lineColorControl.value = BackgroundColorType.White;
                break;
            case BackgroundColorType.Blue:
                mainCamera.backgroundColor = Color.darkBlue;
                lineColorControl.value = BackgroundColorType.White;
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
    
    void OnLineColorChange(ChangeEvent<Enum> e)
    {
        switch (e.newValue)
        {
            case BackgroundColorType.Black:
                viewport.latticeRenderer.LineColor = Color.black;
                break;
            case BackgroundColorType.White:
                viewport.latticeRenderer.LineColor = Color.white;
                break;
            case BackgroundColorType.Grey:
                viewport.latticeRenderer.LineColor = Color.grey;
                break;
            case BackgroundColorType.Blue:
                viewport.latticeRenderer.LineColor = Color.darkBlue;
                break;
        }
    }
    
    void OnUseDottedLinesChange(ChangeEvent<bool> e)
    {
        viewport.latticeRenderer.UseDottedLines = e.newValue;

        if (e.newValue)
        {
            dashDensityControl.style.display = DisplayStyle.Flex;
        }
        else
        {
            dashDensityControl.style.display = DisplayStyle.None;
        }
    }
    
    void OnDashDensityChange(ChangeEvent<float> e)
    {
        viewport.latticeRenderer.DashScale = 1f / e.newValue;
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
    
    void OnShowSurfaceChange(ChangeEvent<bool> e)
    {
        SurfaceRenderer.Instance.SetDisplay(e.newValue);
    }
    
    void OnSurfaceThresholdChange(ChangeEvent<float> e)
    {
        SurfaceRenderer.Instance.SurfaceValue = Mathf.Pow(10f, e.newValue);
    }
    
    void OnCameraParametersChange(ChangeEvent<int> e)
    {
        viewport.SetCameraAngles(cameraAzimuthControl.value, cameraAltitudeControl.value);
    }
}