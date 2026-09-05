
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class ExtxyzFormat : ICrystalFormat
{
    public static string[] SplitWhiteSpaceRespectQuotes(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result.ToArray();
    }

    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        var linesEnumerator = lines.GetEnumerator();

        // One iteration per frame, frames are separated by blank lines per the extxyz spec
        string line;
        while ((line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator)) != null)
        {
            if (!ReadFrame(file, linesEnumerator, line, progressCallback))
            {
                break;
            }
        }
    }

    // Reads a single frame whose atom count line is `line`, returns false if the frame is unrecoverable
    private bool ReadFrame(StructureFile file, IEnumerator<string> linesEnumerator, string line,
        Action<int> progressCallback = null)
    {
        int atomCount = int.Parse(line);
        line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator);

        if (line == null)
        {
            throw new Exception("Missing comment line in EXTXYZ file. ");
        }

        string lattice = "", properties = "";
        float energy = 0f;

        foreach (var variable in SplitWhiteSpaceRespectQuotes(line))
        {
            if (string.IsNullOrWhiteSpace(variable))
            {
                continue;
            }

            var keyValue = variable.Split('=');
            Debug.Assert(keyValue.Length == 2);

            string key = keyValue[0].ToLower();
            string value = keyValue[1].Trim('"');

            if (key == "lattice")
            {
                lattice = value;
            }
            else if (key == "properties")
            {
                properties = value;
            }
            else if (key == "energy")
            {
                // Optional metadata, don't fail the whole frame on a malformed value
                if (!float.TryParse(value, out energy))
                {
                    Debug.LogWarning($"Failed to parse energy value `{value}` in EXTXYZ file. ");
                    energy = 0f;
                }
            }
        }

        if (lattice == "" || properties == "")
        {
            throw new Exception("Lattice or properties definition missing in EXTXYZ file. ");
        }

        // Parse lattice vectors
        var latticeLines = lattice.Split(' ');
        Debug.Assert(latticeLines.Length == 9);

        Vector3 latticeVectorA = new(float.Parse(latticeLines[0]), float.Parse(latticeLines[1]), float.Parse(latticeLines[2]));
        Vector3 latticeVectorB = new(float.Parse(latticeLines[3]), float.Parse(latticeLines[4]), float.Parse(latticeLines[5]));
        Vector3 latticeVectorC = new(float.Parse(latticeLines[6]), float.Parse(latticeLines[7]), float.Parse(latticeLines[8]));

        float[] latticeParams = {
            latticeVectorA.magnitude, latticeVectorB.magnitude, latticeVectorC.magnitude,
            Vector3.Angle(latticeVectorB, latticeVectorC),
            Vector3.Angle(latticeVectorA, latticeVectorC),
            Vector3.Angle(latticeVectorA, latticeVectorB)
        };

        CrystalStructure crystalStructure = new();
        crystalStructure.SetLattice(latticeParams);
        crystalStructure.Energy = energy;

        int symbolIndex = -1, xIndex = -1, yIndex = -1, zIndex = -1, chargeIndex = -1;

        // Parse properties
        var propertyList = properties.Split(':');
        Assert.IsTrue(propertyList.Length % 3 == 0);

        int currentIndex = 0;
        for (int i = 0; i < propertyList.Length; i += 3)
        {
            string propertyName = propertyList[i];
            string propertyType = propertyList[i + 1];
            int propertyLength = int.Parse(propertyList[i + 2]);

            switch (propertyName)
            {
                case "species":
                    Assert.AreEqual(propertyType, "S");
                    Assert.AreEqual(propertyLength, 1);
                    symbolIndex = currentIndex;
                    break;
                case "pos":
                    Assert.AreEqual(propertyType, "R");
                    Assert.AreEqual(propertyLength, 3);
                    xIndex = currentIndex;
                    yIndex = currentIndex + 1;
                    zIndex = currentIndex + 2;
                    break;
                case "charge":
                    Assert.IsTrue(propertyType == "R" || propertyType == "I");
                    Assert.AreEqual(propertyLength, 1);
                    chargeIndex = currentIndex;
                    break;
            }

            currentIndex += propertyLength;
        }

        if (symbolIndex < 0 || xIndex < 0 || yIndex < 0 || zIndex < 0)
        {
            Debug.LogError("Essential atom properties (species, pos) are not defined in EXTXYZ file. ");
            return false;
        }

        // Read atom lines, stop when the declared atom count is reached so that
        // the next frame's count line is left to the outer loop
        while (crystalStructure.Atoms.Count < atomCount &&
               (line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator)) != null)
        {
            var columns = ICrystalFormat.SplitWhiteSpace(line);
            Assert.AreEqual(columns.Length, currentIndex);

            var coords = new Vector3(float.Parse(columns[xIndex]), float.Parse(columns[yIndex]),
                float.Parse(columns[zIndex]));
            var element = columns[symbolIndex];
            var charge = 0f;

            if (chargeIndex >= 0)
            {
                charge = float.Parse(columns[chargeIndex]);
            }

            crystalStructure.AddAtom(element, coords, charge);
        }

        Assert.AreEqual(crystalStructure.Atoms.Count, atomCount, "Atom count mismatch in EXTXYZ file. ");
#if UNITY_WEBGL
        file.Structures.Add(crystalStructure);
#else
        file.ListMutex.WaitOne();
        file.Structures.Add(crystalStructure);
        file.ListMutex.ReleaseMutex();
#endif

        progressCallback?.Invoke(file.StructureCount);
        return true;
    }

    public string Write(StructureFile file)
    {
        if (file.StructureCount == 0)
        {
            Debug.LogError("Structure is empty, cannot write EXTXYZ. ");
            return null;
        }

        StringBuilder lines = new();

        for (int i = 0; i < file.StructureCount; i++)
        {
            CrystalStructure crystalStructure = file.Structures[i];
            bool hasCharge = crystalStructure.Atoms.Any(atom => Mathf.Abs(atom.Charge) > 1e-6f);

            lines.Append($"{crystalStructure.Atoms.Count}\n");

            var baseVectors = crystalStructure.GetBaseVectors();
            lines.Append(
                $"Lattice=\"{baseVectors[0].x:f8} {baseVectors[0].y:f8} {baseVectors[0].z:f8} " +
                $"{baseVectors[1].x:f8} {baseVectors[1].y:f8} {baseVectors[1].z:f8} " +
                $"{baseVectors[2].x:f8} {baseVectors[2].y:f8} {baseVectors[2].z:f8}\" ");
            lines.Append("Properties=species:S:1:pos:R:3");
            if (hasCharge)
            {
                lines.Append(":charge:R:1");
            }

            if (Mathf.Abs(crystalStructure.Energy) > 1e-8f)
            {
                lines.Append($" energy={crystalStructure.Energy:f8}");
            }

            lines.Append('\n');

            foreach (var atom in crystalStructure.Atoms)
            {
                var coords = crystalStructure.ClipCartesianCoords(atom.Coordinates);
                lines.Append($"{atom.Symbol} {coords.x:f8} {coords.y:f8} {coords.z:f8}");
                if (hasCharge)
                {
                    lines.Append($" {atom.Charge:f8}");
                }

                lines.Append('\n');
            }

            // Per the extxyz spec, each frame except the last is terminated by a blank line
            if (i < file.StructureCount - 1)
            {
                lines.Append('\n');
            }
        }

        return lines.ToString();
    }
}