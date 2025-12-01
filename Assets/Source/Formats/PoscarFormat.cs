using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class PoscarFormat : ICrystalFormat
{
    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        var linesEnumerator = lines.GetEnumerator();
        string line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

        Debug.Log($"Reading poscar with custom name `{line}`");

        line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);
        if (!float.TryParse(line, out float scaleFactor))
        {
            throw new ArgumentException($"Expected a float for scale factor, got `{line}`");
        }

        Vector3[] baseVectors = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

            var vectorStrings = ICrystalFormat.SplitWhiteSpace(line.Trim());
            Assert.AreEqual(vectorStrings.Length, 3);
            baseVectors[i] = new(float.Parse(vectorStrings[0]), float.Parse(vectorStrings[1]),
                float.Parse(vectorStrings[2]));
            baseVectors[i] *= scaleFactor;
        }

        line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

        string[] elementList, countList;
        if (line.Any(x => char.IsLetter(x)))
        {
            // Element list specified
            elementList = ICrystalFormat.SplitWhiteSpace(line);
            
            line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);
            countList = ICrystalFormat.SplitWhiteSpace(line);
        }
        else
        {
            // Element list omitted
            countList = ICrystalFormat.SplitWhiteSpace(line);
            elementList = Enumerable.Repeat("C", countList.Length).ToArray();
        }

        var atomTypes = Enumerable.Zip(elementList, countList, (elem, count) => new
        {
            element = elem,
            count = int.Parse(count)
        });

        while ((line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator)) != null)
        {
            CrystalStructure crystalStructure = new();
            crystalStructure.SetLattice(baseVectors);

            bool hasFixedFlags = false;
            if (line.ToLower() == "selective dynamics")
            {
                line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);
                hasFixedFlags = true;
            }

            bool isCartesian;
            if (line.First() is 'c' or 'C')
            {
                isCartesian = true;
            }
            else if (line.First() is 'd' or 'D')
            {
                isCartesian = false;
            }
            else
            {
                Debug.Log($"Unknown expression `{line}`, aborting read");
                break;
            }

            foreach (var atomType in atomTypes)
            {
                if (!App.Elements.ContainsKey(atomType.element))
                {
                    Debug.LogError($"Unknown element symbol `{atomType.element}`");
                }
                
                for (int i = 0; i < atomType.count; i++)
                {
                    line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

                    var vectorStrings = ICrystalFormat.SplitWhiteSpace(line.Trim());
                    
                    AtomFlags flags = AtomFlags.None;
                    if (vectorStrings.Length == 6)
                    {
                        if (!hasFixedFlags)
                        {
                            Debug.LogWarning(
                                "Fixed flags specified, but the file doesn't contains `Selective dynamics`. ");
                        }

                        if (vectorStrings[3] == "F" || vectorStrings[4] == "F" || vectorStrings[5] == "F")
                        {
                            flags = AtomFlags.Fixed;
                        }
                    }
                    else
                    {
                        if (hasFixedFlags)
                        {
                            Debug.LogWarning("Fixed flags not specified, but they should be. ");
                        }

                        Assert.AreEqual(vectorStrings.Length, 3,
                            $"Expected 3 or 6 tokens for a atom, got {vectorStrings.Length}. ");
                    }

                    Vector3 coords = new(float.Parse(vectorStrings[0]), float.Parse(vectorStrings[1]),
                        float.Parse(vectorStrings[2]));

                    if (isCartesian)
                    {
                        coords *= scaleFactor;
                    }
                    else
                    {
                        coords = crystalStructure.FractionalToCartesian(coords);
                    }

                    crystalStructure.AddAtom(atomType.element, coords, 0, flags);
                }
            }

#if UNITY_WEBGL
            file.Structures.Add(crystalStructure);
#else
            file.ListMutex.WaitOne();
            file.Structures.Add(crystalStructure);
            file.ListMutex.ReleaseMutex();
#endif
            progressCallback?.Invoke(file.StructureCount);
        }
    }

    public string Write(StructureFile file)
    {
        if (file.StructureCount > 1)
        {
            Debug.LogWarning(
                "Current file contains more than one structure, exporter will assume that the lattice params and element composition is same across structures. ");
        }

        if (file.StructureCount == 0)
        {
            Debug.LogError("Structure is empty, cannot write POSCAR. ");
            return null;
        }

        var structure = file.Structures.First();

        StringBuilder lines = new();
        lines.Append("Exported from LaspView\n");

        lines.Append("1.00\n");

        var baseVectors = structure.GetBaseVectors();
        for (int i = 0; i < 3; i++)
        {
            lines.Append($"{baseVectors[i].x:f8} {baseVectors[i].y:f8} {baseVectors[i].z:f8}\n");
        }

        var elementCounts = ChemicalFormulaUtils.GroupAtoms(structure.Atoms).ToArray();
        lines.Append(string.Join(" ", elementCounts.Select(x => x.Key)));
        lines.Append("\n");

        lines.Append(string.Join(" ", elementCounts.Select(x => x.Value.Count)));
        lines.Append("\n");

        for (int i = 0; i < file.StructureCount; i++)
        {
            structure = file.Structures[i];
            
            bool useSelectiveDynamics = false;
            if (structure.Atoms.Any(x => x.Flags > AtomFlags.Fixed))
            {
                // If any fixed atoms, then enable selective dynamics
                lines.Append("Selective dynamics\n");
                useSelectiveDynamics = true;
            }

            lines.Append("Cartesian\n");
            foreach (var elementType in elementCounts)
            {
                foreach (var atom in elementType.Value)
                {
                    var clippedCoordinates = structure.ClipAtomCoords(atom.Coordinates);
                    if (useSelectiveDynamics)
                    {
                        char fixedFlags = atom.Flags == AtomFlags.Fixed ? 'F' : 'T';
                        lines.Append(
                            $"{clippedCoordinates.x:f8} {clippedCoordinates.y:f8} {clippedCoordinates.z:f8} {fixedFlags} {fixedFlags} {fixedFlags}\n");
                    }
                    else
                    {
                        lines.Append(
                            $"{clippedCoordinates.x:f8} {clippedCoordinates.y:f8} {clippedCoordinates.z:f8}\n");
                    }
                }
            }
        }

        return lines.ToString();
    }
}