using System;
using System.Collections.Generic;
using UnityEngine;

// Batch of atoms and bonds that belong to the same element, so that they could be rendered in batch
class DrawBatch
{
    public List<Matrix4x4> AtomTransforms = new();
    public List<Matrix4x4> BondTransforms = new();
}

public class StructureRenderer : MonoBehaviour
{
    static Dictionary<string, Material> ElementMaterials = new();
    Dictionary<string, DrawBatch> ElementDrawBatches = new();

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public ArcStructureView CurrentStructureView;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void SubmitAtomBuffer(string atomSymbol, List<Matrix4x4> buffer)
    {
        if (!ElementDrawBatches.ContainsKey(atomSymbol))
        {
            ElementDrawBatches[atomSymbol] = new();
        }
        ElementDrawBatches[atomSymbol].AtomTransforms = buffer;

        if (!ElementMaterials.ContainsKey(atomSymbol))
        {
            Material material = new(RenderMaterial);
            material.SetColor("_BaseColor", ConFun.Elements[atomSymbol].Color);
            ElementMaterials[atomSymbol] = material;
        }
    }

    private void SubmitBondBuffer(string atomSymbol, List<Matrix4x4> buffer)
    {
        ElementDrawBatches[atomSymbol].BondTransforms = buffer;
    }

    public void LoadStructure(ArcStructureView structureView)
    {
        CurrentStructureView = structureView;

        // Recollect the draw batches
        CollectDrawBatches();
    }

    void CollectDrawBatches()
    {
        ElementDrawBatches = new();
        GC.Collect();

        string currentRenderMat = "";
        List<Matrix4x4> buffer = new();
        foreach (var item in CurrentStructureView.Atoms)
        {
            if (item.Symbol != currentRenderMat)
            {
                if (buffer.Count > 0)
                {
                    SubmitAtomBuffer(currentRenderMat, buffer);
                    buffer = new();
                }

                currentRenderMat = item.RenderMat;
            }

            buffer.Add(item.Transform);
        }

        if (buffer.Count > 0)
        {
            SubmitAtomBuffer(currentRenderMat, buffer);
        }

        buffer = new();
        currentRenderMat = "";

        foreach (var item in CurrentStructureView.Bonds)
        {
            if (item.FromAtom.RenderMat != currentRenderMat)
            {
                if (buffer.Count > 0)
                {
                    SubmitBondBuffer(currentRenderMat, buffer);
                    buffer = new();
                }

                currentRenderMat = item.FromAtom.RenderMat;
            }

            buffer.Add(item.Transform);
        }

        if (buffer.Count > 0)
        {
            SubmitBondBuffer(currentRenderMat, buffer);
        }
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
