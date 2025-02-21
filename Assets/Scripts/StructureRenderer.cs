using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

// Batch of atoms and bonds that belong to the same element, so that they could be rendered in batch
class DrawBatch
{
    public List<Matrix4x4> AtomTransforms = new();
    public List<Matrix4x4> BondTransforms = new();
}

public class StructureRenderer : MonoBehaviour
{
    public const string RENDERMAT_SELECTION = "select";

    static Dictionary<string, Material> ElementMaterials = new();
    Dictionary<string, DrawBatch> ElementDrawBatches = new();

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public ArcStructureView CurrentStructureView
    {
        get;
        private set;
    } = null;

    private float stickRadius = 0.35f, radiusScale = 1f;
    private Matrix4x4[,,] latticeTransforms = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetBondStyle(float stickRadius)
    {
        if (this.stickRadius == stickRadius)
        {
            return;
        }
        this.stickRadius = stickRadius;

        foreach (var item in CurrentStructureView.Bonds)
        {
            item.CreateTransformCache(stickRadius);
        }

        RecollectDrawBatches();
    }

    public void SetAtomStyle(float radiusScale)
    {
        if (this.radiusScale == radiusScale)
        {
            return;
        }
        this.radiusScale = radiusScale;

        foreach (var item in CurrentStructureView.Atoms)
        {
            item.CreateTransformCache(radiusScale);
        }

        RecollectDrawBatches();
    }

    public bool SetLatticeRepeat(int repeatA, int repeatB, int repeatC)
    {
        Debug.Log($"Setting lattice repeat {repeatA}, {repeatB}, {repeatC}");

        latticeTransforms = new Matrix4x4[repeatA, repeatB, repeatC];
        for (int i = 0; i < repeatA; i++)
        {
            for (int j = 0; j < repeatB; j++)
            {
                for (int k = 0; k < repeatC; k++)
                {
                    latticeTransforms[i, j, k] = CurrentStructureView.GetLatticeTransform(i, j, k);
                }
            }
        }

        RecollectDrawBatches();
        return true;
    }

    // Duplicate render items, to render multiple lattices
    private void ReplicateBuffer(ref List<Matrix4x4> buffer)
    {
        if (latticeTransforms == null || latticeTransforms.Length == 1)
        {
            return;
        }

        int length = buffer.Count;
        int repeatA = latticeTransforms.GetLength(0), repeatB = latticeTransforms.GetLength(1), repeatC = latticeTransforms.GetLength(2);
        buffer.Capacity = length * repeatA * repeatB * repeatC;

        for (int i = 0; i < repeatA; i++)
        {
            for (int j = 0; j < repeatB; j++)
            {
                for (int k = 0; k < repeatC; k++)
                {
                    if (i == 0 && j == 0 && k == 0)
                    {
                        continue;
                    }

                    for (int count = 0; count < length; count++)
                    {
                        buffer.Add(latticeTransforms[i, j, k] * buffer[count]);
                    }
                }
            }
        }

        Assert.AreEqual(buffer.Count, length * repeatA * repeatB * repeatC);
    }

    private void SubmitAtomBuffer(Dictionary<string, DrawBatch> drawBatches, string renderMat, List<Matrix4x4> buffer)
    {
        if (!drawBatches.ContainsKey(renderMat))
        {
            drawBatches[renderMat] = new();
        }
        ReplicateBuffer(ref buffer);
        drawBatches[renderMat].AtomTransforms = buffer;

        if (!ElementMaterials.ContainsKey(renderMat))
        {
            Material material = new(RenderMaterial);
            if(renderMat == RENDERMAT_SELECTION)
            {
                material.SetColor("_BaseColor", Color.yellow);
            }
            else
            {
                material.SetColor("_BaseColor", ConFun.Elements[renderMat].Color);
            }
                
            ElementMaterials[renderMat] = material;
        }
    }

    private void SubmitBondBuffer(Dictionary<string, DrawBatch> drawBatches, string renderMat, List<Matrix4x4> buffer)
    {
        ReplicateBuffer(ref buffer);
        drawBatches[renderMat].BondTransforms = buffer;
    }

    internal void SetStructureView(ArcStructureView structureView)
    {
        CurrentStructureView = structureView;
        
        // Recollect the draw batches
        RecollectDrawBatches(true);
    }

    internal void RecollectDrawBatches(bool doSort=false)
    {
        Profiler.BeginSample("Collect draw batch");
        if (doSort)
        {
            CurrentStructureView.SortLists();
        }

        Dictionary<string, DrawBatch> drawBatches = new();
        // GC.Collect();

        string currentRenderMat = "";
        List<Matrix4x4> buffer = new();
        foreach (var item in CurrentStructureView.Atoms)
        {
            if (item.RenderMat != currentRenderMat)
            {
                if (buffer.Count > 0)
                {
                    SubmitAtomBuffer(drawBatches, currentRenderMat, buffer);
                    buffer = new();
                }

                currentRenderMat = item.RenderMat;
            }

            buffer.Add(item.Transform);
        }

        if (buffer.Count > 0)
        {
            SubmitAtomBuffer(drawBatches, currentRenderMat, buffer);
        }

        buffer = new();
        currentRenderMat = "";

        CurrentStructureView.BondMutex.WaitOne();
        foreach (var item in CurrentStructureView.Bonds)
        {
            if (item.FromAtom.RenderMat != currentRenderMat)
            {
                if (buffer.Count > 0)
                {
                    SubmitBondBuffer(drawBatches, currentRenderMat, buffer);
                    buffer = new();
                }

                currentRenderMat = item.FromAtom.RenderMat;
            }

            buffer.Add(item.Transform);
        }
        CurrentStructureView.BondMutex.ReleaseMutex();

        if (buffer.Count > 0)
        {
            SubmitBondBuffer(drawBatches, currentRenderMat, buffer);
        }

        ElementDrawBatches = drawBatches;
        Profiler.EndSample();
    }

    internal void RenderSingleAtom(string renderMat, Vector3 pos, float radius)
    {
        RenderParams renderParams = new(ElementMaterials[renderMat]);
        var transformMat = Matrix4x4.TRS(pos, Quaternion.identity, radius * radiusScale * Vector3.one);
        Graphics.RenderMesh(renderParams, AtomMesh, 0, transformMat);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in ElementDrawBatches)
        {
            RenderParams renderParams = new(ElementMaterials[item.Key]);
            Graphics.RenderMeshInstanced(renderParams, AtomMesh, 0, item.Value.AtomTransforms);
            if (item.Value.BondTransforms.Count > 0)
            {
                Graphics.RenderMeshInstanced(renderParams, BondMesh, 0, item.Value.BondTransforms);
            }
        }
    }
}
