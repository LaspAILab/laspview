using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class R_AtomEditPanelSrc : MonoBehaviour
{
    public static R_AtomEditPanelSrc S;

    public VectorInputController coordsInput;
    public InputField elementInput;
    public Toggle fractionalToggle;
    public ScalarInputController radiusInput;
    public ColorPickerController colorPickerController;

    public GameObject label, edits;
    public GameObject addAtomButton, fixAtomPanel;

    private Text textLabel;
    private AtomView activeAtom;

    private CrystalStructure currentStructure;

    void Start()
    {
        S = this;

        //Set Edit panel
        this.transform.SetParent(GuiMain.S.rightCanvas.transform, false);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button edButton = GameObject.Find("EditBtn").GetComponent<Button>();
        edButton.onClick.AddListener(() => { GuiMain.S.BtnHideObject(edButton, gameObject); });

        textLabel = label.GetComponent<Text>();
        coordsInput.SetOnChangeCallback(UpdateCoords);
        radiusInput.SetOnChangeCallback(UpdateRadius);

        gameObject.SetActive(false);
    }

    public void SetCurrentStructure(CrystalStructure crystalStructure)
    {
        currentStructure = crystalStructure;
        OnNoSelection();
    }

    public void OnNoSelection()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        label.SetActive(true);
        edits.SetActive(true);
        addAtomButton.SetActive(true);
        fixAtomPanel.SetActive(false);
        radiusInput.gameObject.SetActive(false);
        colorPickerController.gameObject.SetActive(false);

        if (currentStructure != null)
        {
            textLabel.text = $"{currentStructure.Atoms.Count} atoms within single lattice";
        }

        coordsInput.SetValue(0.5f, 0.5f, 0.5f);
        activeAtom = null;
    }

    public void UpdateSelection(List<SelectionItem> selection)
    {
        if (selection.Count == 0)
        {
            OnNoSelection();
            return;
        }

        fixAtomPanel.SetActive(true);
        if (selection.Count > 1)
        {
            label.SetActive(true);
            textLabel.text = $"{selection.Count} atoms selected";
            edits.SetActive(false);
            addAtomButton.SetActive(false);
            radiusInput.gameObject.SetActive(false);
            colorPickerController.gameObject.SetActive(false);
            activeAtom = null;
            return;
        }

        // One selection
        activeAtom = selection[0].Atom;
        label.SetActive(false);
        edits.SetActive(true);
        addAtomButton.SetActive(false);
        radiusInput.gameObject.SetActive(true);
        colorPickerController.gameObject.SetActive(true);
        DisplayActiveAtomProperties();
    }

    public void DisplayActiveAtomProperties()
    {
        if (activeAtom == null)
        {
            return;
        }
        
        elementInput.SetTextWithoutNotify(activeAtom.SourceAtom.Symbol);

        coordsInput.SetValue(fractionalToggle.isOn
            ? currentStructure.CartesianToFractional(activeAtom.SourceAtom.Coordinates)
            : activeAtom.SourceAtom.Coordinates);

        radiusInput.SetValue(activeAtom.Radius);
        colorPickerController.SetColor(activeAtom.DisplayColor);
        colorPickerController.RegisterCallback((color) =>
        {
            activeAtom.DisplayColor = color;
            App.Instance.Viewport.structureRenderer.RecollectDrawBatches();
        });
    }

    private static string ToElementSymbol(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return null;
        }
        
        return char.ToUpper(rawText[0]) + rawText.Substring(1).ToLower();
    }

    public void UpdateElement()
    {
        string element = ToElementSymbol(elementInput.text);
        if (activeAtom == null || activeAtom.SourceAtom.Symbol == element)
        {
            return;
        }

        if (element == null || !App.Elements.ContainsKey(element))
        {
            ConFun.UpdateStatusLabel($"Invalid element {element}, ignoring! ");
            elementInput.SetTextWithoutNotify(activeAtom.SourceAtom.Symbol);
            return;
        }

        elementInput.SetTextWithoutNotify(element);
        App.Instance.Viewport.SubmitOperation(new SetElementOperation(activeAtom, element));
    }

    void UpdateCoords()
    {
        if (activeAtom == null)
        {
            return;
        }

        Vector3? coords = coordsInput.GetValue();
        if (coords == null)
        {
            ConFun.UpdateStatusLabel("Invalid position");
            return;
        }

        App.Instance.Viewport.SubmitOperation(new SetPositionOperation(activeAtom, fractionalToggle.isOn
            ? currentStructure.FractionalToCartesian(coords.Value)
            : coords.Value));
    }

    void UpdateRadius()
    {
        if (activeAtom == null)
        {
            return;
        }

        if (!float.TryParse(radiusInput.GetValue(), out float radius) || radius <= 0)
        {
            ConFun.UpdateStatusLabel("Invalid charge");
            return;
        }

        activeAtom.Radius = radius;
        ColliderManager.Instance.UpdateColliderForAtom(activeAtom);
        App.Instance.Viewport.structureRenderer.RecollectDrawBatches();
    }

    public void FixAtoms(bool isFixed)
    {
        if (App.Instance.Viewport.CurrentStructureView == null)
        {
            return;
        }

        Assert.IsFalse(App.Instance.Viewport.selectionManager.IsEmpty);
        App.Instance.Viewport.SubmitOperation(
            new SetFixedOperation(App.Instance.Viewport.selectionManager.SelectedAtoms, isFixed));
    }

    public void ToggleFractionalCoordinates()
    {
        DisplayActiveAtomProperties();
    }

    public void AddAtom()
    {
        Vector3? coords = coordsInput.GetValue();
        if (coords == null)
        {
            ConFun.UpdateStatusLabel("Invalid position");
            return;
        }

        string element = ToElementSymbol(elementInput.text);
        if (element == null || !App.Elements.ContainsKey(element))
        {
            ConFun.UpdateStatusLabel($"Invalid element {element}, cannot add atom! ");
            elementInput.SetTextWithoutNotify("H");
            return;
        }

        App.Instance.Viewport.SubmitOperation(new AddAtomOperation(fractionalToggle.isOn
            ? currentStructure.FractionalToCartesian(coords.Value)
            : coords.Value, element, 0));
    }
}