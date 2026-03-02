using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

public class CifFormat : ICrystalFormat
{
    private static readonly Regex DATA_REGEX = new(@"^(_\w+)\s+([a-zA-Z0-9_.]+)$", RegexOptions.Compiled);
    private static readonly Regex VARIABLE_REGEX = new(@"^(_\w+)$", RegexOptions.Compiled);

    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        var linesEnumerator = lines.GetEnumerator();
        string line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

        // Skip comments
        while (line.Trim().StartsWith("#"))
        {
            line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);
        }
        
        if (line.StartsWith("data_"))
        {
            Debug.LogWarning("Cif file should start with `data_image0`, but it doesn't. ");
        }

        // a, b, c, alpha, beta, gamma
        float[] latticeParams = { -1, -1, -1, -1, -1, -1 };
        bool isReadingList = false;
        
        List<string> listDefinition = new();

        // Read lattice paramaters
        while (true)
        {
            line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

            if (line == null)
            {
                throw new Exception("End of file reached but atom column definition is still not found. ");
            }

            string trimmedLine = line.Trim();
            if (trimmedLine.ToLower() == "loop_")
            {
                isReadingList = true;
                // We only need the last table, which is the header for the atom table
                listDefinition.Clear();
                continue;
            }
            
            if (isReadingList)
            {
                var varMatch = VARIABLE_REGEX.Match(trimmedLine);
                if (varMatch.Success)
                {
                    var dataTag = varMatch.Groups[1].Value;
                    
                    listDefinition.Add(dataTag.ToLower());
                    continue;
                }

                isReadingList = false;
            }

            var dataMatch = DATA_REGEX.Match(trimmedLine);
            if (dataMatch.Success)
            {
                var dataTag = dataMatch.Groups[1].Value;
                var dataValue = dataMatch.Groups[2].Value;

                switch (dataTag.ToLower())
                {
                    case "_cell_length_a":
                        latticeParams[0] = float.Parse(dataValue);
                        break;
                    case "_cell_length_b":
                        latticeParams[1] = float.Parse(dataValue);
                        break;
                    case "_cell_length_c":
                        latticeParams[2] = float.Parse(dataValue);
                        break;
                    case "_cell_angle_alpha":
                        latticeParams[3] = float.Parse(dataValue);
                        break;
                    case "_cell_angle_beta":
                        latticeParams[4] = float.Parse(dataValue);
                        break;
                    case "_cell_angle_gamma":
                        latticeParams[5] = float.Parse(dataValue);
                        break;
                    default:
                        Debug.Log($"Ignoring unknown data line `{dataTag}`");
                        break;
                }
            }
            else
            {
                if (listDefinition.Any(x => x.StartsWith("_atom")) && ICrystalFormat.SplitWhiteSpace(line).Length == listDefinition.Count)
                {
                    // If the atom column definition is already present and the table begins
                    break;
                }

                Debug.LogWarning($"Ignoring line `{line}`");
            }
        }

        if (latticeParams.Any(x => x < 0))
        {
            throw new Exception("One or more required lattice parameters not specified. ");
        }

        // Read atom list column format specification
        int xColumnIndex = -1, yColumnIndex = -1, zColumnIndex = -1, elementIndex = -1, labelIndex = -1;
        bool isCartCoords = false, isFractCoords = false;
        
        for (int columnIndex = 0; columnIndex < listDefinition.Count; columnIndex++)
        {
            switch (listDefinition[columnIndex])
            {
                case "_atom_site_label":
                    labelIndex = columnIndex;
                    break;
                case "_atom_site_fract_x":
                    xColumnIndex = columnIndex;
                    isFractCoords = true;
                    break;
                case "_atom_site_fract_y":
                    yColumnIndex = columnIndex;
                    isFractCoords = true;
                    break;
                case "_atom_site_fract_z":
                    zColumnIndex = columnIndex;
                    isFractCoords = true;
                    break;
                case "_atom_site_cartn_x":
                    xColumnIndex = columnIndex;
                    isCartCoords = true;
                    break;
                case "_atom_site_cartn_y":
                    yColumnIndex = columnIndex;
                    isCartCoords = true;
                    break;
                case "_atom_site_cartn_z":
                    zColumnIndex = columnIndex;
                    isCartCoords = true;
                    break;
                case "_atom_site_type_symbol":
                    elementIndex = columnIndex;
                    break;
                default:
                    Debug.Log($"Ignoring unknown column `{listDefinition[columnIndex]}`");
                    break;
            }
        }

        if (elementIndex < 0 && labelIndex >= 0)
        {
            elementIndex = labelIndex;
        }

        if (elementIndex < 0 || xColumnIndex < 0 || yColumnIndex < 0 || zColumnIndex < 0)
        {
            throw new Exception("Atom column specification missing key fields. ");
        }
        
        if (isFractCoords && isCartCoords)
        {
            throw new Exception("Coordinates must be in either cartesian or fractional format, but these two cannot be mixed. ");
        }

        CrystalStructure crystalStructure = new();
        crystalStructure.SetLattice(latticeParams);

        do
        {
            var columns = ICrystalFormat.SplitWhiteSpace(line);
            if(columns.Length != listDefinition.Count)
            {
                // Ignore any content after the table ends
                break;
            }

            var coords = new Vector3(float.Parse(columns[xColumnIndex]), float.Parse(columns[yColumnIndex]),
                float.Parse(columns[zColumnIndex]));
            var element = columns[elementIndex];

            if (!App.Elements.ContainsKey(element))
            {
                Debug.LogWarning($"Unknown element symbol `{element}`, skipping. ");
                continue;
            }

            if (isFractCoords)
            {
                coords = crystalStructure.FractionalToCartesian(coords);
            }
            crystalStructure.AddAtom(element, coords, 0);
            
        } while ((line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator)) != null);

