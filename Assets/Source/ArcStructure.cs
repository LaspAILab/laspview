using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ArcStructure
{
    public float Energy { get; set; }

    public float Conv { get; set; }

    public int PathId { get; set; }

    public int NumStr { get; set; }

    public int NumMinimum { get; set; }

    public int NoPathway { get; set; }

    public Matrix4x4 LatticeMatrix { get; set; }

    public Matrix4x4 InvLatticeMatrix { get; set; }

    // Lattice parameters: (a, b, c, α, β, γ)
    public float[] LatticeParams { get; set; }

    public List<Atom> Atoms { get; set; }

    public int AtomIndex { get; set; } = 0;

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

    public Atom AddAtom(string elemSymbol, Vector3 coords, float charge)
    {
        if (!App.Elements.ContainsKey(elemSymbol))
        {
            throw new ArgumentException("Element symbol not found in the element data dictionary.");
        }

        var newAtom = new Atom
        {
            Index = AtomIndex++,
            Symbol = elemSymbol,
            Coordinates = coords,
            Charge = charge
        };
        Atoms.Add(newAtom);
        return newAtom;
    }

    public Atom AddAtom(Atom atom)
    {
        if (!App.Elements.ContainsKey(atom.Symbol))
        {
            throw new ArgumentException("Element symbol not found in the element data dictionary.");
        }

        atom.Index = AtomIndex++;
        Atoms.Add(atom);
        return atom;
    }

    public bool RemoveAtom(Atom atom)
    {
        return Atoms.Remove(atom);
    }

    public void SetLattice(float[] latticeParams)
    {
        if (latticeParams.Length != 6)
        {
            throw new ArgumentException("Lattice parameters must have 6 elements.");
        }

        float bc2 = latticeParams[1] * latticeParams[1] + latticeParams[2] * latticeParams[2] - 2 * latticeParams[1] *
            latticeParams[2] * Mathf.Cos(latticeParams[3] / 180.0f * Mathf.PI);
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
        LatticeMatrix = new Matrix4x4(new Vector4(h1, 0, 0, 0), new Vector4(h2, h3, 0, 0), new Vector4(h4, h5, h6, 0),
            new Vector4(0, 0, 0, 1));
        InvLatticeMatrix = LatticeMatrix.inverse;
    }

    public Vector3 FractionalToCartesian(Vector3 posFractional)
    {
        Vector4 tmp = new Vector4(posFractional.x, posFractional.y, posFractional.z, 1);
        tmp = LatticeMatrix * tmp;
        return (Vector3)tmp;
    }

    public Vector3 CartesianToFractional(Vector3 posCartesian)
    {
        Vector4 tmp = new Vector4(posCartesian.x, posCartesian.y, posCartesian.z, 1);
        tmp = InvLatticeMatrix * tmp;
        return (Vector3)tmp;
    }
}

public class ArcFile
{
    public List<ArcStructure> Structures { get; private set; }

    public int StructureCount => Structures.Count;

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

    // Remember to recalculate bonds after change
    public float BondTolerance { get; set; }

