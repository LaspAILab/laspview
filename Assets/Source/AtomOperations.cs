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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            structureView.SetAtomPosition(item, item.SourceAtom.Coordinates + translation, false);
            ColliderManager.Instance.UpdateColliderForAtom(item);
            HBondRenderer.Instance.OnAtomUpdate(structureView, item);
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

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            structureView.SetAtomPosition(item, item.SourceAtom.Coordinates - translation, false);
            ColliderManager.Instance.UpdateColliderForAtom(item);
            HBondRenderer.Instance.OnAtomUpdate(structureView, item);
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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        oldPositions = new List<Vector3>(targetAtoms.Count);
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            oldPositions.Add(targetAtoms[i].SourceAtom.Coordinates);
            structureView.SetAtomPosition(targetAtoms[i], newPositions[i], false);
            ColliderManager.Instance.UpdateColliderForAtom(targetAtoms[i]);
            HBondRenderer.Instance.OnAtomUpdate(structureView, targetAtoms[i]);
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

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            structureView.SetAtomPosition(targetAtoms[i], oldPositions[i], false);
            ColliderManager.Instance.UpdateColliderForAtom(targetAtoms[i]);
            HBondRenderer.Instance.OnAtomUpdate(structureView, targetAtoms[i]);
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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        oldPosition = targetAtom.Position;
        structureView.SetAtomPosition(targetAtom, newPosition, false);
        ColliderManager.Instance.UpdateColliderForAtom(targetAtom);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        HBondRenderer.Instance.OnAtomUpdate(structureView, targetAtom);

        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        structureView.SetAtomPosition(targetAtom, oldPosition, false);
        ColliderManager.Instance.UpdateColliderForAtom(targetAtom);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        HBondRenderer.Instance.OnAtomUpdate(structureView, targetAtom);
        
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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        oldSymbol = targetAtom.Symbol;
        structureView.SetAtomElement(targetAtom, newSymbol);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        HBondRenderer.Instance.OnAtomUpdate(structureView, targetAtom);
        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        structureView.SetAtomElement(targetAtom, oldSymbol);
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        HBondRenderer.Instance.OnAtomUpdate(structureView, targetAtom);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class SetFlagOperation : IOperation
{
    private List<AtomView> targetAtoms;
    private AtomFlags[] oldFlags;
    private AtomFlags newFlag;

    public SetFlagOperation(List<AtomView> targetAtoms, AtomFlags newFlag)
    {
        this.targetAtoms = targetAtoms;
        this.newFlag = newFlag;
    }

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        oldFlags = new AtomFlags[targetAtoms.Count];
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            oldFlags[i] = targetAtoms[i].SourceAtom.Flags;
            targetAtoms[i].SourceAtom.Flags = newFlag;
        }

        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            targetAtoms[i].SourceAtom.Flags = oldFlags[i];
        }

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class SetHiddenOperation : IOperation
{
    private List<AtomView> targetAtoms;
    private bool isHidden;

    public SetHiddenOperation(List<AtomView> targetAtoms, bool isHidden)
    {
        this.targetAtoms = targetAtoms;
        this.isHidden = isHidden;
    }

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            targetAtoms[i].IsHidden = isHidden;
        }

        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        for (int i = 0; i < targetAtoms.Count; i++)
        {
            targetAtoms[i].IsHidden = !isHidden;
        }

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class AddAtomOperation : IOperation
{
    private AtomView addedAtom = null;
    private Atom atomToAdd;

    public AddAtomOperation(Vector3 position, string symbol, AtomFlags flags = AtomFlags.None)
    {
        atomToAdd = new Atom
        {
            Symbol = symbol,
            Coordinates = position,
            Flags = flags
        };
    }

    protected override bool DoImpl(CrystalStructureView structureView)
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
        HBondRenderer.Instance.OnAtomUpdate(structureView, addedAtom);
        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        ColliderManager.Instance.DropColliderForAtom(addedAtom);
        structureView.RemoveAtom(addedAtom);
        HBondRenderer.Instance.OnAtomRemove(addedAtom);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class AddAtomsOperation : IOperation
{
    private List<Atom> atomsToAdd;
    private AtomView[] addedAtoms = null;
    private Vector3 position;
    private string symbol;
    private float charge;

    public AddAtomsOperation(List<Atom> atomsToAdd)
    {
        this.atomsToAdd = atomsToAdd;
    }

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        if (addedAtoms != null)
        {
            foreach (var item in addedAtoms)
            {
                structureView.ReviveAtom(item);
                ColliderManager.Instance.CreateColliderForAtom(item);
                HBondRenderer.Instance.OnAtomUpdate(structureView, item);
            }
        }
        else
        {
            addedAtoms = new AtomView[atomsToAdd.Count];

            for (var i = 0; i < atomsToAdd.Count; i++)
            {
                addedAtoms[i] = structureView.AddAtom(atomsToAdd[i]);
                ColliderManager.Instance.CreateColliderForAtom(addedAtoms[i]);
                HBondRenderer.Instance.OnAtomUpdate(structureView, addedAtoms[i]);
            }
        }

        App.Instance.viewport.selectionManager.ReplaceSelection(addedAtoms.Select(x => new SelectionItem
        {
            Atom = x,
            LatticeOffset = Vector3.zero,
            CellIndex = (0, 0, 0)
        }));

        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        foreach (var item in addedAtoms)
        {
            ColliderManager.Instance.DropColliderForAtom(item);
            structureView.RemoveAtom(item);
            HBondRenderer.Instance.OnAtomRemove(item);
        }

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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        foreach (var item in removedAtoms)
        {
            ColliderManager.Instance.DropColliderForAtom(item);
            structureView.RemoveAtom(item);
            HBondRenderer.Instance.OnAtomRemove(item);
        }

        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        foreach (var item in removedAtoms)
        {
            structureView.ReviveAtom(item);
            ColliderManager.Instance.CreateColliderForAtom(item);
            HBondRenderer.Instance.OnAtomUpdate(structureView, item);
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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        oldParams = structureView.Structure.LatticeParams;
        structureView.ChangeLattice(newParams, keepCartesian);
        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
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

    protected override bool DoImpl(CrystalStructureView structureView)
    {
        structureView.ToSuperCell(repeatA, repeatB, repeatC);
        ColliderManager.Instance.CreateCollidersForStructure(structureView);
        return true;
    }

    protected override bool UndoImpl(CrystalStructureView structureView)
    {
        // TODO: unimplemented
        return false;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateLattice;
}