using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CrystalStructure
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

    public CrystalStructure()
    {
        Energy = 0;
        Conv = 0;
        PathId = 0;
        NumStr = 0;
        NumMinimum = 0;
        NoPathway = 0;
        LatticeMatrix = InvLatticeMatrix = Matrix4x4.identity;
        LatticeParams = null;
        Atoms = new();
    }

    public Atom AddAtom(string elemSymbol, Vector3 coords, float charge, AtomFlags flags = AtomFlags.None)
    {
        if (!App.Elements.ContainsKey(elemSymbol))
        {
            throw new ArgumentException("Element symbol not found in the element data dictionary.");
        }

        var newAtom = new Atom
        {
            Index = AtomIndex++,
            Symbol = elemSymbol,
            Coordinates = ClipAtomCoords(coords, true),
            Charge = charge,
            Flags = flags
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
    
    public void AppendAtoms(IEnumerable<Atom> atoms)
    {
        foreach (var atom in atoms)
        {
            AddAtom(atom);
        }
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

    public void SetLattice(Vector3[] baseVectors)
    {
        if (baseVectors.Length != 3)
        {
            throw new ArgumentException("Lattice must have exactly 3 base vectors.");
        }

        LatticeParams = new float[6];
        LatticeParams[0] = baseVectors[0].magnitude;
        LatticeParams[1] = baseVectors[1].magnitude;
        LatticeParams[2] = baseVectors[2].magnitude;
        LatticeParams[3] = Vector3.Angle(baseVectors[1], baseVectors[2]);
        LatticeParams[4] = Vector3.Angle(baseVectors[0], baseVectors[2]);
        LatticeParams[5] = Vector3.Angle(baseVectors[0], baseVectors[1]);

        LatticeMatrix = new Matrix4x4(new Vector4(baseVectors[0].x, baseVectors[1].x, baseVectors[2].x, 0),
            new Vector4(baseVectors[0].y, baseVectors[1].y, baseVectors[2].y, 0),
            new Vector4(baseVectors[0].z, baseVectors[1].z, baseVectors[2].z, 0),
            new Vector4(0, 0, 0, 1));
        InvLatticeMatrix = LatticeMatrix.inverse;
    }

    public Vector3[] GetBaseVectors()
    {
        var baseVectors = new Vector3[3];

        for (int i = 0; i < 3; i++)
        {
            baseVectors[i] = new Vector3(LatticeMatrix[0, i], LatticeMatrix[1, i], LatticeMatrix[2, i]);
        }

        return baseVectors;
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

    /// <summary>
    /// Clip the position of the atom
    /// </summary>
    /// <param name="posCartesian">The cartesian position of the atom</param>
    /// <param name="structure">The parent structure of the atom</param>
    /// <param name="repeat">If true, then use repeat, else use clamp</param>
    /// <returns></returns>
    public Vector3 ClipAtomCoords(Vector3 posCartesian, bool repeat = true)
    {
        // Clip atom to lattice (0, 0, 0) to (1, 1, 1)
        Vector3 fract = CartesianToFractional(posCartesian);
        if (repeat)
        {
            fract = new Vector3(
                fract.x - Mathf.Floor(fract.x),
                fract.y - Mathf.Floor(fract.y),
                fract.z - Mathf.Floor(fract.z)
            );
        }
        else
        {
            fract = new Vector3(
                Mathf.Clamp01(fract.x),
                Mathf.Clamp01(fract.y),
                Mathf.Clamp01(fract.z)
            );
        }

        return FractionalToCartesian(fract);
    }

    public CrystalStructure DeepClone()
    {
        var copy = new CrystalStructure
        {
            Energy = Energy,
            Conv = Conv,
            PathId = PathId,
            NumStr = NumStr,
            NumMinimum = NumMinimum,
            NoPathway = NoPathway,
            // Matrices are value types, so no need to clone
            LatticeMatrix = LatticeMatrix,
            InvLatticeMatrix = InvLatticeMatrix,
            // Do a clone of the lattice params
            LatticeParams = LatticeParams.ToArray(),
            AtomIndex = AtomIndex,
            // Reserve space
            Atoms = new List<Atom>(Atoms.Count)
        };

        // Deep clone all atoms
        foreach (var atom in Atoms)
        {
            copy.Atoms.Add(new Atom
            {
                Index = atom.Index,
                Symbol = atom.Symbol,
                Coordinates = atom.Coordinates,
                Charge = atom.Charge,
                Flags = atom.Flags
            });
        }

        return copy;
    }
}

public class StructureFile
{
#if !UNITY_WEBGL
    public Mutex ListMutex;
#endif

    // Notifier for when a new structure is appended to this file
    public bool NewStructureLoadedNotifier { get; set; } = false;

    // Whether it is queued to open the first structure after loaded
    public bool IsOpenStructureQueued { get; set; } = false;

    public List<CrystalStructure> Structures { get; private set; }

    public int StructureCount => Structures.Count;

    public string Filename { get; private set; }

    public StructureFile(string filename)
    {
        Filename = filename;
        Structures = new();
#if !UNITY_WEBGL
        ListMutex = new();
#endif
    }

    ~StructureFile()
    {
#if !UNITY_WEBGL
        ListMutex.Dispose();
#endif
        Debug.Log($"Disposing structure file {Filename}");
    }
}

// The view model of an ARC structure
public class CrystalStructureView
{
    // Vectors to translate the lattice, in fractional coords, used to check cross-lattice bonds
    public static readonly Vector3[] TranslationVectorsFractional =
    {
        // Adjacent
        new(1, 0, 0),
        new(0, 1, 0),
        new(0, 0, 1),
        new(-1, 0, 0),
        new(0, -1, 0),
        new(0, 0, -1),
        // Diagonal
        new(1, 1, 0),
        new(-1, -1, 0),
        new(-1, 1, 0),
        new(1, -1, 0),
        new(0, 1, 1),
        new(0, -1, -1),
        new(0, -1, 1),
        new(0, 1, -1),
        new(1, 0, 1),
        new(-1, 0, -1),
        new(-1, 0, 1),
        new(1, 0, -1),
        // Body diagonal
        new(1, 1, 1),
        new(-1, 1, 1),
        new(1, -1, 1),
        new(1, 1, -1),
        new(-1, -1, 1),
        new(1, -1, -1),
        new(-1, 1, -1),
        new(-1, -1, -1),
    };

    public static readonly int[] AdjacentCellCounts = { 0, 6, 6 + 12, 6 + 12 + 8 };

    // Remember to recalculate bonds after change
    public static float BondTolerance { get; set; } = 1.26f;

    public static CrossCellBondPolicy CrossCellPolicy { get; set; } = CrossCellBondPolicy.Adjacent;

    // Translation vectors of adjacent cells, converted to cartesian space
    public Vector3[] TranslationVectorsCartesian;

    public CrystalStructure Structure;

    public List<AtomView> Atoms = new();

    public List<BondView> Bonds = new();

#if !UNITY_WEBGL
    public Mutex BondMutex;
#endif

    public PointOctree<AtomView> AccelerationOctree { get; private set; }

    private float maxAtomRadius = 0f;

    public CrystalStructureView(CrystalStructure structure)
    {
        Structure = structure;
#if !UNITY_WEBGL
        BondMutex = new();
#endif

        var latticeCenter = structure.FractionalToCartesian(new Vector3(0.5f, 0.5f, 0.5f));
        var maxSide = Mathf.Max(structure.LatticeParams[0], structure.LatticeParams[1], structure.LatticeParams[2]);
        AccelerationOctree = new PointOctree<AtomView>(maxSide, latticeCenter, 10f);
        Atoms.Capacity = structure.Atoms.Count;
        foreach (var item in structure.Atoms)
        {
            AddAtomView(new AtomView(item, structure));
        }

        TranslationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAtomView(AtomView atomView)
    {
        Atoms.Add(atomView);
        AccelerationOctree.Add(atomView, atomView.Position);
        maxAtomRadius = Mathf.Max(maxAtomRadius, atomView.Radius);
        atomView.InLattice = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveAtomView(AtomView atomView)
    {
        Atoms.Remove(atomView);
        AccelerationOctree.Remove(atomView);
        atomView.InLattice = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAtomPosition(AtomView atomView, Vector3 posCartesian, bool viewOnly, bool repeat = true)
    {
        var clippedPos = Structure.ClipAtomCoords(posCartesian, repeat);
        if (!viewOnly)
        {
            atomView.SourceAtom.Coordinates = clippedPos;
        }

        atomView.Position = clippedPos;
        atomView.InvalidateTransform();
        AccelerationOctree.Remove(atomView);
        AccelerationOctree.Add(atomView, atomView.Position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAtomElement(AtomView atomView, string newSymbol)
    {
        atomView.SourceAtom.Symbol = newSymbol;
        atomView.Symbol = newSymbol;
        atomView.Radius = App.Elements[atomView.SourceAtom.Symbol].Radius;
        atomView.DisplayColor = App.Elements[atomView.SourceAtom.Symbol].Color;
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
        AccelerationOctree.GetNearbyNonAlloc(targetAtom.Position, maxDistance, nearbyAtoms);

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
        var adjacentCellCount = AdjacentCellCounts[(int)CrossCellPolicy];
        for (int i = 0; i < adjacentCellCount; i++)
        {
            var translatedCoord = targetAtom.Position + TranslationVectorsCartesian[i];
            AccelerationOctree.GetNearbyNonAlloc(translatedCoord, maxDistance, nearbyAtoms);

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

        TranslationVectorsCartesian =
            TranslationVectorsFractional.Select(v => Structure.FractionalToCartesian(v)).ToArray();
    }

    // Alternative implementation
    private void __ToSuperCell(int repeatA, int repeatB, int repeatC)
    {
        int originalAtomCount = Atoms.Count, originalBondCount = Bonds.Count;
        var mats = new Matrix4x4[repeatA, repeatB, repeatC];

        Dictionary<AtomView, int> reverseTable = new();
        for (int i = 0; i < Atoms.Count; i++)
        {
            reverseTable[Atoms[i]] = i;
        }

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

                    mats[i, j, k] = GetLatticeTransform(i, j, k);
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

        Atoms.Capacity = Atoms.Count * (repeatA + repeatB + repeatC);
        Bonds.Capacity = Bonds.Count * (repeatA + repeatB + repeatC);

        var copyIndex = 1;
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

                    var latticeTransform = mats[i, j, k];
                    for (int index = 0; index < originalAtomCount; index++)
                    {
                        var item = Atoms[index];
                        Vector3 prevPos = item.SourceAtom.Coordinates;
                        AddAtom(
                            new Atom
                            {
                                Symbol = item.SourceAtom.Symbol,
                                Coordinates = latticeTransform * new Vector4(prevPos.x, prevPos.y, prevPos.z, 1)
                            }, false);
                    }

                    int offset = originalAtomCount * copyIndex;
                    for (var index = 0; index < originalBondCount; index++)
                    {
                        var item = Bonds[index];
                        var fromAtomIndex = reverseTable[item.FromAtom];
                        var toAtomIndex = reverseTable[item.ToAtom];
                        AddBond(Atoms[fromAtomIndex + offset], Atoms[toAtomIndex + offset], item.Theta, item.Rate);
                    }

                    copyIndex++;
                }
            }
        }
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
                        item.SourceAtom.Coordinates = item.Position;
                        Vector3 prevPos = item.Position;
                        newAtoms.Add(new Atom
                        {
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

    public AtomView AddAtom(Atom atom, bool updateBonds = true)
    {
        var newAtom = Structure.AddAtom(atom);
        var newAtomView = new AtomView(newAtom, Structure);
        AddAtomView(newAtomView);

        if (updateBonds)
        {
            UpdateAtomBonds(newAtomView, BondTolerance);
        }

        return newAtomView;
    }

    // Revive a deleted atom, keeping existing references valid
    public void ReviveAtom(AtomView atomView)
    {
        Structure.AddAtom(atomView.SourceAtom);
        AddAtomView(atomView);

        UpdateAtomBonds(atomView, BondTolerance);
    }

    public void RemoveAtom(AtomView atomView)
    {
        // Remove bonds
        Bonds.RemoveAll(bond => bond.FromAtom == atomView || bond.ToAtom == atomView);
        // Remove source atom
        Structure.RemoveAtom(atomView.SourceAtom);
        atomView.InvalidateTransform();
        RemoveAtomView(atomView);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPositionInLattice(Vector3 positionCartesian)
    {
        Vector3 fractional = Structure.CartesianToFractional(positionCartesian);
        return fractional is { x: >= 0 and <= 1, y: >= 0 and <= 1, z: >= 0 and <= 1 };
    }

    ~CrystalStructureView()
    {
#if !UNITY_WEBGL
        BondMutex.Dispose();
#endif
        Debug.Log("Disposing arc structure view");
    }
}