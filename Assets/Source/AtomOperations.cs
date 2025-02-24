using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class TranslateOperation : IOperation
{
    private List<AtomView> targetAtoms;
    private Vector3 translation;

    public TranslateOperation(List<AtomView> targetAtoms, Vector3 translation)
    {
        this.targetAtoms = targetAtoms;
        this.translation = translation;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            item.SetPosition(item.SourceAtom.Coordinates + translation, structureView.Structure, false);
        }

        structureView.BondMutex.WaitOne();
        foreach (var item in targetAtoms)
        {
            structureView.UpdateAtomBonds(item, App.Instance.BondTolerance, true);
        }

        structureView.BondMutex.ReleaseMutex();

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            item.SetPosition(item.SourceAtom.Coordinates - translation, structureView.Structure, false);
        }

        structureView.BondMutex.WaitOne();
        foreach (var item in targetAtoms)
        {
            structureView.UpdateAtomBonds(item, App.Instance.BondTolerance, true);
        }

        structureView.BondMutex.ReleaseMutex();

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
        targetAtom.SetPosition(newPosition, structureView.Structure, false);

        structureView.BondMutex.WaitOne();
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        structureView.BondMutex.ReleaseMutex();

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        targetAtom.SetPosition(oldPosition, structureView.Structure, false);

        structureView.BondMutex.WaitOne();
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        structureView.BondMutex.ReleaseMutex();

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
        targetAtom.SetElement(newSymbol);

        structureView.BondMutex.WaitOne();
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        structureView.BondMutex.ReleaseMutex();
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        targetAtom.SetElement(oldSymbol);

        structureView.BondMutex.WaitOne();
        structureView.UpdateAtomBonds(targetAtom, App.Instance.BondTolerance, true);
        structureView.BondMutex.ReleaseMutex();
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

public class AddAtomOperation : IOperation
{
    private AtomView addedAtom;
    private Vector3 position;
    private string symbol;
    private float charge;

    public AddAtomOperation(Vector3 position, string symbol, float charge = 0)
    {
        this.position = position;
        this.symbol = symbol;
        this.charge = charge;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        addedAtom = structureView.AddAtom(symbol, position, charge);
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        structureView.DeleteAtom(addedAtom);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class RemoveAtomsOperation : IOperation
{
    private List<AtomView> removedAtoms;
    private List<Atom> removedAtomsCopy;
    private Vector3 position;
    private string symbol;
    private float charge;

    public RemoveAtomsOperation(List<AtomView> removedAtoms)
    {
        this.removedAtoms = removedAtoms;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        removedAtomsCopy = removedAtoms.Select(x => x.SourceAtom).ToList();

        foreach (var item in removedAtoms)
        {
            structureView.DeleteAtom(item);
        }

        removedAtoms.Clear();

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        foreach (var item in removedAtomsCopy)
        {
            removedAtoms.Add(structureView.AddAtom(item.Symbol, item.Coordinates, item.Charge));
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