
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class SelectionManager
{
    public List<AtomView> Selection { get; } = new();
    
    public bool IsEmpty => Selection.Count == 0;
    
    public int Count => Selection.Count;

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
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Select(IEnumerable<AtomView> atomViews)
    {
        foreach (var item in atomViews)
        {
            Select(item);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceSelection(AtomView atomView)
    {
        ClearSelection();
        Select(atomView);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceSelection(IEnumerable<AtomView> atomViews)
    {
        ClearSelection();
        Select(atomViews);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Deselect(AtomView atomView)
    {
        atomView.RenderMat = atomView.Symbol;
        return Selection.Remove(atomView);
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
        return true;
    }
}