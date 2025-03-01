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
#if !UNITY_WEBGL
    public Mutex ListMutex;
#endif

    public List<ArcStructure> Structures { get; private set; }

    public int StructureCount => Structures.Count;

    public ArcFile()
    {
        Structures = new();
#if !UNITY_WEBGL
        ListMutex = new();
#endif
    }

    ~ArcFile()
    {
#if !UNITY_WEBGL
        ListMutex.Dispose();
#endif
        Debug.Log("Disposing arc file");
    }
}

// The view model of an ARC structure
public class ArcStructureView
{
    // Vectors to translate the lattice, in fractional coords, used to check cross-lattice bonds
    static readonly Vector3[] TranslationVectorsFractional =
    {
        new(1, 0, 0),
        new(0, 1, 0),
        new(0, 0, 1),
        new(-1, 0, 0),
        new(0, -1, 0),
        new(0, 0, -1),
    };

    // Translation vectors, converted to cartesian space
    Vector3[] translationVectorsCartesian;

    public ArcStructure Structure;

    public List<AtomView> Atoms = new();

    public List<BondView> Bonds = new();

#if !UNITY_WEBGL
    public Mutex BondMutex;
#endif

    // Remember to recalculate bonds after change
    public float BondTolerance { get; set; }

    private PointOctree<AtomView> accelerationOctree;
    private float maxAtomRadius = 0f;

