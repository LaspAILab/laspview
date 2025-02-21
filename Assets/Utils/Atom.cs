using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class Atom
{
    public int Index;
    public float Charge;
    public Vector3 Coordinates;
    public string Symbol;
}

interface ITransform
{
    public Matrix4x4 Transform { get; }
}

public class AtomView : ITransform
{
    /// <summary>
    /// Clip the position of the atom
    /// </summary>
    /// <param name="posCartesian">The cartesian position of the atom</param>
    /// <param name="structure">The parent structure of the atom</param>
    /// <param name="repeat">If true, then use repeat, else use clamp</param>
    /// <returns></returns>
    public static Vector3 ClipAtomCoords(Vector3 posCartesian, ArcStructure structure, bool repeat = true)
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

    public AtomView(Atom atom, ArcStructure structure)
    {
        SourceAtom = atom;
        Symbol = atom.Symbol;
        RenderMat = atom.Symbol;
        Charge = atom.Charge;
        Position = ClipAtomCoords(atom.Coordinates, structure);
        Radius = ConFun.Elements[atom.Symbol].Radius;
        transformCache = null;
        Collider = null;
    }

    public void SetPosition(Vector3 posCartesian, ArcStructure parentStructure)
    {
        SourceAtom.Coordinates = posCartesian;
        Position = ClipAtomCoords(SourceAtom.Coordinates, parentStructure, false);
        transformCache = null;

        // Move collider
        if (Collider != null)
        {
            Collider.SetAtom(this, Position, Radius);
        }
    }

    public void SetElement(string newSymbol)
    {
        SourceAtom.Symbol = newSymbol;
        Radius = ConFun.Elements[SourceAtom.Symbol].Radius;

        if (RenderMat == Symbol)
        {
            RenderMat = Symbol = newSymbol;
        }
        else
        {
            Symbol = newSymbol;
        }
    }

    public void CreateTransformCache(float radiusScale = 1f)
    {
        transformCache = Matrix4x4.TRS(Position, Quaternion.identity, Radius * radiusScale * Vector3.one);
    }

    public void CreateCollider()
    {
        Collider = ColliderPool.GlobalPool.BorrowCollider();
        Collider.SetAtom(this, Position, Radius);
    }

    public void DestroyCollider()
    {
        if (Collider != null)
        {
            ColliderPool.GlobalPool.DropCollider(Collider);
            Collider = null;
        }
    }

    ~AtomView()
    {
        DestroyCollider();
    }

    public Atom SourceAtom
    {
        get;
        private set;
    }

    public Vector3 Position
    {
        get;
        private set;
    }

    public string Symbol;
    // A string that marks which material the atom is rendered with, 
    // this is used to batch render atoms with the same material
    public string RenderMat;
    public float Charge;
    public float Radius;
    public ColliderScript Collider;

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

public class BondView : ITransform
{
    public AtomView FromAtom;
    public AtomView ToAtom;
    public Vector3 Origin;
    // Translation vector from `fromAtom` to `toAtom`
    public Vector3 Theta;
    // Fraction of the whole bond length taken by `fromAtom`
    public float Rate;

    public void CreateTransformCache(float stickRadius = 0.35f)
    {
        Vector3 bondCenter = Origin + 0.5f * Rate * Theta;
        Quaternion rotation = Quaternion.AngleAxis(-Vector3.Angle(Vector3.up, Theta), new Vector3(-Theta.z, 0, Theta.x));
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
