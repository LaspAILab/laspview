using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;


public static class ArcIO
{
    private static string[] SplitWhiteSpace(string line)
    {
        return Regex.Split(line, "\\s+", RegexOptions.IgnoreCase);
    }

    public static bool ReadArc(this ArcFile arcFile, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
#if UNITY_WEBGL
        // Since WebGL is single-threaded, we have to limit the load time and interrupt reading to avoid freeze on large files
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
#endif
        ArcStructure currentStructure = new();

        string prevLine = null;
        foreach (var line in lines)
        {
            if (line.Trim() == "")
            {
                continue;
            }

            var tokens = SplitWhiteSpace(line);
            if (tokens[0].StartsWith("!DATE"))
            {
                // TODO: Precess date
            }

            if (tokens.Contains("Energy") || tokens.Contains("IS") ||
                tokens.Contains("TS") ||
                (tokens.Contains("SSW") && !tokens.Contains("Str")))
            {
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
            else if (tokens[0] == "PBC" && tokens.Length >= 7 && tokens[1] != "=")
            {
                try
                {
                    float[] latticeParams = tokens.Skip(1).Take(6).Select(float.Parse).ToArray();
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
                catch (Exception e)
                {
                    Debug.Log($"Unknown exception: {e.Message}");
                }
            }
            else if (tokens.Contains("CORE"))
            {
                try
                {
                    Vector3 coords = new(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));

                    currentStructure.AddAtom(tokens[0], coords, float.Parse(tokens[8]));
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

            if (line.Trim() == "end" && prevLine.Trim() == "end")
            {
#if UNITY_WEBGL
                arcFile.Structures.Add(currentStructure);
#else
                arcFile.ListMutex.WaitOne();
                arcFile.Structures.Add(currentStructure);
                arcFile.ListMutex.ReleaseMutex();
#endif

                progressCallback?.Invoke(arcFile.StructureCount);
                currentStructure = new();
            }

            prevLine = line;

#if UNITY_WEBGL
            if (stopWatch.Elapsed.TotalSeconds > 10)
            {
                stopWatch.Stop();
                return false;
            }
#endif
        }

        return true;
    }

    public static string WriteArc(this ArcFile arcFile)
    {
        StringBuilder lines = new StringBuilder();
        // Header
        lines.Append("!BIOSYM archive 2\nPBC=ON\n");

        for (int i = 0; i < arcFile.StructureCount; i++)
        {
            ArcStructure arcStructure = arcFile.Structures[i];
            lines.Append($"                      Energy{i,10:d}{0.0f,16:f4}{arcStructure.Energy,18:f6}\n");
            lines.Append("!DATE\n");
            lines.Append(string.Format("PBC{0,14:f8}{1,14:f8}{2,14:f8}{3,14:f8}{4,14:f8}{5,14:f8}\n",
                arcStructure.LatticeParams[0], arcStructure.LatticeParams[1], arcStructure.LatticeParams[2],
                arcStructure.LatticeParams[3], arcStructure.LatticeParams[4], arcStructure.LatticeParams[5]));
            for (int h = 0; h < arcStructure.Atoms.Count; h++)
            {
                Atom atom = arcStructure.Atoms[h];
                lines.Append(string.Format("{0,-2}{1,15:f9}{2,15:f9}{3,15:f9}", atom.Symbol, atom.Coordinates[0],
                    atom.Coordinates[1], atom.Coordinates[2]));
                lines.Append($" CORE{h + 1,5:d} {atom.Symbol,-3}{atom.Symbol,-3}{0.0f,8:f4}{h + 1,5:d}\n");
            }

            lines.Append("end\nend\n");
        }

        return lines.ToString();
    }
}