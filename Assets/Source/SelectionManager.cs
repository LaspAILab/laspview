
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public class SelectionManager
{
    public List<AtomView> Selection { get; private set; }
    
    private Action<List<AtomView>> onSelectionChanged;
    
    public bool IsEmpty => Selection.Count == 0;
    
    public int Count => Selection.Count;

    public SelectionManager(Action<List<AtomView>> onSelectionChanged = null)
    {
        Selection = new();
        this.onSelectionChanged = onSelectionChanged;
        this.onSelectionChanged?.Invoke(Selection);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSelected(AtomView atomView)
    {
        return Selection.Contains(atomView);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Select(AtomView atomView)
    {
        atomView.RenderMat = StructureRenderer.RENDERMAT_SELECTION;
        Selection.Add(atomView);
        
        onSelectionChanged?.Invoke(Selection);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Select(IEnumerable<AtomView> atomViews)
    {
        foreach (var item in atomViews)
        {
            Select(item);
        }
        
        onSelectionChanged?.Invoke(Selection);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceSelection(AtomView atomView)
    {
        ClearSelection();
        Select(atomView);
        
        onSelectionChanged?.Invoke(Selection);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceSelection(IEnumerable<AtomView> atomViews)
    {
        ClearSelection();
        Select(atomViews);
        
        onSelectionChanged?.Invoke(Selection);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Deselect(AtomView atomView)
    {
        atomView.RenderMat = atomView.Symbol;
        bool result = Selection.Remove(atomView);

        if (result)
        {
            onSelectionChanged?.Invoke(Selection);
        }
        
        return result;
    }

    public bool ClearSelection()
    {
        if (Selection.Count == 0)
        {
            return false;
        }

        foreach (var item in Selection)
        {
            item.RenderMat = item.Symbol;
        }

        Selection.Clear();
        onSelectionChanged?.Invoke(Selection);
        return true;
    }

    public void MergeSelection(SelectionManager other)
    {
        foreach (var item in other.Selection)
        {
            if (!IsSelected(item))
            {
                Selection.Add(item);
            }
        }
        
        onSelectionChanged?.Invoke(Selection);
    }

    // Remove inactive atoms from selection
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Validate()
    {
        Selection.RemoveAll(x => !x.InLattice);
    }

    public List<AtomView> SelectionClone => new(Selection);
}