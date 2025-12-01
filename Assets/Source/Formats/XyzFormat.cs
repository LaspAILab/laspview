
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class XyzFormat : IMoleculeFormat
{
    public Atom[] ReadAtoms(IEnumerable<string> lines, Vector3? centerOffset = null)
    {
        var linesEnumerator = lines.GetEnumerator();
        string line = ICrystalFormat.GetNextLine(linesEnumerator);
        
        int atomCount = int.Parse(line);
        
        // Skip comment line
        _ = IFileFormat.GetNextLine(linesEnumerator);
        
        Atom[] atoms = new Atom[atomCount];
        
        // Read atom lines
        int i = 0;
        while ((line = ICrystalFormat.GetNextNonEmptyLine(linesEnumerator)) != null)
        {
            var columns = ICrystalFormat.SplitWhiteSpace(line);
            Assert.AreEqual(columns.Length, 4, "Too few columns in atom table.");
            
            var coords = new Vector3(float.Parse(columns[1]), float.Parse(columns[2]),
                float.Parse(columns[3]));
            var element = columns[0];

            atoms[i] = new Atom
            {
                Index = i,
                Symbol = element,
                Coordinates = coords,
                Charge = 0,
                Flags = AtomFlags.None
            };
            
            i++;
        }
        
        Assert.AreEqual(i, atomCount, "Atom count mismatch in XYZ file.");
        IMoleculeFormat.PostTranslateMolecule(ref atoms, centerOffset);
        return atoms;
    }
}