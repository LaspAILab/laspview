using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class PoscarFormat : IFormat
{
    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        var linesEnumerator = lines.GetEnumerator();
        string line = IFormat.GetNextLine(linesEnumerator);

        Debug.Log($"Reading poscar with custom name `{line}`");

        line = IFormat.GetNextLine(linesEnumerator);
        if (!float.TryParse(line, out float scaleFactor))
        {
            throw new ArgumentException($"Expected a float for scale factor, got `{line}`");
        }

        Vector3[] baseVectors = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            line = IFormat.GetNextLine(linesEnumerator);

            var vectorStrings = IFormat.SplitWhiteSpace(line.Trim());
            Assert.AreEqual(vectorStrings.Length, 3);
            baseVectors[i] = new(float.Parse(vectorStrings[0]), float.Parse(vectorStrings[1]),
                float.Parse(vectorStrings[2]));
            baseVectors[i] *= scaleFactor;
        }

        line = IFormat.GetNextLine(linesEnumerator);

        string[] elementList, countList;
        if (line.Any(x => char.IsLetter(x)))
        {
            // Element list specified
            elementList = IFormat.SplitWhiteSpace(line);
            
            line = IFormat.GetNextLine(linesEnumerator);
            countList = IFormat.SplitWhiteSpace(line);
        }
        else
        {
            // Element list omitted
            countList = IFormat.SplitWhiteSpace(line);
            elementList = Enumerable.Repeat("C", countList.Length).ToArray();
        }

        var atomTypes = Enumerable.Zip(elementList, countList, (elem, count) => new
        {
            element = elem,
            count = int.Parse(count)
        });

        while ((line = IFormat.GetNextLine(linesEnumerator)) != null)
        {
            CrystalStructure crystalStructure = new();
            crystalStructure.SetLattice(baseVectors);

            bool hasFixedFlags = false;
            if (line.ToLower() == "selective dynamics")
            {
                line = IFormat.GetNextLine(linesEnumerator);
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
                    line = IFormat.GetNextLine(linesEnumerator);

                    var vectorStrings = IFormat.SplitWhiteSpace(line.Trim());
                    
                    float fixedFlags = 0;
                    if (vectorStrings.Length == 6)
                    {
                        if (!hasFixedFlags)
                        {
                            Debug.LogWarning(
                                "Fixed flags specified, but the file doesn't contains `Selective dynamics`. ");
                        }

                        fixedFlags = (vectorStrings[3] == "F" || vectorStrings[4] == "F" || vectorStrings[5] == "F")
                            ? 1
                            : 0;
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

                    crystalStructure.AddAtom(atomType.element, coords, 0, fixedFlags);
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

        var elementCounts = structure.GroupAtoms().ToArray();
        lines.Append(string.Join(" ", elementCounts.Select(x => x.Key)));
        lines.Append("\n");

        lines.Append(string.Join(" ", elementCounts.Select(x => x.Value.Count)));
        lines.Append("\n");

        for (int i = 0; i < file.StructureCount; i++)
        {
            structure = file.Structures[i];
            
            bool useSelectiveDynamics = false;
            if (structure.Atoms.Any(x => x.FixedFlags > 0))
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
                    if (useSelectiveDynamics)
                    {
                        char fixedFlags = atom.FixedFlags > 0 ? 'F' : 'T';
                        lines.Append(
                            $"{atom.Coordinates.x:f8} {atom.Coordinates.y:f8} {atom.Coordinates.z:f8} {fixedFlags} {fixedFlags} {fixedFlags}\n");
                    }
                    else
                    {
                        lines.Append(
                            $"{atom.Coordinates.x:f8} {atom.Coordinates.y:f8} {atom.Coordinates.z:f8}\n");
                    }
                }
            }
        }

        return lines.ToString();
    }
}