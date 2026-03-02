using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ArcFormat : ICrystalFormat
{
    private static readonly Regex HEADER_REGEX = new(@"^!BIOSYM archive (2|3)$", RegexOptions.Compiled);
    private static readonly Regex DATE_REGEX = new(@"^!DATE\s+(.+)$", RegexOptions.Compiled);

    private static readonly Regex LATTICE_PARAMS_REGEX =
        new(@"^PBC\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)(?:\s+\(?(\w+)\)?)?\s*$",
            RegexOptions.Compiled);

    private static readonly Regex ATOM_REGEX =
        new(@"^(\w+)\s+([0-9.-]+)\s+([0-9.-]+)\s+([0-9.-]+)\s+([A-Z]+)\s+(\w+)\s+(\w+)\s+(\w+)\s+([0-9.-]+)(?:\s+(\w+))?$",
            RegexOptions.Compiled);
    
    private static readonly Regex ENERGY_REGEX =
        new(@"^(Energy|IS|TS|SSW)\s+([0-9.-]+)\s+([0-9.-]+)\s+([0-9.-]+).*$",
            RegexOptions.Compiled);

    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
#if UNITY_WEBGL
        // Since WebGL is single-threaded, we have to limit the load time and interrupt reading to avoid freeze on large files
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
#endif
        CrystalStructure currentStructure = new();

        string prevTrimmedLine = null;
        foreach (var line in lines)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine == "")
            {
                continue;
            }

            Match match;
            if (prevTrimmedLine == null)
            {
                match = HEADER_REGEX.Match(trimmedLine);
                if (match.Success)
                {
                    prevTrimmedLine = trimmedLine;
                    continue;
                }
                
                Debug.LogWarning($"First line should be file header, got `{trimmedLine}`");
            }
            
            // We don't have anything to do with DATE, so skip matching. 
            // match = DATE_REGEX.Match(trimmedLine);
            
            // This branch is most likely, put it in the beginning
            match = ATOM_REGEX.Match(trimmedLine);
            if (match.Success)
            {
                try
                {
                    Vector3 coords = new(float.Parse(match.Groups[2].Value), float.Parse(match.Groups[3].Value),
                        float.Parse(match.Groups[4].Value));
                    string element = match.Groups[8].Value;
                    
                    if (!App.Elements.ContainsKey(element))
                    {
                        Debug.LogError($"Unknown element symbol `{element}`");
                    }

                    float charge = float.Parse(match.Groups[9].Value);
                    AtomFlags flags = AtomFlags.None;
                    // View charge value 1 as fixed
                    if (Mathf.Approximately(charge, 1))
                    {
                        flags = AtomFlags.Fixed;
                        charge = 0;
                    }
                    // View charge value 0.1 as react
                    if (Mathf.Approximately(charge, 1))
                    {
                        flags = AtomFlags.React;
                        charge = 0;
                    }

                    currentStructure.AddAtom(element, coords, charge, flags);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException($"Expected a float for atom coordinates: {e.Message}");
                }

                goto end;
            }
            
            match = ENERGY_REGEX.Match(trimmedLine);
            if (match.Success)
            {
                currentStructure.Conv = float.Parse(match.Groups[2].Value);
                currentStructure.Energy = float.Parse(match.Groups[4].Value);
            }
            
            match = LATTICE_PARAMS_REGEX.Match(trimmedLine);
            if (match.Success)
            {
                try
                {
                    float[] latticeParams = Enumerable.Range(1, 6).Select(x => float.Parse(match.Groups[x].Value))
                        .ToArray();
                    currentStructure.SetLattice(latticeParams);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException($"Expected a float as lattice param: {e.Message}");
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"{e.Message}");
                }

                goto end;
            }

            if (trimmedLine.ToLower() == "end" && prevTrimmedLine?.ToLower() == "end")
            {
                if (currentStructure.LatticeParams == null)
                {
                    throw new Exception($"Structure doesn't have lattice parameter definition. ");
                }
                
#if UNITY_WEBGL
                file.Structures.Add(currentStructure);
#else
                file.ListMutex.WaitOne();
                file.Structures.Add(currentStructure);
                file.ListMutex.ReleaseMutex();
#endif

                progressCallback?.Invoke(file.StructureCount);
                currentStructure = new();
            }

            end: prevTrimmedLine = trimmedLine;

#if UNITY_WEBGL
            if (stopWatch.Elapsed.TotalSeconds > 10)
            {
                stopWatch.Stop();
                Debug.LogWarning("File too large, truncated! ");
                return;
            }
#endif
        }
    }

    public string Write(StructureFile file)
    {
        StringBuilder lines = new StringBuilder();
        // Header
        lines.Append("!BIOSYM archive 2\nPBC=ON\n");

        for (int i = 0; i < file.StructureCount; i++)
        {
            CrystalStructure crystalStructure = file.Structures[i];
            lines.Append($"                      Energy{i,10:d}{0.0f,16:f4}{crystalStructure.Energy,18:f6}\n");
            lines.Append("!DATE\n");
            lines.Append(string.Format("PBC{0,14:f8}{1,14:f8}{2,14:f8}{3,14:f8}{4,14:f8}{5,14:f8}\n",
                crystalStructure.LatticeParams[0], crystalStructure.LatticeParams[1], crystalStructure.LatticeParams[2],
                crystalStructure.LatticeParams[3], crystalStructure.LatticeParams[4],
                crystalStructure.LatticeParams[5]));
            for (int h = 0; h < crystalStructure.Atoms.Count; h++)
            {
                Atom atom = crystalStructure.Atoms[h];
                float charge = atom.Flags switch
                {
                    AtomFlags.Fixed => 1,
                    AtomFlags.React => 0.1f,
                    _ => atom.Charge
                };
                
                var clippedCoordinates = crystalStructure.ClipCartesianCoords(atom.Coordinates);
                lines.Append(string.Format("{0,-5}{1,15:f9}{2,15:f9}{3,15:f9}", atom.Symbol, clippedCoordinates.x,
                    clippedCoordinates.y, clippedCoordinates.z));
                lines.Append($" CORE{h + 1,5:d} {atom.Symbol,-4}{atom.Symbol,-4}{charge,8:f4}{h + 1,5:d}\n");
            }

            lines.Append("end\nend\n");
        }

        return lines.ToString();
    }
}