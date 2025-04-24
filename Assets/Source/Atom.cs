using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class Atom
{
    public int Index;
    public Vector3 Coordinates;
    public float Charge;

    // Flags about whether the atom is fixed, 1 means fixed
    public float FixedFlags;
    public string Symbol;
}

interface ITransform
{
    public Matrix4x4 Transform { get; }
}

public class AtomView : ITransform, IEquatable<AtomView>
{
    /// <summary>
    /// Clip the position of the atom
    /// </summary>
    /// <param name="posCartesian">The cartesian position of the atom</param>
    /// <param name="structure">The parent structure of the atom</param>
    /// <param name="repeat">If true, then use repeat, else use clamp</param>
    /// <returns></returns>
    public static Vector3 ClipAtomCoords(Vector3 posCartesian, CrystalStructure structure, bool repeat)
    {
        // Clip atom to lattice (0, 0, 0) to (1, 1, 1)
        Vector3 fract = structure.CartesianToFractional(posCartesian);
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

        return structure.FractionalToCartesian(fract);
    }

    public AtomView(Atom atom, CrystalStructure structure)
    {
        SourceAtom = atom;
        Symbol = atom.Symbol;
        IsSelected = false;
        Position = ClipAtomCoords(atom.Coordinates, structure, true);
        radius = App.Elements[atom.Symbol].Radius;
        DisplayColor = App.Elements[atom.Symbol].Color;
        transformCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateTransform()
    {
        transformCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateTransformCache(float radiusScale = 1f)
    {
        transformCache = Matrix4x4.TRS(Position, Quaternion.identity, Radius * radiusScale * Vector3.one);
    }

    public string Symbol;
    public bool InLattice = true;
    public Color DisplayColor;

    private Matrix4x4? transformCache;
    private float radius;

    public Atom SourceAtom { get; }

    public Vector3 Position
    {
        get;
        // Be careful calling this, use `SetPosition` instead
        internal set;
    }

    public bool IsSelected { get; set; }

    public float Radius
    {
        get => radius;
        set
        {
            InvalidateTransform();
            radius = value;
        }
    }

    public Matrix4x4 Transform
    {
        get
        {
            if (transformCache == null)
            {
                CreateTransformCache();
            }
            return transformCache.Value;
        }
    }

    public bool Equals(AtomView other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SourceAtom.Index == other.SourceAtom.Index;
    }

    public override bool Equals(object obj)
    {
        return obj is AtomView other && Equals(other);
    }

    public override int GetHashCode()
    {
        return SourceAtom.Index.GetHashCode();
    }
}

public class BondView : ITransform
{
    public AtomView FromAtom;
    public AtomView ToAtom;

    public Vector3 Origin;

    // Translation vector from `fromAtom` to `toAtom`
    public Vector3 Theta;

    // Fraction of the whole bond length taken by `fromAtom`
    public float Rate;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateTransformCache(float stickRadius = 0.35f)
    {
        Vector3 bondCenter = Origin + 0.5f * Rate * Theta;
        Quaternion rotation =
            Quaternion.AngleAxis(-Vector3.Angle(Vector3.up, Theta), new Vector3(-Theta.z, 0, Theta.x));
        Vector3 scale = new(stickRadius, Theta.magnitude * Rate * 0.5f, stickRadius);
        transformCache = Matrix4x4.TRS(bondCenter, rotation, scale);
    }

    private Matrix4x4? transformCache;

    public Matrix4x4 Transform
    {
        get
        {
            if (transformCache == null)
            {
                CreateTransformCache();
            }
            return transformCache.Value;
        }
    }
}