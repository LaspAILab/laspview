using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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

    public Matrix4x4 InvLatticeMatrix
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

    public int AtomIndex
    {
        get;
        set;
    } = 0;

    public ArcStructure()
    {
        Energy = 0;
        Conv = 0;
        PathId = 0;
        NumStr = 0;
        NumMinimum = 0;
        NoPathway = 0;
        LatticeMatrix = InvLatticeMatrix = Matrix4x4.identity;
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
            Index = AtomIndex++,
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
        InvLatticeMatrix = LatticeMatrix.inverse;
    }

    public Vector3 FractionalToCartesian(Vector3 posFractional)
    {
        Vector4 tmp = posFractional;
        tmp = LatticeMatrix * tmp;
        return (Vector3)tmp;
    }

    public Vector3 CartesianToFractional(Vector3 posCartesian)
    {
        Vector4 tmp = posCartesian;
        tmp = InvLatticeMatrix * tmp;
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
        Structures = new();
    }
}

// The view model of an ARC structure
public class ArcStructureView
{
    // Vectors to translate the lattice, in fractional coords, used to check cross-lattice bonds
    public static readonly Vector3[] TranslationVectorsFractional = new Vector3[]
    {
        new(1, 0, 0),
        new(0, 1, 0),
        new(0, 0, 1),
    };

    // Translation vectors, converted to cartesian space
    public Vector3[] TranslationVectorsCartesian;

    public ArcStructure Structure;

    public List<AtomView> Atoms = new();

    public List<BondView> Bonds = new();

    public Mutex BondMutex;

    public ArcStructureView(ArcStructure structure)
    {
        Structure = structure;
        BondMutex = new();

        Atoms.Capacity = structure.Atoms.Count;
        foreach (var item in structure.Atoms)
        {
            Atoms.Add(new AtomView(item, structure));
        }

        TranslationVectorsCartesian = TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
    }

    public void EditAtom(int atomIndex, Vector3? newCoord = null, string newSymbol = null)
    {
        var targetAtom = Atoms[atomIndex].SourceAtom;

        if (newCoord != null)
        {
            targetAtom.Coordinates = newCoord.Value;
        }

        if (newSymbol != null)
        {
            Assert.IsTrue(ConFun.Elements.ContainsKey(newSymbol));
            targetAtom.Symbol = newSymbol;
        }

        Atoms[atomIndex] = new(targetAtom, Structure);
    }

    private void AddBond(AtomView fromAtom, AtomView toAtom, Vector3 theta, float rate)
    {
        Bonds.Add(new BondView
        {
            FromAtom = fromAtom,
            ToAtom = toAtom,
            Origin = fromAtom.Position,
            Theta = theta,
            Rate = rate
        });

        Bonds.Add(new BondView
        {
            FromAtom = toAtom,
            ToAtom = fromAtom,
            Origin = toAtom.Position,
            Theta = -theta,
            Rate = 1 - rate
        });
    }

    public void GenerateBonds(float tolerance)
    {
        var latticeTranslations = TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToList();

        BondMutex.WaitOne();
        Bonds.Clear();
        for (int i = 0; i < Atoms.Count; i++)
        {
            UpdateAtomBonds(Atoms[i], tolerance, false);
        }
        BondMutex.ReleaseMutex();
    }

    public void UpdateAtomBonds(AtomView targetAtom, float tolerance, bool cleanExisting = false)
    {
        if (cleanExisting)
        {
            Bonds.RemoveAll(bond => bond.FromAtom == targetAtom || bond.ToAtom == targetAtom);
        }

        for (int i = 0; i < Atoms.Count; i++)
        {
            if (Atoms[i] == targetAtom)
            {
                continue;
            }

            float radiusA = ConFun.Elements[targetAtom.Symbol].Radius;
            float radiusB = ConFun.Elements[Atoms[i].Symbol].Radius;
            float sumRadius = radiusA + radiusB;

            // Check bond within lattice
            float distance = Vector3.Distance(targetAtom.Position, Atoms[i].Position);
            if (distance < tolerance * sumRadius)
            {
                Vector3 theta = Atoms[i].Position - targetAtom.Position;
                float len = radiusA / sumRadius;
                AddBond(targetAtom, Atoms[i], theta, len);
            }

            // Check bond across lattice
            foreach (var item in TranslationVectorsCartesian)
            {
                var translatedCoord = Atoms[i].Position + item;
                distance = Vector3.Distance(targetAtom.Position, translatedCoord);
                if (distance < tolerance * sumRadius)
                {
                    Vector3 theta = translatedCoord - targetAtom.Position;
                    float len = radiusA / sumRadius;
                    AddBond(targetAtom, Atoms[i], theta, len);
                }

                translatedCoord = targetAtom.Position + item;
                distance = Vector3.Distance(Atoms[i].Position, translatedCoord);
                if (distance < tolerance * sumRadius)
                {
                    Vector3 theta = translatedCoord - Atoms[i].Position;
                    float len = radiusA / sumRadius;
                    AddBond(Atoms[i], targetAtom, theta, len);
                }
            }
        }
    }

    public Matrix4x4 GetLatticeTransform(int aIndex, int bIndex, int cIndex)
    {
        Vector3 translateCart = Structure.FractionalToCartesian(new Vector3(aIndex, bIndex, cIndex));
        return Matrix4x4.Translate(translateCart);
    }

    /// <summary>
    /// Sort atom and bond list for more efficient draw cell batching
    /// </summary>
    public void SortLists()
    {
        Atoms.Sort((a, b) => a.RenderMat.CompareTo(b.RenderMat));
        Bonds.Sort((a, b) => a.FromAtom.RenderMat.CompareTo(b.FromAtom.RenderMat));
    }

    public void CreateColliders()
    {
        foreach (var item in Atoms)
        {
            item.CreateCollider();
        }
    }

    public void ReleaseColliders()
    {
        foreach (var item in Atoms)
        {
            item.DestroyCollider();
        }
    }

    ~ArcStructureView()
    {
        BondMutex.Dispose();
        ReleaseColliders();
    }
}
