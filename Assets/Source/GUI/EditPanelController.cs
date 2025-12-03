
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class EditPanelController : MonoBehaviour
{
    public static EditPanelController Instance;
    
    [NonSerialized] public string ReplacedElement = null;
    
    private Camera mainCamera;
    private VisualElement editPanelRoot;
    private ViewportController viewport;
    private CrystalStructure currentStructure;

    private AtomView activeAtom;

    // Lattice section
    private Vec3InputControl latticeLengthsInput;
    private Vec3InputControl latticeAnglesInput;
    private Toggle keepFractionalToggle;
    private Button changeLatticeButton;
    
    private Vec3InputControl repeatsInput;
    private Button supercellButton;

    // Atom section
    private Label multiselectLabel;

    private TextField elementInput;
    private Button replaceElementButton;
    private Toggle fractionalToggle;
    private Vec3InputControl coordsInput;
    private Button addAtomButton;

    private VisualElement flagsRow;
    private Button clearFlagsButton;
    private Button fixedFlagButton;
    private Button reactFlagButton;
    
    // Move control
    private VisualElement moveTools;
    private Button moveUpButton;
    private Button moveDownButton;
    private Button moveLeftButton;
    private Button moveRightButton;
    private FloatField moveStepSizeInput;
    
    // Display section
    private FloatField displayRadiusInput;
    private TextField displayColorInput;
    private Button hideSelectedButton;
    private Button restoreAllButton;

    void Awake()
    {
        Instance = this;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        
        viewport = GameObject.Find("App Container").GetComponent<ViewportController>();
        editPanelRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("EditPanel");

        latticeLengthsInput = editPanelRoot.Q<Vec3InputControl>("LatticeLengthsInput");
        latticeAnglesInput = editPanelRoot.Q<Vec3InputControl>("LatticeAnglesInput");
        keepFractionalToggle = editPanelRoot.Q<Toggle>("KeepFractionalToggle");
        changeLatticeButton = editPanelRoot.Q<Button>("ChangeLatticeButton");
        changeLatticeButton.RegisterCallback<ClickEvent>(OnChangeLatticeClicked);
        repeatsInput = editPanelRoot.Q<Vec3InputControl>("RepeatsInput");
        supercellButton = editPanelRoot.Q<Button>("SupercellButton");
        supercellButton.RegisterCallback<ClickEvent>(OnSupercellClicked);
        
        multiselectLabel = editPanelRoot.Q<Label>("MultiselectLabel");
        var elementRow = editPanelRoot.Q<VisualElement>("ElementRow");
        elementInput = editPanelRoot.Q<TextField>("ElementInput");
        elementInput.RegisterCallback<ClickEvent>(OverlayUiController.Instance.ShowPeriodicTable);
        replaceElementButton = editPanelRoot.Q<Button>("ReplaceElementButton");
        replaceElementButton.RegisterCallback<ClickEvent>(OnReplaceElementClicked);
        fractionalToggle = editPanelRoot.Q<Toggle>("FractionalToggle");
        fractionalToggle.RegisterValueChangedCallback(OnFractionalToggleChanged);
        coordsInput = editPanelRoot.Q<Vec3InputControl>("CoordsInput");
        coordsInput.ValueChangedEvent += OnCoordsSubmit;
        
        addAtomButton = editPanelRoot.Q<Button>("AddAtomButton");
        addAtomButton.RegisterCallback<ClickEvent>(OnAddAtomClicked);
        
        flagsRow = editPanelRoot.Q<VisualElement>("FlagsRow");
        clearFlagsButton = editPanelRoot.Q<Button>("ClearFlagsButton");
        clearFlagsButton.RegisterCallback<ClickEvent>(OnSetFlagsClicked);
        fixedFlagButton = editPanelRoot.Q<Button>("FixedFlagButton");
        fixedFlagButton.RegisterCallback<ClickEvent>(OnSetFlagsClicked);
        reactFlagButton = editPanelRoot.Q<Button>("ReactFlagButton");
        reactFlagButton.RegisterCallback<ClickEvent>(OnSetFlagsClicked);
        
        moveTools = editPanelRoot.Q<VisualElement>("MoveTools");
        moveUpButton = editPanelRoot.Q<Button>("MoveUpButton");
        moveUpButton.RegisterCallback<ClickEvent>(OnMoveButtonClicked);
        moveDownButton = editPanelRoot.Q<Button>("MoveDownButton");
        moveDownButton.RegisterCallback<ClickEvent>(OnMoveButtonClicked);
        moveLeftButton = editPanelRoot.Q<Button>("MoveLeftButton");
        moveLeftButton.RegisterCallback<ClickEvent>(OnMoveButtonClicked);
        moveRightButton = editPanelRoot.Q<Button>("MoveRightButton");
        moveRightButton.RegisterCallback<ClickEvent>(OnMoveButtonClicked);
        moveStepSizeInput = editPanelRoot.Q<FloatField>("MoveStepSizeInput");
        
        displayRadiusInput = editPanelRoot.Q<FloatField>("DisplayRadiusInput");
        displayRadiusInput.RegisterValueChangedCallback(OnRadiusSubmit);
        displayColorInput = editPanelRoot.Q<TextField>("DisplayColorInput");
        hideSelectedButton = editPanelRoot.Q<Button>("HideSelectedButton");
        hideSelectedButton.RegisterCallback<ClickEvent>(OnHideSelectedClicked);
        restoreAllButton = editPanelRoot.Q<Button>("RestoreAllButton");
        restoreAllButton.RegisterCallback<ClickEvent>(OnRestoreAllClicked);
    }

    public void UpdateSelectionInfo(List<SelectionItem> selection)
    {
        if (selection.Count <= 1)
        {
            fractionalToggle.style.display = DisplayStyle.Flex;
            coordsInput.style.display = DisplayStyle.Flex;
            multiselectLabel.style.display = DisplayStyle.None;
        }
        
        if (selection.Count == 0)
        {
            activeAtom = null;
            
            // If no atom selected, show the add atom button
            addAtomButton.style.display = DisplayStyle.Flex;
            // Hide display property inputs
            displayRadiusInput.style.display = DisplayStyle.None;
            displayColorInput.style.display = DisplayStyle.None;
            flagsRow.style.display = DisplayStyle.None;
            moveTools.style.display = DisplayStyle.None;
            return;
        }

        if (selection.Count == 1)
        {
            activeAtom = selection[0].Atom;
            UpdateActiveAtom();
            
            // If single atom selected, show display atom properties
            addAtomButton.style.display = DisplayStyle.None;
            displayRadiusInput.style.display = DisplayStyle.Flex;
            displayColorInput.style.display = DisplayStyle.Flex;
            flagsRow.style.display = DisplayStyle.Flex;
            moveTools.style.display = DisplayStyle.Flex;

            return;
        }

        // If multiple atoms selected, hide all controls
        activeAtom = null;
        
        fractionalToggle.style.display = DisplayStyle.None;
        coordsInput.style.display = DisplayStyle.None;
        addAtomButton.style.display = DisplayStyle.None;
        displayRadiusInput.style.display = DisplayStyle.None;
        displayColorInput.style.display = DisplayStyle.None;
        
        moveTools.style.display = DisplayStyle.Flex;
        multiselectLabel.style.display = DisplayStyle.Flex;
        flagsRow.style.display = DisplayStyle.Flex;
    }

    public void OnElementSubmit(string element)
    {
        elementInput.SetValueWithoutNotify(element);
        if (activeAtom != null)
        {
            App.Instance.operationManager.SubmitOperation(new SetElementOperation(activeAtom, element));
        }
        
        OverlayUiController.Instance.ClosePeriodicTable(null);
    }

    public void UpdateLattice(CrystalStructure structure)
    {
        currentStructure = structure;
        latticeLengthsInput.SetValueWithoutNotify(new Vector3(structure.LatticeParams[0], structure.LatticeParams[1], structure.LatticeParams[2]));
        latticeAnglesInput.SetValueWithoutNotify(new Vector3(structure.LatticeParams[3], structure.LatticeParams[4], structure.LatticeParams[5]));
    }

    public void UpdateActiveAtom()
    {
        if (activeAtom == null)
        {
            return;
        }
        
        elementInput.SetValueWithoutNotify(activeAtom.Symbol);
        
        coordsInput.SetValueWithoutNotify(fractionalToggle.value
            ? currentStructure.CartesianToFractional(activeAtom.SourceAtom.Coordinates)
            : activeAtom.SourceAtom.Coordinates);
        
        displayRadiusInput.SetValueWithoutNotify(activeAtom.Radius);
        displayColorInput.SetValueWithoutNotify(activeAtom.DisplayColor.ToString());
    }

    public void EndReplaceAtom()
    {
        replaceElementButton.RemoveFromClassList("active-edit-panel-button");
        ReplacedElement = null;
    }
    
    void OnSupercellClicked(ClickEvent evt)
    {
        Vector3 repeats = repeatsInput.value;

        if (repeats.x <= 0 || repeats.y <= 0 || repeats.z <= 0)
        {
            OverlayUiController.Instance.PrintStatusLog("Repeat count must be greater than zero");
            return;
        }
        
        App.Instance.operationManager.SubmitOperation(new SuperCellOperation((int)repeats.x, (int)repeats.y, (int)repeats.z));
    }
    
    void OnChangeLatticeClicked(ClickEvent evt)
    {
        Vector3 lengths = latticeLengthsInput.value;
        Vector3 angles = latticeAnglesInput.value;

        if (lengths.x <= 0 || lengths.y <= 0 || lengths.z <= 0)
        {
            OverlayUiController.Instance.PrintStatusLog("Invalid lattice length");
            return;
        }
        
        if (angles.x <= 0 || angles.y <= 0 || angles.z <= 0 || angles.x >= 180 || angles.y >= 180 || angles.z >= 180)
        {
            OverlayUiController.Instance.PrintStatusLog("Invalid lattice angle");
            return;
        }

        float[] latticeParams = { lengths.x, lengths.y, lengths.z, angles.x, angles.y, angles.z };
        App.Instance.operationManager.SubmitOperation(new ChangeLatticeOperation(latticeParams, !keepFractionalToggle.value));
    }
    
    void OnFractionalToggleChanged(ChangeEvent<bool> evt)
    {
        UpdateActiveAtom();
    }

    void OnReplaceElementClicked(ClickEvent evt)
    {
        if (ReplacedElement == null)
        {
            replaceElementButton.AddToClassList("active-edit-panel-button");
            ReplacedElement = elementInput.value;
            viewport.BeginReplaceAtom();
        }
        else
        {
            viewport.EndReplaceAtom();
        }
    }

    void OnCoordsSubmit(VisualElement sender, Vector3 newValue)
    {
        if (activeAtom == null)
        {
            return;
        }
        
        Vector3 coords = coordsInput.value;

        App.Instance.operationManager.SubmitOperation(new SetPositionOperation(activeAtom, fractionalToggle.value
            ? currentStructure.FractionalToCartesian(coords)
            : coords));
    }
    
    void OnRadiusSubmit(ChangeEvent<float> evt)
    {
        if (activeAtom == null)
        {
            return;
        }
        
        if (evt.newValue < 0){
            OverlayUiController.Instance.PrintStatusLog("Invalid radius");
            return;
        }
        
        activeAtom.Radius = evt.newValue;
        ColliderManager.Instance.UpdateColliderForAtom(activeAtom);
        App.Instance.viewport.structureRenderer.RecollectDrawBatches();
    }

    void OnAddAtomClicked(ClickEvent evt)
    {
        Vector3 coords = coordsInput.value;

        string element = elementInput.value;
        if (element == null || !App.Elements.ContainsKey(element))
        {
            OverlayUiController.Instance.PrintStatusLog($"Invalid element {element}, cannot add atom! ");
            elementInput.SetValueWithoutNotify("H");
            return;
        }

        App.Instance.operationManager.SubmitOperation(new AddAtomOperation(fractionalToggle.value
            ? currentStructure.FractionalToCartesian(coords)
            : coords, element, 0));
    }

    void OnSetFlagsClicked(ClickEvent evt)
    {
        if (App.Instance.viewport.CurrentStructureView == null)
        {
            return;
        }
        
        if(evt.target is not Button button)
        {
            return;
        }
        
        AtomFlags flags = button.text switch
        {
            "None" => AtomFlags.None,
            "Fixed" => AtomFlags.Fixed,
            "React" => AtomFlags.React,
            _ => AtomFlags.None,
        };

        Assert.IsFalse(App.Instance.viewport.selectionManager.IsEmpty);
        App.Instance.operationManager.SubmitOperation(
            new SetFlagOperation(App.Instance.viewport.selectionManager.SelectedAtoms, flags));
    }

    void OnMoveButtonClicked(ClickEvent evt)
    {
        if (evt.target is Button btn)
        {
            Vector3 moveDirection = btn.name switch
            {
                "MoveUpButton" => Vector3.up,
                "MoveDownButton" => Vector3.down,
                "MoveLeftButton" => Vector3.left,
                "MoveRightButton" => Vector3.right,
                _ => Vector3.zero
            };

            // Transform to screen space
            moveDirection = mainCamera.transform.TransformDirection(moveDirection);
            moveDirection *= moveStepSizeInput.value;
            
            App.Instance.operationManager.SubmitOperation(
                new TranslateAtomsOperation(App.Instance.viewport.selectionManager.SelectedAtoms, moveDirection));
        }
    }
    
    void OnHideSelectedClicked(ClickEvent evt)
    {
        App.Instance.operationManager.SubmitOperation(
            new SetHiddenOperation(App.Instance.viewport.selectionManager.SelectedAtoms, true));
    }
    
    void OnRestoreAllClicked(ClickEvent evt)
    {
        // Make all atom displayed again
        App.Instance.operationManager.SubmitOperation(
            new SetHiddenOperation(App.Instance.viewport.CurrentStructureView.Atoms, false));
    }
}