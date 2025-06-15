using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

struct InstancingData
{
    public List<Matrix4x4> Transforms;
    public List<Vector4> Colors;
    public List<Vector4> RimColors;

    public int InstanceCount => Transforms.Count;

    public int Capacity
    {
        get => Transforms.Capacity;
        set
        {
            Transforms.Capacity = value;
            Colors.Capacity = value;
            RimColors.Capacity = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Transforms.Clear();
        Colors.Clear();
        RimColors.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Matrix4x4 matrix, Vector4 color, Vector4 rimColor)
    {
        Transforms.Add(matrix);
        Colors.Add(color);
        RimColors.Add(rimColor);
    }
}

public class StructureRenderer : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int RimColor = Shader.PropertyToID("_RimColor");

    public Color FixedRimColor = new Vector4(0.247f, 0.816f, 0.831f, 2);
    public Color ReactRimColor = new Vector4(0.990f, 0.680f, 0.233f, 2);

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public bool OutlineFrozenAtoms = true;

    public CrystalStructureView CurrentStructureView { get; private set; } = null;

    private float stickRadius = 0.35f, radiusScale = 1f;
    private Matrix4x4[,,] latticeTransforms = null;

    private InstancingData atomInstancingData = new InstancingData
    {
        Transforms = new List<Matrix4x4>(),
        Colors = new List<Vector4>(),
        RimColors = new List<Vector4>()
    };

    private InstancingData bondInstancingData = new InstancingData
    {
        Transforms = new List<Matrix4x4>(),
        Colors = new List<Vector4>(),
        RimColors = new List<Vector4>()
    };

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

    Vector4 GetRimColor(AtomFlags flags)
    {
        switch (flags)
        {
            case AtomFlags.Fixed:
                return FixedRimColor;
            case AtomFlags.React:
                return ReactRimColor;
            default:
                return Vector4.zero;
        }
    }

    public void SetLatticeTransforms(Matrix4x4[,,] newLatticeTransforms)
    {
        latticeTransforms = newLatticeTransforms;
        RecollectDrawBatches();
    }

    // Duplicate render items, to render multiple lattices
    private void ReplicateBuffers(ref InstancingData instancingData, int repeatA, int repeatB, int repeatC)
    {
        if (latticeTransforms == null)
        {
            return;
        }

        int instanceCount = instancingData.InstanceCount;
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

                    for (int count = 0; count < instanceCount; count++)
                    {
                        instancingData.Transforms.Add(latticeTransforms[i, j, k] * instancingData.Transforms[count]);
                        instancingData.Colors.Add(instancingData.Colors[count]);
                        instancingData.RimColors.Add(instancingData.RimColors[count]);
                    }
                }
            }
        }

        Assert.AreEqual(instancingData.InstanceCount, instanceCount * repeatA * repeatB * repeatC);
    }

    internal void SetStructureView(CrystalStructureView structureView, Matrix4x4[,,] newLatticeTransforms)
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
        atomInstancingData.Clear();
        atomInstancingData.Capacity = CurrentStructureView.Atoms.Count * repeatA * repeatB * repeatC;
        foreach (var item in CurrentStructureView.Atoms)
        {
            atomInstancingData.Add(item.Transform, GetAtomColor(item), GetRimColor(item.SourceAtom.Flags));
        }

        bondInstancingData.Clear();
        bondInstancingData.Capacity = CurrentStructureView.Bonds.Count * repeatA * repeatB * repeatC;
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.WaitOne();
#endif
        foreach (var item in CurrentStructureView.Bonds)
        {
            // Add blue rim for bonds only if two atoms are both fixed
            Vector4 rimColor = GetRimColor(item.FromAtom.SourceAtom.Flags == item.ToAtom.SourceAtom.Flags ? item.FromAtom.SourceAtom.Flags
                : AtomFlags.None);
            bondInstancingData.Add(item.Transform, GetAtomColor(item.FromAtom), rimColor);
        }
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.ReleaseMutex();
#endif

        ReplicateBuffers(ref atomInstancingData, repeatA, repeatB, repeatC);
        ReplicateBuffers(ref bondInstancingData, repeatA, repeatB, repeatC);
        Profiler.EndSample();
    }

    internal void RenderSingleAtom(AtomView atom, Vector3 pos, float radius)
    {
        var props = new MaterialPropertyBlock();
        props.SetColor(BaseColor, GetAtomColor(atom));
        props.SetColor(RimColor, GetRimColor(atom.SourceAtom.Flags));
        renderParams.matProps = props;
        var transformMat = Matrix4x4.TRS(pos, Quaternion.identity, radius * radiusScale * Vector3.one);
        Graphics.RenderMesh(renderParams, AtomMesh, 0, transformMat);
    }

    // Update is called once per frame
    void Update()
    {
        if (atomInstancingData.InstanceCount == 0)
        {
            return;
        }

        var propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetVectorArray(BaseColor, atomInstancingData.Colors);
        if (OutlineFrozenAtoms)
        {
            propertyBlock.SetVectorArray(RimColor, atomInstancingData.RimColors);
        }

        renderParams.matProps = propertyBlock;
        if (renderAtoms)
        {
            Graphics.RenderMeshInstanced(renderParams, AtomMesh, 0, atomInstancingData.Transforms);
        }

        if (bondInstancingData.InstanceCount == 0)
        {
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetVectorArray(BaseColor, bondInstancingData.Colors);
        if (OutlineFrozenAtoms)
        {
            propertyBlock.SetVectorArray(RimColor, bondInstancingData.RimColors);
        }

        renderParams.matProps = propertyBlock;
        if (renderBonds && bondInstancingData.InstanceCount > 0)
        {
            Graphics.RenderMeshInstanced(renderParams, BondMesh, 0, bondInstancingData.Transforms);
        }
    }
}