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

    public void Do(CrystalStructureView structureView)
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

    public void Undo(CrystalStructureView structureView)
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

    protected abstract bool DoImpl(CrystalStructureView structureView);

    protected abstract bool UndoImpl(CrystalStructureView structureView);

    public abstract UpdateFlags Flags { get; }
}

public class OperationManager
{
    public delegate void OnOperationPerformedEvent(IOperation operation);
    public event OnOperationPerformedEvent OnOperationPerformed;

    private List<IOperation> operationStack;
    private int stackTop = -1;
    private CrystalStructureView structureView;

    public OperationManager(CrystalStructureView structureView)
    {
        operationStack = new();
        this.structureView = structureView;
    }

    public void SubmitOperation(IOperation operation)
    {
        if (stackTop != operationStack.Count - 1)
        {
            // Abandon remaining operations
            operationStack.RemoveRange(stackTop + 1, operationStack.Count - stackTop - 1);
            Assert.AreEqual(stackTop, operationStack.Count - 1);
        }

        operation.Do(structureView);
        OnOperationPerformed?.Invoke(operation);
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
        OnOperationPerformed?.Invoke(operation);
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
        OnOperationPerformed?.Invoke(operation);
        return operation;
    }
}