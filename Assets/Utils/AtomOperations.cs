
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class IAtomOperation
{
    private bool isDone = false;

    public void Do(ArcStructureView structureView)
    {
        if (isDone)
        {
            throw new InvalidOperationException("The operation has already been completed.");
        }

        if (DoImpl(structureView))
        {
            isDone = true;
        }
    }
    
    public void Undo(ArcStructureView structureView)
    {
        if (!isDone)
        {
            throw new InvalidOperationException("Cannot undo undone operations.");
        }
        
        if (!UndoImpl(structureView))
        {
            isDone = false;
        }
    }
    
    protected abstract bool DoImpl(ArcStructureView structureView);

    protected abstract bool UndoImpl(ArcStructureView structureView);
    
    public abstract bool RequireRerender { get; }
    
    public abstract bool RequireResort { get; }
}

public class TranslateAtomsOperation : IAtomOperation
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
            item.SetPosition(item.Position + translation, structureView.Structure);
        }

        return true;
    }

    protected override bool UndoImpl(ArcStructureView structureView)
    {
        foreach (var item in targetAtoms)
        {
            item.SetPosition(item.Position - translation, structureView.Structure);
        }

        return true;
    }

    public override bool RequireRerender => true;
    public override bool RequireResort => false;
}

public class ChangeAtomSymbolOperation : IAtomOperation
{
    private AtomView targetAtom;
    private string newSymbol, oldSymbol;

    public ChangeAtomSymbolOperation(AtomView targetAtom, string newSymbol)
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

    public override bool RequireRerender => true;
    public override bool RequireResort => true;
}