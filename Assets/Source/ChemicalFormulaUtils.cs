using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class ChemicalFormulaUtils
{
    public static Dictionary<string, List<Atom>> GroupAtoms(List<Atom> atoms)
    {
        var groupedAtoms = new Dictionary<string, List<Atom>>();

        foreach (var atom in atoms)
        {
            if (!groupedAtoms.ContainsKey(atom.Symbol))
            {
                groupedAtoms[atom.Symbol] = new List<Atom>();
            }

            groupedAtoms[atom.Symbol].Add(atom);
        }

        return groupedAtoms;
    }

    public static string GetChemicalFormula(List<Atom> atoms, int maxLength=-1)
    {
        var groupedAtoms = GroupAtoms(atoms).Select(x => new KeyValuePair<string, int>(x.Key, x.Value.Count)).ToArray();

        Array.Sort(groupedAtoms, (a, b) =>
        {
            var (periodA, valanceA) = App.GetElementPeriodAndValance(a.Key);
            var (periodB, valanceB) = App.GetElementPeriodAndValance(b.Key);

            return valanceA != valanceB ? valanceB.CompareTo(valanceA) : periodA.CompareTo(periodB);
        });

        if (groupedAtoms.Length == 0)
        {
            return null;
        }

        StringBuilder formulaBuilder = new();
        int textLength = 0;
        foreach (var atomType in groupedAtoms)
        {
            int count = atomType.Value;
            formulaBuilder.Append($"{atomType.Key}{(count == 1 ? "" : "<size=10>" + count + "</size>")}");
            textLength += atomType.Key.Length + Mathf.CeilToInt(Mathf.Log10(count + 1));
            
            if (maxLength > 0 && textLength > maxLength)
            {
                formulaBuilder.Append("...");
                break;
            }
        }

        return formulaBuilder.ToString();
    }
}