using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static System.Windows.Forms.LinkLabel;


public static class ArcIO
{
    private static string[] SplitWhiteSpace(string line)
    {
        return Regex.Split(line, "\\s+", RegexOptions.IgnoreCase);
    }

    public static void ReadArc(this ArcFile arcFile, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        ArcStructure currentStructure = null;

        foreach (var line in lines)
        {
            if (line.Trim() == "")
            {
                continue;
            }

            var tokens = SplitWhiteSpace(line);
            if (tokens.Contains("Energy") || tokens.Contains("IS") || tokens.Contains("TS") || (tokens.Contains("SSW") && !tokens.Contains("Str")))
            {
                if (currentStructure != null)
                {
                    arcFile.Structures.Add(currentStructure);
                    progressCallback?.Invoke(arcFile.StructureCount);
                }

                currentStructure = new ArcStructure();

                if (line.Contains("****"))
                {
                    currentStructure.Conv = 9999;
                }
                else
                {
                    currentStructure.Conv = float.Parse(tokens[2]);
                    currentStructure.Energy = float.Parse(tokens[4]);
                    if (int.TryParse(tokens[1], out int pathId))
                    {
                        currentStructure.PathId = pathId;
                    }
                    else
                    {
                        currentStructure.PathId = 0;
                    }
                }
            }
            else if (tokens.Contains("Str"))
            {
                if (line.Contains("****"))
                {
                    currentStructure.Energy = float.Parse(tokens[4]);
                }

                currentStructure.NumMinimum = int.Parse(tokens[1]);
                currentStructure.NumStr = int.Parse(tokens[2]);
            }
            else if (tokens.Contains("React"))
            {
                currentStructure.Energy = float.Parse(tokens[4]);
                currentStructure.NumStr = int.Parse(tokens[2]);
                currentStructure.NoPathway = int.Parse(tokens[1]);
            }
            else if (tokens[0] == "PBC")
            {
                try
                {
                    float[] latticeParams = tokens.Skip(1).Select(float.Parse).ToArray();
                    currentStructure.SetLattice(latticeParams);
                }
                catch (FormatException e)
                {
                    Debug.Log($"Expected a number for lattice parameters, got: {e.Message}");
                }
                catch (ArgumentException e)
                {
                    Debug.Log($"Invalid PBC line: {e.Message}");
                }
            }
            else if (tokens.Contains("CORE"))
            {
                try
                {
                    Vector3 coords = new(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));
                    currentStructure.AddAtom(tokens[0], coords, float.Parse(tokens[tokens.Length - 2]));
                }
                catch (FormatException e)
                {
                    Debug.Log($"Expected a number for atom parameters, got: {e.Message}");
                }
                catch (ArgumentException e)
                {
                    Debug.Log($"Invalid atom: {e.Message}");
                }
            }
        }

        if (currentStructure != null)
        {
            arcFile.Structures.Add(currentStructure);
            progressCallback?.Invoke(arcFile.StructureCount);
        }
    }

    public static ArcFile ReadArc(IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        ArcFile arcFile = new();
        arcFile.ReadArc(lines, progressCallback);
        return arcFile;
    }
}
