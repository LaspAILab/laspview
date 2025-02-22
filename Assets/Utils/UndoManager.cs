using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

public enum UpdateFlags
{
    None,
    UpdateAtom,
    UpdateLattice
}

public abstract class IOperation
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

        if (UndoImpl(structureView))
        {
            isDone = false;
        }
    }

    protected abstract bool DoImpl(ArcStructureView structureView);

    protected abstract bool UndoImpl(ArcStructureView structureView);

    public abstract UpdateFlags Flags { get; }
}

public class UndoManager
{
    private List<IOperation> operationStack;
    private int stackTop = -1;
    private ArcStructureView structureView;

    public UndoManager(ArcStructureView structureView)
    {
        operationStack = new();
        this.structureView = structureView;
    }

    public void SubmitOperation(IOperation operation)
    {
        if (stackTop != operationStack.Count - 1)
        {
            // Abandon remaining operations
            operationStack.RemoveRange(stackTop, operationStack.Count - stackTop - 1);
            Assert.AreEqual(stackTop, operationStack.Count - 1);
        }

        operation.Do(structureView);
        operationStack.Add(operation);
        stackTop++;
    }

    public IOperation Undo()
    {
        if (stackTop < 0)
        {
            return null;
        }

        IOperation operation = operationStack[stackTop--];
        operation.Undo(structureView);
        return operation;
    }

    public IOperation Redo()
    {
        if (stackTop >= operationStack.Count - 1)
        {
            return null;
        }

        IOperation operation = operationStack[++stackTop];
        operation.Do(structureView);
        return operation;
    }
}