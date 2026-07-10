using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public struct SelectionItem : IEquatable<SelectionItem>
{
    public AtomView Atom;

    // Order of selection
    public int SelectIndex;

    // An atom may have multiple copies in the viewport,
    // so we have to clarify which copy did we select
    public Vector3 LatticeOffset;

    public (int, int, int) CellIndex;

    public Vector3 Position => Atom.Position + LatticeOffset;

    public bool Equals(SelectionItem other)
    {
        return Atom.Equals(other.Atom) && CellIndex == other.CellIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is SelectionItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Atom.GetHashCode();
    }

    public static SelectionItem CreateFromCollider(ColliderScript colliderScript)
    {
        return new SelectionItem
        {
            Atom = colliderScript.Atom,
            LatticeOffset = colliderScript.latticeOffset,
            CellIndex = colliderScript.CellIndex
        };
    }
}

class SelectionItemComparator : IEqualityComparer<SelectionItem>
{
    public bool Equals(SelectionItem x, SelectionItem y)
    {
        return x.Atom.Equals(y.Atom);
    }

    public int GetHashCode(SelectionItem obj)
    {
        return obj.Atom.GetHashCode();
    }
}

public class SelectionManager
{
    // This member may contain duplicate atoms, use with caution!!!
    // In most cases `SelectedAtoms` should be used instead
    public HashSet<SelectionItem> Selection { get; private set; }

    private Action<List<SelectionItem>> onSelectionChanged;

    private int selectionIndex;

    public bool IsEmpty => Selection.Count == 0;

    public int Count => Selection.Count;

    public SelectionManager(Action<List<SelectionItem>> onSelectionChanged = null)
    {
        Selection = new();
        selectionIndex = 0;
        this.onSelectionChanged = onSelectionChanged;
        this.onSelectionChanged?.Invoke(Selection.ToList());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSelected(AtomView atomView, (int, int, int) cellIndex)
    {
        return Selection.Contains(new SelectionItem { Atom = atomView, CellIndex = cellIndex });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Select(SelectionItem item)
    {
        item.Atom.SelectedCellIndex = item.CellIndex;
        item.SelectIndex = selectionIndex++;
        bool result = Selection.Add(item);

        if (result)
        {
            onSelectionChanged?.Invoke(Selection.ToList());
        }
        else
        {
            Debug.LogWarning("Atom attempting to select is already selected");
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Select(IEnumerable<SelectionItem> atomViews)
    {
        foreach (var item in atomViews)
        {
            Select(item);
        }

        onSelectionChanged?.Invoke(Selection.ToList());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceSelection(SelectionItem item)
    {
        ClearSelection();
        Select(item);

        onSelectionChanged?.Invoke(Selection.ToList());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceSelection(IEnumerable<SelectionItem> item)
    {
        ClearSelection();
        Select(item);

        onSelectionChanged?.Invoke(Selection.ToList());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Deselect(AtomView atomView, (int, int, int) cellIndex)
    {
        atomView.SelectedCellIndex = null;
        bool result = Selection.Remove(new SelectionItem { Atom = atomView, CellIndex = cellIndex });

        if (result)
        {
            onSelectionChanged?.Invoke(Selection.ToList());
        }
        else
        {
            Debug.LogWarning("Atom to be deselected isn't found");
        }

        return result;
    }

    public bool ClearSelection()
    {
        selectionIndex = 0;
        if (Selection.Count == 0)
        {
            return false;
        }

        foreach (var item in Selection)
        {
            item.Atom.SelectedCellIndex = null;
        }

        Selection.Clear();
        onSelectionChanged?.Invoke(Selection.ToList());
        return true;
    }

    public void MergeSelection(SelectionManager other)
    {
        foreach (var item in other.Selection)
        {
            Selection.Add(item);
        }

        onSelectionChanged?.Invoke(Selection.ToList());
    }

    // Migrate the selection from another selection manager to this one
    public void MigrateSelection(CrystalStructureView currentStructure, SelectionManager other)
    {
        foreach (var item in other.Selection)
        {
            var match = currentStructure.Atoms.Find(x => x.SourceAtom.Index == item.Atom.SourceAtom.Index);
            if (match != null)
            {
                Select(new SelectionItem
                {
                    Atom = match,
                    LatticeOffset = CrystalStructureView.SwapYZ(
                        currentStructure.Structure.FractionalToCartesian(new Vector3(item.CellIndex.Item1,
                            item.CellIndex.Item2, item.CellIndex.Item3))),
                    CellIndex = item.CellIndex
                });
            }
        }
    }

    // Remove inactive atoms from selection
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveInvalid()
    {
        // Copy the iterator to list in advance to avoid iterator being invalidated after removing
        foreach (var item in Selection.Where(item => !item.Atom.InLattice || item.Atom.IsHidden).ToList())
        {
            item.Atom.SelectedCellIndex = null;
            Selection.Remove(item);
        }

        onSelectionChanged?.Invoke(Selection.ToList());
    }

    public IEnumerable<SelectionItem> DistinctSelection => Selection.Distinct(new SelectionItemComparator());

    public List<AtomView> SelectedAtoms => Selection.Select(x => x.Atom).Distinct().ToList();
}