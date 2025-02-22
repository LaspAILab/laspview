using System;
using System.Collections.Generic;
using UnityEngine;


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
            structureView.UpdateAtomBonds(item, App.Instance.bondTolerance, true);
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
            structureView.UpdateAtomBonds(item, App.Instance.bondTolerance, true);
        }

        structureView.BondMutex.ReleaseMutex();

        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}

public class ChangeSymbolOperation : IOperation
{
    private AtomView targetAtom;
    private string newSymbol, oldSymbol;

    public ChangeSymbolOperation(AtomView targetAtom, string newSymbol)
    {
        this.targetAtom = targetAtom;
        this.newSymbol = newSymbol;
    }

    protected override bool DoImpl(ArcStructureView structureView)
    {
        oldSymbol = targetAtom.Symbol;
        targetAtom.SetElement(newSymbol);
        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        targetAtom.SetElement(oldSymbol);
        return true;
    }

    public override UpdateFlags Flags => UpdateFlags.UpdateAtom;
}