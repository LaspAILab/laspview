using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

// Batch of atoms and bonds that belong to the same element, so that they could be rendered in batch
struct DrawBatch
{
    public List<Matrix4x4> AtomTransforms;
    public List<Matrix4x4> BondTransforms;
}

struct RenderArgs
{
    public RenderParams Params;
    public Material Mat;
}

public class StructureRenderer : MonoBehaviour
{
    public const string RENDERMAT_SELECTION = "select";

    static Dictionary<string, RenderArgs> ElementMaterials = new();
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Dictionary<string, DrawBatch> ElementDrawBatches = new();

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public ArcStructureView CurrentStructureView { get; private set; } = null;

    private float stickRadius = 0.35f, radiusScale = 1f;
    private Matrix4x4[,,] latticeTransforms = null;
    private bool renderAtoms = true, renderBonds = true;

    public void SetDisplayType(bool showBalls = true, bool showSticks = true)
    {
        renderAtoms = showBalls;
        renderBonds = showSticks;
    }

    public void SetBondStyle(float stickRadius)
    {
        if (Mathf.Approximately(this.stickRadius, stickRadius))
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
        if (Mathf.Approximately(this.radiusScale, radiusScale))
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

    public void SetLatticeTransforms(Matrix4x4[,,] newLatticeTransforms)
    {
        latticeTransforms = newLatticeTransforms;
        RecollectDrawBatches();
    }

    // Duplicate render items, to render multiple lattices
    private void ReplicateBuffer(ref List<Matrix4x4> buffer)
    {
        if (latticeTransforms == null)
        {
            return;
        }

        int length = buffer.Count;
        int repeatA = latticeTransforms.GetLength(0),
            repeatB = latticeTransforms.GetLength(1),
            repeatC = latticeTransforms.GetLength(2);
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
        drawBatches[renderMat] = new DrawBatch
        {
            AtomTransforms = buffer,
            BondTransforms = null
        };

        // Lazy creation for materials
        if (!ElementMaterials.ContainsKey(renderMat))
        {
            Material material = new(RenderMaterial);
            if (renderMat == RENDERMAT_SELECTION)
            {
                material.SetColor(BaseColor, Color.yellow);
            }
            else
            {
                material.SetColor(BaseColor, App.Elements[renderMat].Color);
            }

            ElementMaterials[renderMat] = new RenderArgs
            {
                Mat = material,
                Params = new RenderParams(material)
                {
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false
                }
            };
        }
    }

    private void SubmitBondBuffer(Dictionary<string, DrawBatch> drawBatches, string renderMat, List<Matrix4x4> buffer)
    {
        ReplicateBuffer(ref buffer);

        var atomsBuffer = drawBatches[renderMat].AtomTransforms;
        drawBatches[renderMat] = new DrawBatch
        {
            AtomTransforms = atomsBuffer,
            BondTransforms = buffer
        };
    }

    internal void SetStructureView(ArcStructureView structureView, Matrix4x4[,,] newLatticeTransforms)
    {
        CurrentStructureView = structureView;

        // Since the new lattice may have different size as the old one, we have to recalculate lattice translations
        SetLatticeTransforms(newLatticeTransforms);

        // Recollect the draw batches
        RecollectDrawBatches(true);
    }

    internal void RecollectDrawBatches(bool doSort = false)
    {
        Profiler.BeginSample("Collect draw batch");
        if (doSort)
        {
            CurrentStructureView.SortAtoms();
            CurrentStructureView.SortBonds();
        }

        // We create a new dictionary here to potentially support running this function on another thread
        // Since the old dict isn't modified but reassigned, this function is thread-safe
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

#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.WaitOne();
#endif
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
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.ReleaseMutex();
#endif

        if (buffer.Count > 0)
        {
            SubmitBondBuffer(drawBatches, currentRenderMat, buffer);
        }

        ElementDrawBatches = drawBatches;
        Profiler.EndSample();
    }

    internal void RenderSingleAtom(string renderMat, Vector3 pos, float radius)
    {
        var renderParams = ElementMaterials[renderMat].Params;
        var transformMat = Matrix4x4.TRS(pos, Quaternion.identity, radius * radiusScale * Vector3.one);
        Graphics.RenderMesh(renderParams, AtomMesh, 0, transformMat);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in ElementDrawBatches)
        {
            var renderParams = ElementMaterials[item.Key].Params;
            if (renderAtoms)
            {
                Graphics.RenderMeshInstanced(renderParams, AtomMesh, 0, item.Value.AtomTransforms);
            }

            if (renderBonds && item.Value.BondTransforms != null && item.Value.BondTransforms.Count > 0)
            {
                Graphics.RenderMeshInstanced(renderParams, BondMesh, 0, item.Value.BondTransforms);
            }
        }
    }
}