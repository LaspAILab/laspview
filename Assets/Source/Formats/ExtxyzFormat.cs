
using System;
using System.Collections.Generic;
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
        string line = ICrystalFormat.GetNextLine(linesEnumerator);

        int atomCount = int.Parse(line);
        line = ICrystalFormat.GetNextLine(linesEnumerator);

        string lattice = "", properties = "";

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
            return;
        }

        // Read atom lines
        while ((line = ICrystalFormat.GetNextLine(linesEnumerator)) != null)
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

        progressCallback?.Invoke(1);
    }

    public string Write(StructureFile file)
    {
        throw new NotImplementedException();
    }
}