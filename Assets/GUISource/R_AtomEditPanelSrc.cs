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
    public Dropdown elementDropdown;
    public Toggle fractionalToggle;

    public GameObject label, edits;
    public GameObject addAtomButton, fixAtomPanel;

    private Text textLabel;
    private AtomView activeAtom;

    private ArcStructure currentArc;

    void Start()
    {
        S = this;

        //Set Edit panel
        this.transform.SetParent(GuiMain.S.rightCanvas.transform, false);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button edButton = GameObject.Find("EditBtn").GetComponent<Button>();
        edButton.onClick.AddListener(() => { GuiMain.S.BtnHideObject(edButton, gameObject); });

        foreach (var kv in App.Elements)
        {
            elementDropdown.options.Add(new Dropdown.OptionData(kv.Key));
        }

        textLabel = label.GetComponent<Text>();
        coordsInput.SetOnChangeCallback(UpdateCoords);

        gameObject.SetActive(false);
    }

    public void SetCurrentStructure(ArcStructure arcStructure)
    {
        currentArc = arcStructure;
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

        if (currentArc != null)
        {
            textLabel.text = $"{currentArc.Atoms.Count} atoms within single lattice";
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
            activeAtom = null;
            return;
        }

        // One selection
        activeAtom = selection[0].Atom;
        label.SetActive(false);
        edits.SetActive(true);
        addAtomButton.SetActive(false);
        DisplayActiveAtomProperties();
    }

    public void DisplayActiveAtomProperties()
    {
        if (activeAtom == null)
        {
            textLabel.text = $"{currentArc.Atoms.Count} atoms within single lattice";
            return;
        }

        elementDropdown.value = elementDropdown.options.FindIndex(x => x.text == activeAtom.SourceAtom.Symbol);

        coordsInput.SetValue(fractionalToggle.isOn
            ? currentArc.CartesianToFractional(activeAtom.SourceAtom.Coordinates)
            : activeAtom.SourceAtom.Coordinates);
    }

    public void UpdateElement()
    {
        string element = elementDropdown.options[elementDropdown.value].text;
        if (activeAtom == null || activeAtom.SourceAtom.Symbol == element)
        {
            return;
        }

        Assert.IsTrue(App.Elements.ContainsKey(element));
        App.Instance.Viewport.SubmitOperation(new SetElementOperation(activeAtom, element));
    }

    public void UpdateCoords()
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
            ? currentArc.FractionalToCartesian(coords.Value)
            : coords.Value));
    }

    public void FixAtoms(bool isFixed)
    {
        if (App.Instance.Viewport.CurrentStructureView == null)
        {
            return;
        }
        
        Assert.IsFalse(App.Instance.Viewport.selectionManager.IsEmpty);
        App.Instance.Viewport.SubmitOperation(new SetFixedOperation(App.Instance.Viewport.selectionManager.SelectedAtoms, isFixed));
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

        string element = elementDropdown.options[elementDropdown.value].text;
        Assert.IsTrue(App.Elements.ContainsKey(element));

        App.Instance.Viewport.SubmitOperation(new AddAtomOperation(fractionalToggle.isOn
            ? currentArc.FractionalToCartesian(coords.Value)
            : coords.Value, element, 0));
    }
}