    public ArcStructureView(ArcStructure structure, float bondTolerance = 1.0f)
    {
        BondTolerance = bondTolerance;
        Structure = structure;
        BondMutex = new();

        Atoms.Capacity = structure.Atoms.Count;
        foreach (var item in structure.Atoms)
        {
            Atoms.Add(new AtomView(item, structure));
        }

        TranslationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
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

    public void GenerateBonds()
    {
        BondMutex.WaitOne();
        Bonds.Clear();
        for (int i = 0; i < Atoms.Count; i++)
        {
            UpdateAtomBondsNoLock(Atoms[i], BondTolerance, false, true);
        }

        BondMutex.ReleaseMutex();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateAtomBonds(AtomView targetAtom, float tolerance, bool cleanExisting = false,
        bool skipForward = false)
    {
        BondMutex.WaitOne();
        UpdateAtomBondsNoLock(targetAtom, tolerance, cleanExisting, skipForward);
        BondMutex.ReleaseMutex();
    }

    public void UpdateAtomBondsNoLock(AtomView targetAtom, float tolerance, bool cleanExisting = false,
        bool skipForward = false)
    {
        if (cleanExisting)
        {
            Bonds.RemoveAll(bond => bond.FromAtom == targetAtom || bond.ToAtom == targetAtom);
        }

        int atomIndex = Atoms.IndexOf(targetAtom);

        for (int i = 0; i < Atoms.Count; i++)
        {
            if (skipForward && i <= atomIndex)
            {
                continue;
            }

            if (Atoms[i] == targetAtom)
            {
                continue;
            }

            float radiusA = App.Elements[targetAtom.Symbol].Radius;
            float radiusB = App.Elements[Atoms[i].Symbol].Radius;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4 GetLatticeTransform(int aIndex, int bIndex, int cIndex)
    {
        Vector3 translateCart = Structure.FractionalToCartesian(new Vector3(aIndex, bIndex, cIndex));
        return Matrix4x4.Translate(translateCart);
    }

    /// <summary>
    /// Sort atom and bond list for more efficient draw cell batching
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SortLists()
    {
        Atoms.Sort((a, b) => a.RenderMat.CompareTo(b.RenderMat));
        Bonds.Sort((a, b) => a.FromAtom.RenderMat.CompareTo(b.FromAtom.RenderMat));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateColliders()
    {
        foreach (var item in Atoms)
        {
            item.CreateCollider();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReleaseColliders()
    {
        foreach (var item in Atoms)
        {
            item.DestroyCollider();
        }
    }

    public void ChangeLattice(float[] latticeParams, bool keepCartesian = true)
    {
        if (keepCartesian)
        {
            Structure.SetLattice(latticeParams);
            // Just re-clip the atoms to lattice
            foreach (var item in Atoms)
            {
                item.SetPosition(item.SourceAtom.Coordinates, Structure, false, true);
            }
        }
        else
        {
            var prevCartToFract = Structure.InvLatticeMatrix;
            Structure.SetLattice(latticeParams);

            foreach (var item in Atoms)
            {
                Vector3 prevPos = item.SourceAtom.Coordinates;
                Vector3 fractional = prevCartToFract * new Vector4(prevPos.x, prevPos.y, prevPos.z, 1);
                item.SetPosition(Structure.FractionalToCartesian(fractional), Structure, false, true);
            }
        }
        
        TranslationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
    }

    public void ToSuperCell(int repeatA, int repeatB, int repeatC)
    {
        List<Atom> newAtoms = new();

        for (int i = 0; i < repeatA; i++)
        {
            for (int j = 0; j < repeatB; j++)
            {
                for (int k = 0; k < repeatC; k++)
                {
                    if (i == 0 && j == 0 && k == 0)
                    {
                        continue;
                    }

                    var latticeTransform = GetLatticeTransform(i, j, k);
                    foreach (var item in Atoms)
                    {
                        Vector3 prevPos = item.SourceAtom.Coordinates;
                        newAtoms.Add(new Atom
                        {
                            Charge = item.SourceAtom.Charge,
                            Coordinates = latticeTransform * new Vector4(prevPos.x, prevPos.y, prevPos.z, 1),
                            Symbol = item.SourceAtom.Symbol,
                        });
                    }
                }
            }
        }

        Structure.SetLattice(new[]
        {
            Structure.LatticeParams[0] * repeatA, Structure.LatticeParams[1] * repeatB,
            Structure.LatticeParams[2] * repeatC,
            Structure.LatticeParams[3], Structure.LatticeParams[4], Structure.LatticeParams[5]
        });
        
        TranslationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();

        foreach (var item in newAtoms)
        {
            AddAtom(item, false);
        }
    }

    public AtomView AddAtom(string elemSymbol, Vector3 coords, float charge, bool updateBonds = true)
    {
        var newAtom = Structure.AddAtom(elemSymbol, coords, charge);
        var newAtomView = new AtomView(newAtom, Structure);
        Atoms.Add(newAtomView);

        newAtomView.CreateCollider();
        if (updateBonds)
        {
            UpdateAtomBonds(newAtomView, BondTolerance);
        }

        return newAtomView;
    }

    public AtomView AddAtom(Atom atom, bool updateBonds = true)
    {
        var newAtom = Structure.AddAtom(atom);
        var newAtomView = new AtomView(newAtom, Structure);
        Atoms.Add(newAtomView);

        newAtomView.CreateCollider();
        if (updateBonds)
        {
            UpdateAtomBonds(newAtomView, BondTolerance);
        }

        return newAtomView;
    }

    public void DeleteAtom(AtomView atomView)
    {
        // Remove bonds
        Bonds.RemoveAll(bond => bond.FromAtom == atomView || bond.ToAtom == atomView);
        // Remove source atom
        Structure.RemoveAtom(atomView.SourceAtom);
        // Remove atom view
        atomView.DestroyCollider();
        Atoms.Remove(atomView);
    }

    ~ArcStructureView()
    {
        BondMutex.Dispose();
    }
}