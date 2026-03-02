using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class CubeFormat : ICrystalFormat
{
    const float BOHR_TO_ANGSTROM = 0.52917721067121f;

    private static int FastSplit(ReadOnlySpan<char> line, int initPos)
    {
        int startPos = initPos;
        while (startPos < line.Length && !char.IsWhiteSpace(line[startPos]))
        {
            startPos++;
        }

        int endPos = startPos;
        // while (endPos)
        // {
        //     
        // }

        return 0;
    }
    
    public void Read(StructureFile file, IEnumerable<string> lines, Action<int> progressCallback = null)
    {
        var linesEnumerator = lines.GetEnumerator();
        
        // Skip 2 comment lines
        string line = ICrystalFormat.GetNextLine(linesEnumerator);
        line = ICrystalFormat.GetNextLine(linesEnumerator);
        
        line = ICrystalFormat.GetNextLine(linesEnumerator);
        var parts = ICrystalFormat.SplitWhiteSpace(line);
        if (parts.Length != 4)
        {
            throw new Exception("Invalid cube format: expected 4 values in the 3rd line. ");
        }
        int numAtoms = int.Parse(parts[0]);
        Vector3 origin = new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        
        line = ICrystalFormat.GetNextLine(linesEnumerator);
        parts = ICrystalFormat.SplitWhiteSpace(line);
        if (parts.Length != 4)
        {
            throw new Exception("Invalid cube format: expected 4 values in the 4th line. ");
        }
        int numVoxelsX = int.Parse(parts[0]);
        Vector3 strideX = new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        if (numVoxelsX > 0)
        {
            strideX *= BOHR_TO_ANGSTROM;
        }
        else
        {
            numVoxelsX = -numVoxelsX;
        }
        
        line = ICrystalFormat.GetNextLine(linesEnumerator);
        parts = ICrystalFormat.SplitWhiteSpace(line);
        if (parts.Length != 4)
        {
            throw new Exception("Invalid cube format: expected 4 values in the 5th line. ");
        }
        int numVoxelsY = int.Parse(parts[0]);
        Vector3 strideY = new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        if (numVoxelsY > 0)
        {
            strideY *= BOHR_TO_ANGSTROM;
        }
        else
        {
            numVoxelsY = -numVoxelsY;
        }
        
        line = ICrystalFormat.GetNextLine(linesEnumerator);
        parts = ICrystalFormat.SplitWhiteSpace(line);
        if (parts.Length != 4)
        {
            throw new Exception("Invalid cube format: expected 4 values in the 6th line. ");
        }
        int numVoxelsZ = int.Parse(parts[0]);
        Vector3 strideZ = new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        if (numVoxelsZ > 0)
        {
            strideZ *= BOHR_TO_ANGSTROM;
        }
        else
        {
            numVoxelsZ = -numVoxelsZ;
        }
        
        CrystalStructure crystalStructure = new();
        crystalStructure.SetLattice(new[] {
            numVoxelsX * strideX, numVoxelsY * strideY, numVoxelsZ * strideZ
        });
        
        for (int i = 0; i < numAtoms; i++)
        {
            line = ICrystalFormat.GetNextLine(linesEnumerator);
            parts = ICrystalFormat.SplitWhiteSpace(line);
            if (parts.Length != 5)
            {
                throw new Exception("Invalid cube format: expected 5 values in each atom line. ");
            }
            
            int atomicNumber = int.Parse(parts[0]);
            float charge = float.Parse(parts[1]);
            Vector3 coords = new(float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
            coords *= BOHR_TO_ANGSTROM;
            crystalStructure.AddAtom(App.Elements.First(x => x.Value.Number == atomicNumber).Key, coords, charge);
        }
        
        float[,,] voxelData = new float[numVoxelsX, numVoxelsY, numVoxelsZ];
        int indexInRow = 0;
        line = ICrystalFormat.GetNextLine(linesEnumerator);
        parts = ICrystalFormat.SplitWhiteSpace(line);
        for (int i = 0; i < numVoxelsX; i++)
        {
            for (int j = 0; j < numVoxelsY; j++)
            {
                for (int k = 0; k < numVoxelsZ; k++)
                {
                    if (indexInRow >= parts.Length)
                    {
                        line = ICrystalFormat.GetNextLine(linesEnumerator);
                        parts = ICrystalFormat.SplitWhiteSpace(line);
                        indexInRow = 0;
                    }
                    
                    voxelData[i, j, k] = float.Parse(parts[indexInRow]);
                    indexInRow++;
                }
            }
        }

        crystalStructure.VoxelData = voxelData;
        
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
        throw new NotImplementedException("Write method is not implemented for CubeFormat. ");
    }
}
