
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public interface IViewportAPI
{
    void MarkRendererDirty();
    CrystalStructureView GetCrystalStructureView();
}

public enum PreviewedTransformation
{
    Any,
    Drag,
    Rotate,
}

// Tracks a pending translation with previewing in viewport
public class PreviewTracker
{
    private AtomView[] trackedAtoms;
    private IViewportAPI viewport;
    private bool submitted = false;

    // Whether to update bonds in realtime when transforming atoms
    public bool UpdateBondingRealtime
    {
        get;
        set;
    } = true;

    public PreviewedTransformation TransformationType
    {
        get;
        set;
    } = PreviewedTransformation.Any;

    // A buffer to fill positions, in order to eliminate allocation
    public List<Vector3> PositionBuffer
    {
        get;
    }

    public AtomView[] TrackedAtoms => trackedAtoms;

    public PreviewTracker()
    {
        trackedAtoms = Array.Empty<AtomView>();
    }

    public PreviewTracker(IViewportAPI activeViewport, IList<AtomView> trackedAtoms)
    {
        this.trackedAtoms = trackedAtoms.ToArray();
        PositionBuffer = Enumerable.Repeat(Vector3.zero, trackedAtoms.Count).ToList();
        viewport = activeViewport;
    }

    public void UpdatePositions(bool usePositionBuffer, IList<Vector3> newPositions = null)
    {
        if (submitted)
        {
            Debug.LogWarning("Attempting to update after submission, ignoring. ");
            return;
        }

        if (usePositionBuffer)
        {
            newPositions = PositionBuffer;
        }
        else
        {
            Assert.IsNotNull(newPositions, "New positions cannot be null unless the position buffer is used");
        }

        Assert.AreEqual(trackedAtoms.Length, newPositions.Count);

        var crystalStructureView = viewport.GetCrystalStructureView();
        for (int i = 0; i < trackedAtoms.Length; i++)
        {
            crystalStructureView.SetAtomPosition(trackedAtoms[i], newPositions[i], true);
        }

        UpdateBonding();
        viewport.MarkRendererDirty();
    }

    public void UpdatePositions(Func<AtomView, Vector3> positionGenerator)
    {
        if (submitted)
        {
            Debug.LogWarning("Attempting to update after submission, ignoring. ");
            return;
        }

        var crystalStructureView = viewport.GetCrystalStructureView();
        foreach (var item in trackedAtoms)
        {
            crystalStructureView.SetAtomPosition(item, positionGenerator(item), true);
        }

        UpdateBonding();
        viewport.MarkRendererDirty();
    }

    public void Submit(List<Vector3> newPositions = null)
    {
        newPositions ??= trackedAtoms.Select(x => x.Position).ToList();

        IOperation op = new RepositionAtomsOperation(trackedAtoms.ToList(), newPositions);
        App.Instance.operationManager.SubmitOperation(op);
        submitted = true;
    }

    private void UpdateBonding()
    {
        if (!UpdateBondingRealtime)
        {
            return;
        }

        var crystalStructureView = viewport.GetCrystalStructureView();

#if !UNITY_WEBGL
        crystalStructureView.BondMutex.WaitOne();
#endif
        foreach (var item in trackedAtoms)
        {
            crystalStructureView.UpdateAtomBondsNoLock(item, App.Instance.BondTolerance, true);
        }
#if !UNITY_WEBGL
        crystalStructureView.BondMutex.ReleaseMutex();
#endif
    }

    public void Reset()
    {
        if (submitted)
        {
            Debug.LogWarning("Attempting to update but changes are already submitted, ignoring. ");
            return;
        }

        var crystalStructureView = viewport.GetCrystalStructureView();

        // Restore atom positions to their original ones
        for (int i = 0; i < trackedAtoms.Length; i++)
        {
            crystalStructureView.SetAtomPosition(trackedAtoms[i], CrystalStructureView.SwapYZ(trackedAtoms[i].SourceAtom.Coordinates), true);
        }

        UpdateBonding();
    }

    ~PreviewTracker()
    {
        if (!submitted)
        {
            // Reset upon dispose
            Reset();
        }
    }
}
