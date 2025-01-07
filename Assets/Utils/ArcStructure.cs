using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ArcStructure
{
    public float Energy
    {
        get;
        set;
    }

    public float Conv
    {
        get;
        set;
    }

    public int PathId
    {
        get;
        set;
    }

    public int NumStr
    {
        get; 
        set;
    }

    public int NumMinimum
    {
        get;
        set;
    }

    public int NoPathway
    {
        get;
        set;
    }

    public Matrix4x4 LatticeMatrix
    {
        get;
        set;
    }

    // Lattice parameters: (a, b, c, α, β, γ)
    public float[] LatticeParams
    {
        get;
        set;
    }

    public List<Atom> Atoms
    {
        get;
        set;
    }

    public ArcStructure()
    {
        Energy = 0;
        Conv = 0;
        PathId = 0;
        NumStr = 0;
        NumMinimum = 0;
        NoPathway = 0;
        LatticeMatrix = Matrix4x4.identity;
        LatticeParams = new float[6];
        Atoms = new();
    }

    public void AddAtom(string elemSymbol, Vector3 coords, float charge)
    {
        if (!ConFun.Elements.ContainsKey(elemSymbol))
        {
            throw new ArgumentException("Element symbol not found in the element data dictionary.");
        }

        Atoms.Add(new Atom
        {
            Symbol = elemSymbol,
            Coordinates = coords,
            Charge = charge
        });
    }

    public void SetLattice(float[] latticeParams)
    {
        if (latticeParams.Length != 6)
        {
            throw new ArgumentException("Lattice parameters must have 6 elements.");
        }

        float bc2 = latticeParams[1] * latticeParams[1] + latticeParams[2] * latticeParams[2] - 2 * latticeParams[1] * latticeParams[2] * Mathf.Cos(latticeParams[3] / 180.0f * Mathf.PI);
        float h1 = latticeParams[0];
        float h2 = latticeParams[1] * Mathf.Cos(latticeParams[5] / 180.0f * Mathf.PI);
        float h3 = latticeParams[1] * Mathf.Sin(latticeParams[5] / 180.0f * Mathf.PI);
        float h4 = latticeParams[2] * Mathf.Cos(latticeParams[4] / 180.0f * Mathf.PI);
        float h5 = ((h2 - h4) * (h2 - h4) + h3 * h3 + latticeParams[2] * latticeParams[2] - h4 * h4 - bc2) / (2 * h3);
        float h6 = Mathf.Pow(latticeParams[2] * latticeParams[2] - h4 * h4 - h5 * h5, 0.5F);

        LatticeParams = latticeParams;

        // [h1, 0,  0 ]
        // [h2, h3, 0 ]
        // [h4, h5, h6]
        LatticeMatrix = new Matrix4x4(new Vector4(h1, 0, 0, 0), new Vector4(h2, h3, 0, 0), new Vector4(h4, h5, h6, 0), new Vector4(0, 0, 0, 1));
    }

    public Vector3 FractionalToCartesian(Vector3 posFractional)
    {
        Vector4 tmp = posFractional;
        tmp = LatticeMatrix * tmp;
        return (Vector3)tmp;
    }
}

public class ArcFile
{
    public List<ArcStructure> Structures
    {
        get;
        set;
    }

    public ArcFile()
    {
        Structures = new List<ArcStructure>();
    }
}