    public ArcStructureView(ArcStructure structure, float bondTolerance = 1.0f)
    {
        BondTolerance = bondTolerance;
        Structure = structure;
#if !UNITY_WEBGL
        BondMutex = new();
#endif

        var latticeCenter = structure.FractionalToCartesian(new Vector3(0.5f, 0.5f, 0.5f));
        var maxSide = Mathf.Max(structure.LatticeParams[0], structure.LatticeParams[1], structure.LatticeParams[2]);
        accelerationOctree = new PointOctree<AtomView>(maxSide, latticeCenter, 10f);
        Atoms.Capacity = structure.Atoms.Count;
        foreach (var item in structure.Atoms)
        {
            AddAtomView(new AtomView(item, structure));
        }

        translationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAtomView(AtomView atomView)
    {
        Atoms.Add(atomView);
        accelerationOctree.Add(atomView, atomView.Position);
        maxAtomRadius = Mathf.Max(maxAtomRadius, atomView.Radius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveAtomView(AtomView atomView)
    {
        Atoms.Remove(atomView);
        accelerationOctree.Remove(atomView);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAtomPosition(AtomView atomView, Vector3 posCartesian, bool viewOnly, bool repeat = true)
    {
        if (!viewOnly)
        {
            atomView.SourceAtom.Coordinates = posCartesian;
        }

        atomView.Position = AtomView.ClipAtomCoords(posCartesian, Structure, repeat);
        atomView.InvalidateTransform();
        accelerationOctree.Remove(atomView);
        accelerationOctree.Add(atomView, atomView.Position);

        // A faster way for null check since Unity uses a slow overload for ==
        if (!viewOnly && atomView.Collider is not null)
        {
            // Move collider
            atomView.Collider.SetAtom(atomView, atomView.Position, atomView.Radius);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAtomElement(AtomView atomView, string newSymbol)
    {
        atomView.SourceAtom.Symbol = newSymbol;
        atomView.Radius = App.Elements[atomView.SourceAtom.Symbol].Radius;

        if (atomView.RenderMat == atomView.Symbol)
        {
            atomView.RenderMat = atomView.Symbol = newSymbol;
        }
        else
        {
            atomView.Symbol = newSymbol;
        }

        // Since the radius has changed, we have to renew the transform cache
        atomView.InvalidateTransform();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
#if !UNITY_WEBGL
        BondMutex.WaitOne();
#endif
        Bonds.Clear();
        for (int i = 0; i < Atoms.Count; i++)
        {
            UpdateAtomBondsNoLock(Atoms[i], BondTolerance, false, true);
        }
#if !UNITY_WEBGL
        BondMutex.ReleaseMutex();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateAtomBonds(AtomView targetAtom, float tolerance, bool cleanExisting = false,
        bool skipForward = false)
    {
#if !UNITY_WEBGL
        BondMutex.WaitOne();
#endif
        UpdateAtomBondsNoLock(targetAtom, tolerance, cleanExisting, skipForward);
#if !UNITY_WEBGL
        BondMutex.ReleaseMutex();
#endif
    }

    public void UpdateAtomBondsNoLock(AtomView targetAtom, float tolerance, bool cleanExisting = false,
        bool skipForward = false)
    {
        if (cleanExisting)
        {
            Bonds.RemoveAll(bond => bond.FromAtom == targetAtom || bond.ToAtom == targetAtom);
        }

        float maxDistance = tolerance * (targetAtom.Radius + maxAtomRadius);
        List<AtomView> nearbyAtoms = new();
        accelerationOctree.GetNearbyNonAlloc(targetAtom.Position, maxDistance, nearbyAtoms);

        foreach (var otherAtom in nearbyAtoms)
        {
            if (skipForward && otherAtom.SourceAtom.Index <= targetAtom.SourceAtom.Index)
            {
                continue;
            }

            if (otherAtom == targetAtom)
            {
                continue;
            }

            float sumRadius = targetAtom.Radius + otherAtom.Radius;
            float distance = Vector3.Distance(targetAtom.Position, otherAtom.Position);
            if (distance < tolerance * sumRadius)
            {
                Vector3 theta = otherAtom.Position - targetAtom.Position;
                AddBond(targetAtom, otherAtom, theta, targetAtom.Radius / sumRadius);
            }
        }

        // Check bond across lattice
        for (int i = 0; i < translationVectorsCartesian.Length; i++)
        {
            var translatedCoord = targetAtom.Position + translationVectorsCartesian[i];
            accelerationOctree.GetNearbyNonAlloc(translatedCoord, maxDistance, nearbyAtoms);

            foreach (var otherAtom in nearbyAtoms)
            {
                if (skipForward && otherAtom.SourceAtom.Index <= targetAtom.SourceAtom.Index)
                {
                    continue;
                }

                if (otherAtom == targetAtom)
                {
                    continue;
                }

                float sumRadius = targetAtom.Radius + otherAtom.Radius;
                float distance = Vector3.Distance(translatedCoord, otherAtom.Position);
                if (distance < tolerance * sumRadius)
                {
                    Vector3 theta = otherAtom.Position - translatedCoord;
                    AddBond(targetAtom, otherAtom, theta, targetAtom.Radius / sumRadius);
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
                SetAtomPosition(item, item.SourceAtom.Coordinates, false, true);
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
                SetAtomPosition(item, Structure.FractionalToCartesian(fractional), false, true);
            }
        }

        translationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
    }

    public void ToSuperCell(int repeatA, int repeatB, int repeatC)
    {
        List<Atom> newAtoms = new(Atoms.Count * (repeatA + repeatB + repeatC - 1));
        Atoms.Capacity = Atoms.Count * (repeatA + repeatB + repeatC);

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

        translationVectorsCartesian =
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
        AddAtomView(newAtomView);

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
        AddAtomView(newAtomView);

        newAtomView.CreateCollider();
        if (updateBonds)
        {
            UpdateAtomBonds(newAtomView, BondTolerance);
        }

        return newAtomView;
    }

    public void RemoveAtom(AtomView atomView)
    {
        // Remove bonds
        Bonds.RemoveAll(bond => bond.FromAtom == atomView || bond.ToAtom == atomView);
        // Remove source atom
        Structure.RemoveAtom(atomView.SourceAtom);
        // Remove atom view
        atomView.DestroyCollider();
        RemoveAtomView(atomView);
    }

    ~ArcStructureView()
    {
#if !UNITY_WEBGL
        BondMutex.Dispose();
#endif
        Debug.Log("Disposing arc structure view");
    }
}