#if UNITY_WEBGL
        file.Structures.Add(crystalStructure);
#else
        file.ListMutex.WaitOne();
        file.Structures.Add(crystalStructure);
        file.ListMutex.ReleaseMutex();
#endif

        progressCallback?.Invoke(1);
    }

    public string Write(StructureFile file)
    {
        if (file.StructureCount > 1)
        {
            Debug.LogWarning(
                "Current file contains more than one structure, exporter will only write the first. ");
        }

        if (file.StructureCount == 0)
        {
            Debug.LogError("Structure is empty, cannot write POSCAR. ");
            return null;
        }

        StringBuilder lines = new StringBuilder();

        lines.Append("data_image0\n");

        var structure = file.Structures.First();

        var latticeParams = structure.LatticeParams;
        lines.Append($"_cell_length_a     {latticeParams[0]:f5}\n");
        lines.Append($"_cell_length_b     {latticeParams[1]:f5}\n");
        lines.Append($"_cell_length_c     {latticeParams[2]:f5}\n");
        lines.Append($"_cell_angle_alpha  {latticeParams[3]:f5}\n");
        lines.Append($"_cell_angle_beta   {latticeParams[4]:f5}\n");
        lines.Append($"_cell_angle_gamma  {latticeParams[5]:f5}\n");

        lines.Append("loop_\n");
        lines.Append("  _atom_site_label\n");
        lines.Append("  _atom_site_fract_x\n");
        lines.Append("  _atom_site_fract_y\n");
        lines.Append("  _atom_site_fract_z\n");
        lines.Append("  _atom_site_type_symbol\n");

        Dictionary<string, int> elementCount = new();

        foreach (var atom in structure.Atoms)
        {
            var indexOfElementType = elementCount.GetValueOrDefault(atom.Symbol, 0);
            indexOfElementType++;

            elementCount[atom.Symbol] = indexOfElementType;

            var atomName = $"{atom.Symbol}{indexOfElementType}";

            var clippedCoordinates = structure.ClipFractionalCoords(structure.CartesianToFractional(atom.Coordinates));
            lines.Append(string.Format("{0,6}{1,14:f6}{2,14:f6}{3,14:f6}{4,4}\n", atomName, clippedCoordinates.x,
                clippedCoordinates.y, clippedCoordinates.z, atom.Symbol));
        }
        
        return lines.ToString();
    }
}