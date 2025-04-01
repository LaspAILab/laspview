using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    private List<Matrix4x4> atomTransforms = new();
    private List<Vector4> atomColors = new();
    private List<Matrix4x4> bondTransforms = new();
    private List<Vector4> bondColors = new();

    private RenderParams renderParams;
    private bool renderAtoms = true, renderBonds = true;

    public void SetDisplayType(bool showBalls = true, bool showSticks = true)
    {
        renderAtoms = showBalls;
        renderBonds = showSticks;
    }

    void Start()
    {
        renderParams = new RenderParams(RenderMaterial)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Color GetAtomColor(AtomView atom)
    {
        return atom.IsSelected ? Color.yellow : atom.DisplayColor;
    }

    public void SetLatticeTransforms(Matrix4x4[,,] newLatticeTransforms)
    {
        latticeTransforms = newLatticeTransforms;
        RecollectDrawBatches();
    }

    // Duplicate render items, to render multiple lattices
    private void ReplicateBuffers(int repeatA, int repeatB, int repeatC)
    {
        if (latticeTransforms == null)
        {
            return;
        }

        int atomCount = atomTransforms.Count;
        int bondCount = bondTransforms.Count;
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

                    for (int count = 0; count < atomCount; count++)
                    {
                        atomTransforms.Add(latticeTransforms[i, j, k] * atomTransforms[count]);
                        atomColors.Add(atomColors[count]);
                    }

                    for (int count = 0; count < bondCount; count++)
                    {
                        bondTransforms.Add(latticeTransforms[i, j, k] * bondTransforms[count]);
                        bondColors.Add(bondColors[count]);
                    }
                }
            }
        }

        Assert.AreEqual(atomTransforms.Count, atomCount * repeatA * repeatB * repeatC);
        Assert.AreEqual(bondTransforms.Count, bondCount * repeatA * repeatB * repeatC);
    }

    internal void SetStructureView(ArcStructureView structureView, Matrix4x4[,,] newLatticeTransforms)
    {
        CurrentStructureView = structureView;

        // Since the new lattice may have different size as the old one, we have to recalculate lattice translations
        SetLatticeTransforms(newLatticeTransforms);

        // Recollect the draw batches
        RecollectDrawBatches();
    }

    internal void RecollectDrawBatches()
    {
        Profiler.BeginSample("Collect draw batch");

        int repeatA = 1, repeatB = 1, repeatC = 1;
        if (latticeTransforms != null)
        {
            repeatA = latticeTransforms.GetLength(0);
            repeatB = latticeTransforms.GetLength(1);
            repeatC = latticeTransforms.GetLength(2);
        }

        // GC.Collect();
        atomTransforms.Clear();
        atomColors.Clear();
        atomTransforms.Capacity = CurrentStructureView.Atoms.Count * repeatA * repeatB * repeatC;
        atomColors.Capacity = CurrentStructureView.Atoms.Count * repeatA * repeatB * repeatC;
        foreach (var item in CurrentStructureView.Atoms)
        {
            atomTransforms.Add(item.Transform);
            atomColors.Add(GetAtomColor(item));
        }

        bondTransforms.Clear();
        bondColors.Clear();
        bondTransforms.Capacity = CurrentStructureView.Bonds.Count * repeatA * repeatB * repeatC;
        bondColors.Capacity = CurrentStructureView.Bonds.Count * repeatA * repeatB * repeatC;
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.WaitOne();
#endif
        foreach (var item in CurrentStructureView.Bonds)
        {
            bondTransforms.Add(item.Transform);
            bondColors.Add(GetAtomColor(item.FromAtom));
        }
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.ReleaseMutex();
#endif

        ReplicateBuffers(repeatA, repeatB, repeatC);

        Profiler.EndSample();
    }

    internal void RenderSingleAtom(AtomView atom, Vector3 pos, float radius)
    {
        var props = new MaterialPropertyBlock();
        props.SetColor(BaseColor, GetAtomColor(atom));
        renderParams.matProps = props;
        var transformMat = Matrix4x4.TRS(pos, Quaternion.identity, radius * radiusScale * Vector3.one);
        Graphics.RenderMesh(renderParams, AtomMesh, 0, transformMat);
    }

    // Update is called once per frame
    void Update()
    {
        if (atomColors.Count == 0)
        {
            return;
        }

        var propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetVectorArray(BaseColor, atomColors);
        renderParams.matProps = propertyBlock;
        if (renderAtoms)
        {
            Graphics.RenderMeshInstanced(renderParams, AtomMesh, 0, atomTransforms);
        }

        propertyBlock = new();
        propertyBlock.SetVectorArray(BaseColor, bondColors);
        renderParams.matProps = propertyBlock;
        if (renderBonds && bondColors.Count > 0)
        {
            Graphics.RenderMeshInstanced(renderParams, BondMesh, 0, bondTransforms);
        }
    }
}