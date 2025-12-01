
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

public class MolFormat : IMoleculeFormat
{
    static readonly string[] END_LABEL = {"M", "END"};
    private static readonly string[] V3000_LABEL = { "M", "V30" };
    private static readonly string[] V3000_COUNT_LABEL = { "M", "V30", "COUNT" };
    private static readonly string[] CTAB_BEGIN_LABEL = { "M", "V30", "BEGIN", "CTAB" };
    private static readonly string[] CTAB_END_LABEL = { "M", "V30", "END", "CTAB" };
    private static readonly string[] ATOM_BEGIN_LABEL = { "M", "V30", "BEGIN", "ATOM" };
    private static readonly string[] ATOM_END_LABEL = { "M", "V30", "END", "ATOM" };
    private static readonly string[] BOND_BEGIN_LABEL = { "M", "V30", "BEGIN", "BOND" };
    private static readonly string[] BOND_END_LABEL = { "M", "V30", "END", "BOND" };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool RelaxedEqual(string a, string b)
    {
        if (a == null || b == null)
        {
            throw new ArgumentNullException();
        }
        
        var partsA = IFileFormat.SplitWhiteSpace(a);
        var partsB = IFileFormat.SplitWhiteSpace(b);
        return RelaxedEqual(partsA, partsB);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool RelaxedEqual(string a, ReadOnlySpan<string> partsB)
    {
        if (a == null)
        {
            throw new ArgumentNullException();
        }
        
        var partsA = IFileFormat.SplitWhiteSpace(a);
        return RelaxedEqual(partsA, partsB);
    }
    
    static bool RelaxedEqual(ReadOnlySpan<string> partsA, ReadOnlySpan<string> partsB)
    {
        if (partsA.Length != partsB.Length)
        {
            return false;
        }
        
        for (int i = 0; i < partsA.Length; i++)
        {
            if (partsA[i] != partsB[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    private Atom[] ReadBodyV2000(IEnumerator<string> linesEnumerator, int atomCount, int bondCount)
    {
        Atom[] atoms = new Atom[atomCount];
        string line;
        int colLength = -1;
        // Read atom lines
        for (int i = 0; i < atomCount; i++)
        {
            line = IFileFormat.GetNextNonEmptyLine(linesEnumerator);
            var columns = IFileFormat.SplitWhiteSpace(line);
            
            if (colLength < 0)
            {
                Assert.IsTrue(columns.Length >= 4, "Too few columns in atom table.");
                colLength = columns.Length;
            }
            else
            {
                Assert.AreEqual(colLength, columns.Length, "Atom table should have consistent number of columns.");
            }
            
            var coords = new Vector3(float.Parse(columns[0]), float.Parse(columns[1]),
                float.Parse(columns[2]));
            var element = columns[3];

            atoms[i] = new Atom
            {
                Index = i,
                Symbol = element,
                Coordinates = coords,
                Charge = 0,
                Flags = AtomFlags.None
            };
        }

        for (int i = 0; i < bondCount; i++)
        {
            // Just consume the bond lines but do nothing
            IFileFormat.GetNextNonEmptyLine(linesEnumerator);
        }

        return atoms;
    }

    private Atom[] ReadBodyV3000(IEnumerator<string> linesEnumerator)
    {
        while (!RelaxedEqual(IFileFormat.GetNextLine(linesEnumerator), CTAB_BEGIN_LABEL))
        {
            // Consume until begin of ctab
        }

        string[] headerParts;
        string line;
        while (true)
        {
            line = IFileFormat.GetNextLine(linesEnumerator);
            headerParts = IFileFormat.SplitWhiteSpace(line);
            if (headerParts.Length >= 5 && !RelaxedEqual(headerParts.AsSpan(0, 3), V3000_COUNT_LABEL))
            {
                break;
            }
        }
        
        int atomCount = int.Parse(headerParts[3]);
        int bondCount = int.Parse(headerParts[4]);
        
        Atom[] atoms = new Atom[atomCount];
        
        while (!RelaxedEqual(IFileFormat.GetNextLine(linesEnumerator), ATOM_BEGIN_LABEL))
        {
            // Consume until begin of atom table
        }

        int i = 0;
        while (!RelaxedEqual(line = IFileFormat.GetNextNonEmptyLine(linesEnumerator), ATOM_END_LABEL))
        {
            var columns = IFileFormat.SplitWhiteSpace(line);
            
            Assert.IsTrue(RelaxedEqual(columns.AsSpan(0, 2), V3000_LABEL), "Line should start with M V30.");
            Assert.IsTrue(columns.Length >= 7, "Too few columns in atom table.");
            
            var coords = new Vector3(float.Parse(columns[4]), float.Parse(columns[5]),
                float.Parse(columns[6]));
            var element = columns[3];

            var chargeColumn = columns.FirstOrDefault(x => x.StartsWith("CHG="));
            float charge = 0;
            if (chargeColumn != null)
            {
                charge = float.Parse(chargeColumn.Substring(4));
            }

            atoms[i] = new Atom
            {
                Index = i,
                Symbol = element,
                Coordinates = coords,
                Charge = charge,
                Flags = AtomFlags.None
            };
            
            i++;
        }
        
        Assert.AreEqual(i, atomCount, "Atom table should have the number of atoms.");
        
        while (!RelaxedEqual(IFileFormat.GetNextLine(linesEnumerator), BOND_BEGIN_LABEL))
        {
            // Consume until begin of bond table
        }

        i = 0;
        while (!RelaxedEqual(IFileFormat.GetNextLine(linesEnumerator), BOND_END_LABEL))
        {
            // Consume until end of bond table
            i++;
        }
        Assert.AreEqual(i, bondCount, "Bond table should have the number of bonds.");
        
        while (!RelaxedEqual(IFileFormat.GetNextLine(linesEnumerator), CTAB_END_LABEL))
        {
            // Consume until end of ctab
        }

        return atoms;
    }

    public Atom[] ReadAtoms(IEnumerable<string> lines, Vector3? centerOffset = null)
    {
        var linesEnumerator = lines.GetEnumerator();
        string line = IFileFormat.GetNextLine(linesEnumerator);
        
        Debug.Log($"Parsing molecule {line}");
        
        // Skip timestamp line
        _ = IFileFormat.GetNextLine(linesEnumerator);
        // Skip comment line
        _ = IFileFormat.GetNextLine(linesEnumerator);

        line = ICrystalFormat.GetNextLine(linesEnumerator);
        string[] headerParts = IFileFormat.SplitWhiteSpace(line);
        Assert.IsTrue(headerParts.Length >= 4, "Too few columns in header line.");
        
        int atomCount = int.Parse(headerParts[0]);
        // Not used, but required to parse the file correctly
        int bondCount = int.Parse(headerParts[1]);
        string molVersion = headerParts.Last();
        Atom[] atoms = molVersion switch
        {
            "V2000" => ReadBodyV2000(linesEnumerator, atomCount, bondCount),
            "V3000" => ReadBodyV3000(linesEnumerator),
            _ => throw new NotImplementedException($"MOL version {molVersion} not supported.")
        };

        do
        {
            // Consume any additional lines until the end label
            line = IFileFormat.GetNextLine(linesEnumerator);
        } while (!string.IsNullOrEmpty(line) && !RelaxedEqual(line, END_LABEL));
        
        IMoleculeFormat.PostTranslateMolecule(ref atoms, centerOffset);
        return atoms;
    }
}