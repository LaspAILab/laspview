
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class OptionsPanelController : MonoBehaviour
{
    private VisualElement optionsPanelRoot;
    private ViewportController viewport;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        viewport = GameObject.Find("App Container").GetComponent<ViewportController>();
        optionsPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("OptionsPanel");

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
        
        var displayStyleControl = optionsPanelRoot.Q<EnumField>("DisplayStyle");
        displayStyleControl.RegisterValueChangedCallback(OnDisplayStyleChange);
        
        var showHighlightControl = optionsPanelRoot.Q<Toggle>("ShowHighlight");
        showHighlightControl.RegisterValueChangedCallback(OnShowHighlightChange);
        
        var showHBondControl = optionsPanelRoot.Q<Toggle>("ShowHBond");
        showHBondControl.RegisterValueChangedCallback(OnShowHBondChange);
        
        var bondToleranceControl = optionsPanelRoot.Q<Slider>("BondTolerance");
        bondToleranceControl.RegisterValueChangedCallback(OnBondToleranceChange);
        
        var bondPolicyControl = optionsPanelRoot.Q<EnumField>("BondPolicy");
        bondPolicyControl.RegisterValueChangedCallback(OnBondPolicyChange);
    }

    void OnPlayIntervalChange(ChangeEvent<float> e)
    {
        
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