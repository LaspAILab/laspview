using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public interface IFormat
{
    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null);

    public string Write(StructureFile file);

    public static string[] SplitWhiteSpace(string line)
    {
        return Regex.Split(line, "\\s+", RegexOptions.IgnoreCase);
    }

    public static string GetNextLine(IEnumerator<string> lineEnumerator, bool doTrim = true)
    {
        string line;
        do
        {
            if (!lineEnumerator.MoveNext())
            {
                return null;
            }

            line = lineEnumerator.Current;
        } while (string.IsNullOrWhiteSpace(line));
        
        return doTrim ? line.Trim() : line;
    }

    public static IFormat GetFormatIO(string filename)
    {
        // Recognize any file with name `POSCAR` as a poscar file
        if (Path.GetFileNameWithoutExtension(filename).ToUpper() == "POSCAR")
        {
            return new PoscarFormat();
        }
         
        return Path.GetExtension(filename) switch
        {
            ".arc" or ".car" => new ArcFormat(),
            ".poscar" => new PoscarFormat(),
            ".cif" => new CifFormat(),
            _ => null
        };
    }
}