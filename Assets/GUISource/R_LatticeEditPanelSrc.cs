using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class R_LatticeEditPanelSrc : MonoBehaviour
{
    public static R_LatticeEditPanelSrc S;

    public Text textLabel;
    public VectorInputController lengthsInput, anglesInput, repeatsInput;
    public Toggle keepFractionalToggle;

    void Start()
    {
        S = this;

        //Set Edit panel
        this.transform.SetParent(GuiMain.S.rightCanvas.transform, false);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button edButton = GameObject.Find("EditBtn").GetComponent<Button>();
        edButton.onClick.AddListener(() => { GuiMain.S.BtnHideObject(edButton, this.gameObject); });

        gameObject.SetActive(false);
    }

    public void UpdateLattice(CrystalStructure structure)
    {
        lengthsInput.SetValue(structure.LatticeParams[0], structure.LatticeParams[1], structure.LatticeParams[2]);
        anglesInput.SetValue(structure.LatticeParams[3], structure.LatticeParams[4], structure.LatticeParams[5]);
    }

    static int Gcd(IEnumerable<int> numbers)
    {
        return numbers.Aggregate(_GcdImpl);
    }

    private static int _GcdImpl(int a, int b)
    {
        while (true)
        {
            if (b == 0) return a;
            var a1 = a;
            a = b;
            b = a1 % b;
        }
    }

    public void UpdateAtomsCount(CrystalStructure structure)
    {
        var atoms = structure.GroupAtoms().Select(x => new KeyValuePair<string, int>(x.Key, x.Value.Count)).ToArray();

        Array.Sort(atoms, (a, b) =>
        {
            var (periodA, valanceA) = App.GetElementPeriodAndValance(a.Key);
            var (periodB, valanceB) = App.GetElementPeriodAndValance(b.Key);

            return valanceA != valanceB ? valanceB.CompareTo(valanceA) : periodA.CompareTo(periodB);
        });

        StringBuilder formulaBuilder = new();
        int totalAtoms = 0;
        int gcd = Gcd(atoms.Select(x => x.Value));

        foreach (var atomType in atoms)
        {
            int count = atomType.Value / gcd;
            formulaBuilder.Append($"{atomType.Key}<size=10>{(count == 1 ? "" : count)}</size>");
            totalAtoms += atomType.Value;
        }

        textLabel.text = $"{totalAtoms} atoms in total, formula: {formulaBuilder}";
    }

    public void ChangeLattice()
    {
        Vector3? lengthsNullable = lengthsInput.GetValue();
        Vector3? anglesNullable = anglesInput.GetValue();

        if (lengthsNullable == null || anglesNullable == null)
        {
            ConFun.UpdateStatusLabel("Invalid lattice params");
            return;
        }

        Vector3 lengths = lengthsNullable.Value, angles = anglesNullable.Value;

        float[] latticeParams = { lengths.x, lengths.y, lengths.z, angles.x, angles.y, angles.z };
        App.Instance.Viewport.SubmitOperation(new ChangeLatticeOperation(latticeParams, !keepFractionalToggle.isOn));
    }

    public void ToSuperCell()
    {
        Vector3? repeatsNullable = repeatsInput.GetValue();

        if (repeatsNullable == null)
        {
            ConFun.UpdateStatusLabel("Invalid lattice params");
            return;
        }

        Vector3 repeats = repeatsNullable.Value;
        App.Instance.Viewport.SubmitOperation(new SuperCellOperation((int)repeats.x, (int)repeats.y, (int)repeats.z));
    }
}