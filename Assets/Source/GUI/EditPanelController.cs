using System;
using System.Collections.Generic;
using System.Linq;
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

    // The only selected atom (non-null only if the selection is unique)
    private AtomView activeAtom;

    // Saved for bond angle or dihedral editing
    private SelectionItem[] selectedAtoms;

    // For bond angle & dihedral editing preview
    private PreviewTracker previewTracker = null;

    // Lattice section
    private Label latticeInfoLabel;
    private Button changeLatticeButton;
    private Button transformLatticeButton;

    // Atom section
    private Label multiselectLabel;

    private TextField elementInput;
    private Button replaceElementButton;
    private Toggle fractionalToggle;
    private Vec3InputControl coordsInput;
    private Button addAtomButton;

    private VisualElement elementRow;
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

    // Angle related
    private Slider bondLengthSlider;
    private Slider bondAngleSlider;
    private Slider dihedralSlider;
    private Button confirmAngleButton;

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

        latticeInfoLabel = editPanelRoot.Q<Label>("LatticeInfoLabel");
        changeLatticeButton = editPanelRoot.Q<Button>("ChangeLatticeButton");
        changeLatticeButton.RegisterCallback<ClickEvent>(InvokeChangeLatticeWindow);
        transformLatticeButton = editPanelRoot.Q<Button>("TransformLatticeButton");
        transformLatticeButton.RegisterCallback<ClickEvent>(InvokeTransformLatticeWindow);

        multiselectLabel = editPanelRoot.Q<Label>("MultiselectLabel");
        elementRow = editPanelRoot.Q<VisualElement>("ElementRow");
        elementInput = editPanelRoot.Q<TextField>("ElementInput");
        elementInput.RegisterCallback<ClickEvent>(InvokePeriodicTable);
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

        bondLengthSlider = editPanelRoot.Q<Slider>("BondLengthSlider");
        bondLengthSlider.RegisterValueChangedCallback(OnEditBondLength);
        bondAngleSlider = editPanelRoot.Q<Slider>("BondAngleSlider");
        bondAngleSlider.RegisterValueChangedCallback(OnEditBondAngle);
        dihedralSlider = editPanelRoot.Q<Slider>("DihedralSlider");
        dihedralSlider.RegisterValueChangedCallback(OnEditDihedral);
        confirmAngleButton = editPanelRoot.Q<Button>("ConfirmAngleButton");
        confirmAngleButton.RegisterCallback<ClickEvent>(OnConfirmAngleButtonClicked);
    }

    public void UpdateSelectionInfo(List<SelectionItem> selection)
    {
        if (previewTracker != null)
        {
            previewTracker.Reset();
            previewTracker = null;
        }

        if (selection.Count <= 1)
        {
            fractionalToggle.style.display = DisplayStyle.Flex;
            coordsInput.style.display = DisplayStyle.Flex;
            multiselectLabel.style.display = DisplayStyle.None;
        }

        bondLengthSlider.style.display = selection.Count == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        bondAngleSlider.style.display = selection.Count == 3 ? DisplayStyle.Flex : DisplayStyle.None;
        dihedralSlider.style.display = selection.Count == 4 ? DisplayStyle.Flex : DisplayStyle.None;
        confirmAngleButton.style.display = selection.Count is >=2 and <=4 ? DisplayStyle.Flex : DisplayStyle.None;

        if (selection.Count == 0)
        {
            activeAtom = null;

            // If no atom selected, show the add atom button
            addAtomButton.style.display = DisplayStyle.Flex;
            elementRow.style.display = DisplayStyle.Flex;
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
            elementRow.style.display = DisplayStyle.Flex;

            return;
        }

        if (selection.Count == 2)
        {
            var sortedSelection = selection.OrderBy(x => x.SelectIndex).ToList();
            // Omit SwapYZ here as it does not affect the results
            var angle = Vector3.Distance(sortedSelection[0].Atom.SourceAtom.Coordinates,
                sortedSelection[1].Atom.SourceAtom.Coordinates);
            bondLengthSlider.SetValueWithoutNotify(angle);
            selectedAtoms = sortedSelection.ToArray();
            previewTracker = new PreviewTracker(App.Instance.viewport.Api, new[] { sortedSelection[1].Atom });
        }

        if (selection.Count == 3)
        {
            var sortedSelection = selection.OrderBy(x => x.SelectIndex).ToList();
            // Omit SwapYZ here as it does not affect the results
            var angle = SpatialUtils.GetBondAngle(sortedSelection[0].Atom.SourceAtom.Coordinates,
                sortedSelection[1].Atom.SourceAtom.Coordinates, sortedSelection[2].Atom.SourceAtom.Coordinates);
            bondAngleSlider.SetValueWithoutNotify(angle);
            selectedAtoms = sortedSelection.ToArray();
            previewTracker = new PreviewTracker(App.Instance.viewport.Api, new[] { sortedSelection[2].Atom });
        }

        if (selection.Count == 4)
        {
            var sortedSelection = selection.OrderBy(x => x.SelectIndex).ToList();
            // Omit SwapYZ here as it does not affect the results
            var angle = SpatialUtils.GetDihedral(sortedSelection[0].Atom.SourceAtom.Coordinates,
                sortedSelection[1].Atom.SourceAtom.Coordinates, sortedSelection[2].Atom.SourceAtom.Coordinates,
                sortedSelection[3].Atom.SourceAtom.Coordinates);
            dihedralSlider.SetValueWithoutNotify(angle);
            selectedAtoms = sortedSelection.ToArray();
            previewTracker = new PreviewTracker(App.Instance.viewport.Api, new[] { sortedSelection[3].Atom });
        }

        // If multiple atoms selected, hide all controls
        activeAtom = null;

        elementRow.style.display = DisplayStyle.None;
        fractionalToggle.style.display = DisplayStyle.None;
        coordsInput.style.display = DisplayStyle.None;
        addAtomButton.style.display = DisplayStyle.None;
        displayRadiusInput.style.display = DisplayStyle.None;
        displayColorInput.style.display = DisplayStyle.None;

        moveTools.style.display = DisplayStyle.Flex;
        multiselectLabel.style.display = DisplayStyle.Flex;
        flagsRow.style.display = DisplayStyle.Flex;
    }

    void InvokePeriodicTable(ClickEvent evt)
    {
        var element =
            OverlayUiController.Instance.ShowPopupWindow("Periodic Table",
                OverlayUiController.Instance.PeriodicTableTemplate);
        var periodicTableControl = element.Q<PeriodicTableControl>();
        periodicTableControl.ElementClickedEvent += OnElementSubmit;
    }

    void InvokeChangeLatticeWindow(ClickEvent evt)
    {
        var element = OverlayUiController.Instance.ShowPopupWindow("Change Lattice",
            OverlayUiController.Instance.ChangeLatticeWindowTemplate);
        var latticeLengthsInput = element.Q<Vec3InputControl>("LatticeLengthsInput");
        var latticeAnglesInput = element.Q<Vec3InputControl>("LatticeAnglesInput");
        var keepFractionalToggle = element.Q<Toggle>("KeepFractionalToggle");

        latticeLengthsInput.value = new Vector3(currentStructure.LatticeParams[0], currentStructure.LatticeParams[1],
            currentStructure.LatticeParams[2]);
        latticeAnglesInput.value = new Vector3(currentStructure.LatticeParams[3], currentStructure.LatticeParams[4],
            currentStructure.LatticeParams[5]);

        element.Q<Button>("ChangeLatticeButton").RegisterCallback<ClickEvent>(_ =>
            OnChangeLatticeClicked(latticeLengthsInput, latticeAnglesInput, keepFractionalToggle));
    }

    void InvokeTransformLatticeWindow(ClickEvent evt)
    {
        var element = OverlayUiController.Instance.ShowPopupWindow("Transform Lattice",
            OverlayUiController.Instance.TransformLatticeWindowTemplate);
        var matrixInputLine1 = element.Q<Vec3InputControl>("MatrixLine1");
        var matrixInputLine2 = element.Q<Vec3InputControl>("MatrixLine2");
        var matrixInputLine3 = element.Q<Vec3InputControl>("MatrixLine3");

        element.Q<Button>("TransformLatticeButton").RegisterCallback<ClickEvent>(_ =>
            OnTransformLatticeClicked(new[] { matrixInputLine1, matrixInputLine2, matrixInputLine3 }));
    }

    void OnElementSubmit(string element)
    {
        elementInput.SetValueWithoutNotify(element);
        if (activeAtom != null)
        {
            App.Instance.operationManager.SubmitOperation(new SetElementOperation(activeAtom, element));
        }

        OverlayUiController.Instance.ClosePopupWindow(null);
    }

    void OnChangeLatticeClicked(Vec3InputControl latticeLengthsInput, Vec3InputControl latticeAnglesInput,
        Toggle keepFractionalToggle)
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
        App.Instance.operationManager.SubmitOperation(new ChangeLatticeOperation(latticeParams,
            !keepFractionalToggle.value));
        OverlayUiController.Instance.ClosePopupWindow(null);
    }

    void OnTransformLatticeClicked(Vec3InputControl[] matrixInput)
    {
        Matrix4x4 matrix = new(new Vector4(matrixInput[0].value.x, matrixInput[0].value.y, matrixInput[0].value.z, 0),
            new Vector4(matrixInput[1].value.x, matrixInput[1].value.y, matrixInput[1].value.z, 0),
            new Vector4(matrixInput[2].value.x, matrixInput[2].value.y, matrixInput[2].value.z, 0),
            new Vector4(0, 0, 0, 1));

        App.Instance.CurrentFile.Structures.Add(currentStructure.TransformLattice(matrix));
        OverlayUiController.Instance.UpdateStructureIndex(null, App.Instance.CurrentFile.StructureCount);
        StructureSequenceController.Instance.SwitchStructure(StructureSequenceController.Instance.StructureCount - 1);
        OverlayUiController.Instance.ClosePopupWindow(null);
    }


    public void UpdateLattice(CrystalStructure structure)
    {
        currentStructure = structure;
        latticeInfoLabel.text =
            $"a={structure.LatticeParams[0]:F2}Å   b={structure.LatticeParams[1]:F2}Å   c={structure.LatticeParams[2]:F2}Å\nα={structure.LatticeParams[3]:F2}°   β={structure.LatticeParams[4]:F2}°   γ={structure.LatticeParams[5]:F2}°";
    }

    public void UpdateActiveAtom()
    {
        if (activeAtom == null)
        {
            return;
        }

        elementInput.SetValueWithoutNotify(activeAtom.Symbol);

        Vector3 coordsRH = CrystalStructureView.SwapYZ(activeAtom.Position);
        coordsInput.SetValueWithoutNotify(fractionalToggle.value
            ? currentStructure.CartesianToFractional(coordsRH)
            : coordsRH);

        displayRadiusInput.SetValueWithoutNotify(activeAtom.Radius);
        displayColorInput.SetValueWithoutNotify(activeAtom.DisplayColor.ToString());
    }

    public void EndReplaceAtom()
    {
        replaceElementButton.RemoveFromClassList("active-edit-panel-button");
        ReplacedElement = null;
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

        Vector3 coordsRH = fractionalToggle.value
            ? currentStructure.FractionalToCartesian(coordsInput.value)
            : coordsInput.value;

        // Swap y and z to convert from right-handed to left-handed coordinate system
        App.Instance.operationManager.SubmitOperation(new SetPositionOperation(activeAtom,
            CrystalStructureView.SwapYZ(coordsRH)));
    }

    void OnRadiusSubmit(ChangeEvent<float> evt)
    {
        if (activeAtom == null)
        {
            return;
        }

        if (evt.newValue < 0)
        {
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

        if (evt.target is not Button button)
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

            var atomsToMove = App.Instance.viewport.selectionManager.SelectedAtoms;
            var newPositions =
                atomsToMove.Select(x => CrystalStructureView.SwapYZ(x.SourceAtom.Coordinates) + moveDirection).ToList();

            App.Instance.operationManager.SubmitOperation(
                new RepositionAtomsOperation(atomsToMove, newPositions));
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

    void OnEditBondLength(ChangeEvent<float> evt)
    {
        var newPos = SpatialUtils.SetBondLength(
            CrystalStructureView.SwapYZ(selectedAtoms[0].Atom.SourceAtom.Coordinates),
            CrystalStructureView.SwapYZ(selectedAtoms[1].Atom.SourceAtom.Coordinates),
            evt.newValue);
        previewTracker.PositionBuffer[0] = newPos;
        previewTracker.UpdatePositions(true);
    }

    void OnEditBondAngle(ChangeEvent<float> evt)
    {
        var newPos = SpatialUtils.SetBondAngle(
            CrystalStructureView.SwapYZ(selectedAtoms[0].Atom.SourceAtom.Coordinates),
            CrystalStructureView.SwapYZ(selectedAtoms[1].Atom.SourceAtom.Coordinates),
            CrystalStructureView.SwapYZ(selectedAtoms[2].Atom.SourceAtom.Coordinates), evt.newValue);
        previewTracker.PositionBuffer[0] = newPos;
        previewTracker.UpdatePositions(true);
    }

    void OnEditDihedral(ChangeEvent<float> evt)
    {
        var newPos = SpatialUtils.SetDihedral(CrystalStructureView.SwapYZ(selectedAtoms[0].Atom.SourceAtom.Coordinates),
            CrystalStructureView.SwapYZ(selectedAtoms[1].Atom.SourceAtom.Coordinates),
            CrystalStructureView.SwapYZ(selectedAtoms[2].Atom.SourceAtom.Coordinates),
            CrystalStructureView.SwapYZ(selectedAtoms[3].Atom.SourceAtom.Coordinates), evt.newValue);
        previewTracker.PositionBuffer[0] = newPos;
        previewTracker.UpdatePositions(true);
    }

    void OnConfirmAngleButtonClicked(ClickEvent evt)
    {
        if (previewTracker != null)
        {
            previewTracker.Submit();
            previewTracker = null;
        }
    }
}