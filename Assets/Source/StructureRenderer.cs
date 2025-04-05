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
    public List<float> RimStrengths;

    public int InstanceCount => Transforms.Count;

    public int Capacity
    {
        get => Transforms.Capacity;
        set
        {
            Transforms.Capacity = value;
            Colors.Capacity = value;
            RimStrengths.Capacity = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Transforms.Clear();
        Colors.Clear();
        RimStrengths.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Matrix4x4 matrix, Vector4 color, float strength)
    {
        Transforms.Add(matrix);
        Colors.Add(color);
        RimStrengths.Add(strength);
    }
}

public class StructureRenderer : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int RimStrength = Shader.PropertyToID("_RimStrength");

    // Rim strength for frozen atoms
    private const float FrozenRimStrength = 2f;

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public bool OutlineFrozenAtoms = true;

    public ArcStructureView CurrentStructureView { get; private set; } = null;

    private float stickRadius = 0.35f, radiusScale = 1f;
    private Matrix4x4[,,] latticeTransforms = null;

    private InstancingData atomInstancingData = new InstancingData
    {
        Transforms = new List<Matrix4x4>(),
        Colors = new List<Vector4>(),
        RimStrengths = new List<float>()
    };

    private InstancingData bondInstancingData = new InstancingData
    {
        Transforms = new List<Matrix4x4>(),
        Colors = new List<Vector4>(),
        RimStrengths = new List<float>()
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
                        instancingData.RimStrengths.Add(instancingData.RimStrengths[count]);
                    }
                }
            }
        }

        Assert.AreEqual(instancingData.InstanceCount, instanceCount * repeatA * repeatB * repeatC);
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
        atomInstancingData.Clear();
        atomInstancingData.Capacity = CurrentStructureView.Atoms.Count * repeatA * repeatB * repeatC;
        foreach (var item in CurrentStructureView.Atoms)
        {
            atomInstancingData.Add(item.Transform, GetAtomColor(item),
                item.SourceAtom.FixedFlags > 0 ? FrozenRimStrength : 0);
        }

        bondInstancingData.Clear();
        bondInstancingData.Capacity = CurrentStructureView.Bonds.Count * repeatA * repeatB * repeatC;
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.WaitOne();
#endif
        foreach (var item in CurrentStructureView.Bonds)
        {
            // Add blue rim for bonds only if two atoms are both fixed
            float rimStrength = (item.FromAtom.SourceAtom.FixedFlags > 0 && item.ToAtom.SourceAtom.FixedFlags > 0)
                ? FrozenRimStrength
                : 0;
            bondInstancingData.Add(item.Transform, GetAtomColor(item.FromAtom), rimStrength);
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
            propertyBlock.SetFloatArray(RimStrength, atomInstancingData.RimStrengths);
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
            propertyBlock.SetFloatArray(RimStrength, bondInstancingData.RimStrengths);
        }

        renderParams.matProps = propertyBlock;
        if (renderBonds && bondInstancingData.InstanceCount > 0)
        {
            Graphics.RenderMeshInstanced(renderParams, BondMesh, 0, bondInstancingData.Transforms);
        }
    }
}