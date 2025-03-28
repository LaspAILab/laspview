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
    public Vector3 Position => Atom.Position + LatticeOffset;
    
    public bool Equals(SelectionItem other)
    {
        return Atom.Equals(other.Atom);
    }
    
    public override bool Equals(object obj)
    {
        return obj is SelectionItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Atom.GetHashCode();
    }
}

public class SelectionManager
{
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
    public bool IsSelected(AtomView atomView)
    {
        return Selection.Contains(new SelectionItem { Atom = atomView });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Select(SelectionItem item)
    {
        item.Atom.RenderMat = StructureRenderer.RENDERMAT_SELECTION;
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
    public bool Deselect(AtomView atomView)
    {
        atomView.RenderMat = atomView.Symbol;
        bool result = Selection.Remove(new SelectionItem { Atom = atomView });

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
            item.Atom.RenderMat = item.Atom.Symbol;
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

    // Remove inactive atoms from selection
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveInvalid()
    {
        Selection.RemoveWhere(x => !x.Atom.InLattice);
    }

    public List<AtomView> SelectedAtoms => Selection.Select(x => x.Atom).ToList();
}