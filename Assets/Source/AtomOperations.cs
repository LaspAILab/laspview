using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class TranslateAtomsOperation : IOperation
{
    private List<AtomView> targetAtoms;
    private Vector3 translation;

    public TranslateAtomsOperation(List<AtomView> targetAtoms, Vector3 translation)
    {
        this.targetAtoms = targetAtoms;
        this.translation = translation;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            structureView.SetAtomPosition(item, item.SourceAtom.Coordinates + translation, false);
            ColliderManager.Instance.UpdateColliderForAtom(item);
        }

#if !UNITY_WEBGL
        structureView.BondMutex.WaitOne();
#endif
        foreach (var item in targetAtoms)
        {
            structureView.UpdateAtomBondsNoLock(item, App.Instance.BondTolerance, true);
        }
#if !UNITY_WEBGL
        structureView.BondMutex.ReleaseMutex();
#endif

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            structureView.SetAtomPosition(item, item.SourceAtom.Coordinates - translation, false);
            ColliderManager.Instance.UpdateColliderForAtom(item);
        }

#if !UNITY_WEBGL
        structureView.BondMutex.WaitOne();
#endif
        foreach (var item in targetAtoms)
        {
            structureView.UpdateAtomBondsNoLock(item, App.Instance.BondTolerance, true);
        }
#if !UNITY_WEBGL
        structureView.BondMutex.ReleaseMutex();
#endif

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class RepositionAtomsOperation : IOperation
{
    private List<AtomView> targetAtoms;
    private List<Vector3> newPositions, oldPositions;

    public RepositionAtomsOperation(List<AtomView> targetAtoms, List<Vector3> newPositions)
    {
        this.targetAtoms = targetAtoms;
        this.newPositions = newPositions;
        Assert.AreEqual(newPositions.Count, targetAtoms.Count);
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldPositions = new List<Vector3>(targetAtoms.Count);
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            oldPositions.Add(targetAtoms[i].SourceAtom.Coordinates);
            structureView.SetAtomPosition(targetAtoms[i], newPositions[i], false);
            ColliderManager.Instance.UpdateColliderForAtom(targetAtoms[i]);
        }

#if !UNITY_WEBGL
        structureView.BondMutex.WaitOne();
#endif
        foreach (var item in targetAtoms)
        {
            structureView.UpdateAtomBondsNoLock(item, App.Instance.BondTolerance, true);
        }
#if !UNITY_WEBGL
        structureView.BondMutex.ReleaseMutex();
#endif

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            structureView.SetAtomPosition(targetAtoms[i], oldPositions[i], false);
            ColliderManager.Instance.UpdateColliderForAtom(targetAtoms[i]);
        }

#if !UNITY_WEBGL
        structureView.BondMutex.WaitOne();
#endif
        foreach (var item in targetAtoms)
        {
            structureView.UpdateAtomBondsNoLock(item, App.Instance.BondTolerance, true);
        }
#if !UNITY_WEBGL
        structureView.BondMutex.ReleaseMutex();
#endif

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class SetPositionOperation : IOperation
{
    private AtomView targetAtom;
    private Vector3 newPosition, oldPosition;

    public SetPositionOperation(AtomView targetAtom, Vector3 newPosition)
    {
        this.targetAtom = targetAtom;
        this.newPosition = newPosition;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldPosition = targetAtom.Position;
        structureView.SetAtomPosition(targetAtom, newPosition, false);
        ColliderManager.Instance.UpdateColliderForAtom(targetAtom);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        structureView.SetAtomPosition(targetAtom, oldPosition, false);
        ColliderManager.Instance.UpdateColliderForAtom(targetAtom);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class SetElementOperation : IOperation
{
    private AtomView targetAtom;
    private string newSymbol, oldSymbol;

    public SetElementOperation(AtomView targetAtom, string newSymbol)
    {
        this.targetAtom = targetAtom;
        this.newSymbol = newSymbol;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldSymbol = targetAtom.Symbol;
        structureView.SetAtomElement(targetAtom, newSymbol);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        structureView.SetAtomElement(targetAtom, oldSymbol);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class SetChargeOperation : IOperation
{
    private AtomView targetAtom;
    private float newCharge, oldCharge;

    public SetChargeOperation(AtomView targetAtom, float newCharge)
    {
        this.targetAtom = targetAtom;
        this.newCharge = newCharge;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldCharge = targetAtom.SourceAtom.Charge;
        targetAtom.SourceAtom.Charge = newCharge;
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        targetAtom.SourceAtom.Charge = oldCharge;
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.None;
}

public class SetFixedOperation : IOperation
{
    private List<AtomView> targetAtoms;
    private float[] oldFixedFlags;
    private bool isFixed;

    public SetFixedOperation(List<AtomView> targetAtoms, bool isFixed)
    {
        this.targetAtoms = targetAtoms;
        this.isFixed = isFixed;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldFixedFlags = new float[targetAtoms.Count];
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            oldFixedFlags[i] = targetAtoms[i].SourceAtom.FixedFlags;
            targetAtoms[i].SourceAtom.FixedFlags = isFixed ? 1f : 0f;
        }

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            targetAtoms[i].SourceAtom.FixedFlags = oldFixedFlags[i];
        }

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.None;
}

public class AddAtomOperation : IOperation
{
    private AtomView addedAtom = null;
    private Atom atomToAdd;

    public AddAtomOperation(Vector3 position, string symbol, float charge = 0)
    {
        atomToAdd = new Atom
        {
            Symbol = symbol,
            Charge = charge,
            Coordinates = position
        };
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        if (addedAtom == null)
        {
            addedAtom = structureView.AddAtom(atomToAdd);
        }
        else
        {
            structureView.ReviveAtom(addedAtom);
        }

        ColliderManager.Instance.CreateColliderForAtom(addedAtom);
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        ColliderManager.Instance.DropColliderForAtom(addedAtom);
        structureView.RemoveAtom(addedAtom);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class RemoveAtomsOperation : IOperation
{
    private List<AtomView> removedAtoms;
    private Vector3 position;
    private string symbol;
    private float charge;

    public RemoveAtomsOperation(List<AtomView> removedAtoms)
    {
        this.removedAtoms = removedAtoms;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        foreach (var item in removedAtoms)
        {
            ColliderManager.Instance.DropColliderForAtom(item);
            structureView.RemoveAtom(item);
        }

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        foreach (var item in removedAtoms)
        {
            structureView.ReviveAtom(item);
            ColliderManager.Instance.CreateColliderForAtom(item);
        }

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class ChangeLatticeOperation : IOperation
{
    private float[] newParams, oldParams;
    private bool keepCartesian;

    public ChangeLatticeOperation(float[] newParams, bool keepCartesian)
    {
        Assert.AreEqual(newParams.Length, 6);
        this.newParams = newParams;
        this.keepCartesian = keepCartesian;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldParams = structureView.Structure.LatticeParams;
        structureView.ChangeLattice(newParams, keepCartesian);
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        structureView.ChangeLattice(oldParams, keepCartesian);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateLattice;
}

public class SuperCellOperation : IOperation
{
    private int repeatA, repeatB, repeatC;

    public SuperCellOperation(int repeatA, int repeatB, int repeatC)
    {
        this.repeatA = repeatA;
        this.repeatB = repeatB;
        this.repeatC = repeatC;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        structureView.ToSuperCell(repeatA, repeatB, repeatC);
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        // TODO: unimplemented
        return false;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateLattice;
}