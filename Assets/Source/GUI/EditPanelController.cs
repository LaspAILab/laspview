
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EditPanelController : MonoBehaviour
{
    public static EditPanelController Instance;

    [NonSerialized] public string ReplacedElement = null;
    
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

    private VisualElement elementRow;
    private TextField elementInput;
    private Button replaceElementButton;
    private Toggle fractionalToggle;
    private Vec3InputControl coordsInput;
    private Button addAtomButton;

    private FloatField displayRadiusInput;
    private TextField displayColorInput;

    void Awake()
    {
        Instance = this;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        elementRow = editPanelRoot.Q<VisualElement>("ElementRow");
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
        displayRadiusInput = editPanelRoot.Q<FloatField>("DisplayRadiusInput");
        displayRadiusInput.RegisterValueChangedCallback(OnRadiusSubmit);
        displayColorInput = editPanelRoot.Q<TextField>("DisplayColorInput");
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

            return;
        }

        // If multiple atoms selected, hide all controls
        activeAtom = null;
        
        multiselectLabel.style.display = DisplayStyle.Flex;
        fractionalToggle.style.display = DisplayStyle.None;
        coordsInput.style.display = DisplayStyle.None;
        addAtomButton.style.display = DisplayStyle.None;
        displayRadiusInput.style.display = DisplayStyle.None;
        displayColorInput.style.display = DisplayStyle.None;
    }

    public void OnElementSubmit(string element)
    {
        elementInput.SetValueWithoutNotify(element);
        if (activeAtom != null)
        {
            App.Instance.Viewport.SubmitOperation(new SetElementOperation(activeAtom, element));
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
    }

    public void EndReplaceAtom()
    {
        Debug.Log("End replace atom impl");
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
        
        App.Instance.Viewport.SubmitOperation(new SuperCellOperation((int)repeats.x, (int)repeats.y, (int)repeats.z));
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
        App.Instance.Viewport.SubmitOperation(new ChangeLatticeOperation(latticeParams, !keepFractionalToggle.value));
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

        App.Instance.Viewport.SubmitOperation(new SetPositionOperation(activeAtom, fractionalToggle.value
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
        App.Instance.Viewport.structureRenderer.RecollectDrawBatches();
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

        App.Instance.Viewport.SubmitOperation(new AddAtomOperation(fractionalToggle.value
            ? currentStructure.FractionalToCartesian(coords)
            : coords, element, 0));
    }
}