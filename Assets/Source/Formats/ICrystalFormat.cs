using System;
using System.Collections.Generic;
using System.IO;

public interface ICrystalFormat : IFileFormat
{
    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null);

    public string Write(StructureFile file);

    public static ICrystalFormat GetFormatIO(string filename)
    {
        // Recognize any file with name `POSCAR` as a poscar file
        if (Path.GetFileNameWithoutExtension(filename).ToUpper() == "POSCAR")
        {
            return new PoscarFormat();
        }
         
        return Path.GetExtension(filename) switch
        {
            ".arc" or ".car" or ".txt" => new ArcFormat(),
            ".poscar" => new PoscarFormat(),
            ".cif" => new CifFormat(),
            ".extxyz" => new ExtxyzFormat(),
            _ => null
        };
    }
}