using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface IMoleculeFormat : IFileFormat
{
    /// <summary>
    /// Reads atoms from a line enumerable.
    /// </summary>
    /// <param name="lines">The lines iterator of the file.</param>
    /// <param name="centerOffset">The new center of the molecule, if null then the original coords will be kept.</param>
    /// <returns></returns>
    public Atom[] ReadAtoms(IEnumerable<string> lines, Vector3? centerOffset = null);
    
    public static IMoleculeFormat GetFormatIO(string filename)
    {
        // Recognize any file with name `POSCAR` as a poscar file
         
        return Path.GetExtension(filename) switch
        {
            ".xyz" => new XyzFormat(),
            ".mol" => new MolFormat(),
            _ => null
        };
    }

    public static void PostTranslateMolecule(ref Atom[] atoms, Vector3? centerOffset)
    {
        if (centerOffset == null)
        {
            return;
        }
        
        Vector3 center = Vector3.zero;
        foreach (var atom in atoms)
        {
            center += atom.Coordinates;
        }
        
        center /= atoms.Length;
        Vector3 offset = centerOffset.Value - center;

        foreach (var atom in atoms)
        {
            atom.Coordinates += offset;
        }
    }
}