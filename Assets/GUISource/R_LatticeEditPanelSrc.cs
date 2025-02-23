using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class R_LatticeEditPanelSrc : MonoBehaviour
{
    public static R_LatticeEditPanelSrc S;

    public VectorInputController lengthsInput, anglesInput, repeatsInput;
    public Toggle keepFractionalToggle;

    void Awake()
    {
        S = this;

        //Set Edit panel
        this.transform.SetParent(GuiMain.S.rightCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button edButton = GameObject.Find("EditBtn").GetComponent<Button>();
        edButton.onClick.AddListener(() =>
        {
            GuiMain.S.BtnHideObject(edButton, this.gameObject);
        });
        // gameObject.SetActive(false);
    }

    public void UpdateLattice(ArcStructure structure)
    {
        lengthsInput.SetValue(structure.LatticeParams[0], structure.LatticeParams[1], structure.LatticeParams[2]);
        anglesInput.SetValue(structure.LatticeParams[3], structure.LatticeParams[4], structure.LatticeParams[5]);
    }

    public void ChangeLattice()
    {
        Vector3 lengths = lengthsInput.GetValue();
        Vector3 angles = anglesInput.GetValue();
        float[] latticeParams = { lengths.x, lengths.y, lengths.z, angles.x, angles.y, angles.z };
        App.Instance.Viewport.ChangeLattice(latticeParams, !keepFractionalToggle.isOn);
    }
}
