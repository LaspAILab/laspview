
using System.Collections.Generic;
using System.Text.RegularExpressions;

public interface IFileFormat
{
    public static string[] SplitWhiteSpace(string line)
    {
        return Regex.Split(line, "\\s+", RegexOptions.IgnoreCase);
    }

    public static string GetNextNonEmptyLine(IEnumerator<string> lineEnumerator, bool doTrim = true)
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
    
    public static string GetNextLine(IEnumerator<string> lineEnumerator, bool doTrim = true)
    {
        if (!lineEnumerator.MoveNext())
        {
            return null;
        }

        string line = lineEnumerator.Current;
        return doTrim ? line.Trim() : line;
    }
}