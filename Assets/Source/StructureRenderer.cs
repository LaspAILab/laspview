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
    public static StructureRenderer Instance;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int RimColor = Shader.PropertyToID("_RimColor");

    public static Color FixedRimColor = new Vector4(0.247f, 0.816f, 0.831f, 2);
    public static Color ReactRimColor = new Vector4(0.990f, 0.680f, 0.233f, 2);

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public bool OutlineFrozenAtoms = true;

    public CrystalStructureView CurrentStructureView { get; private set; } = null;

    public float StickRadius
    {
        get;
        private set;
    } = 0.35f;

    public float BallRadiusScale
    {
        get;
        private set;
    } = 1f;

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

    public void SetDisplayType(bool showBalls, bool showSticks)
    {
        renderAtoms = showBalls;
        renderBonds = showSticks;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        renderParams = new RenderParams(RenderMaterial)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
    }

    public void SetBondRadius(float stickRadius)
    {
        if (Mathf.Approximately(this.StickRadius, stickRadius))
        {
            return;
        }

        this.StickRadius = stickRadius;

        // Refresh bond transforms
        foreach (var item in CurrentStructureView.Bonds)
        {
            item.CreateTransformCache();
        }

        RecollectDrawBatches();
    }

    public void SetAtomRadius(float radiusScale)
    {
        if (Mathf.Approximately(BallRadiusScale, radiusScale))
        {
            return;
        }

        BallRadiusScale = radiusScale;

        // Refresh atom transforms
        foreach (var item in CurrentStructureView.Atoms)
        {
            item.CreateTransformCache();
        }

        RecollectDrawBatches();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Color GetAtomColor(AtomView atom, int aIndex, int bIndex, int cIndex)
    {
        return atom.SelectedCellIndex != null ? Color.yellow : atom.DisplayColor;
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

        int repeatA, repeatB, repeatC;

        latticeTransforms ??= new [,,] {{{ Matrix4x4.identity }}};
        repeatA = latticeTransforms.GetLength(0);
        repeatB = latticeTransforms.GetLength(1);
        repeatC = latticeTransforms.GetLength(2);

        // GC.Collect();
        atomInstancingData.Clear();
        atomInstancingData.Capacity = CurrentStructureView.Atoms.Count * repeatA * repeatB * repeatC;

        bondInstancingData.Clear();
#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.WaitOne();
#endif
        bondInstancingData.Capacity = CurrentStructureView.Bonds.Count * repeatA * repeatB * repeatC;

        for (int i = 0; i < repeatA; i++)
        {
            for (int j = 0; j < repeatB; j++)
            {
                for (int k = 0; k < repeatC; k++)
                {
                    Matrix4x4 transformMatrix = latticeTransforms[i, j, k];

                    foreach (var item in CurrentStructureView.Atoms)
                    {
                        if (item.IsHidden)
                        {
                            continue;
                        }

                        atomInstancingData.Transforms.Add(transformMatrix * item.Transform);
                        atomInstancingData.Colors.Add(GetAtomColor(item, i,j,k));
                        atomInstancingData.RimColors.Add(GetRimColor(item.SourceAtom.Flags));
                    }

                    foreach (var item in CurrentStructureView.Bonds)
                    {
                        if(item.FromAtom.IsHidden || item.ToAtom.IsHidden)
                        {
                            continue;
                        }

                        bondInstancingData.Transforms.Add(transformMatrix * item.Transform);
                        bondInstancingData.Colors.Add(GetAtomColor(item.FromAtom, i, j, k));
                        // Add blue rim for bonds only if two atoms are both fixed
                        bondInstancingData.RimColors.Add(GetRimColor(item.FromAtom.SourceAtom.Flags == item.ToAtom.SourceAtom.Flags ? item.FromAtom.SourceAtom.Flags
                            : AtomFlags.None));
                    }
                }
            }
        }

#if !UNITY_WEBGL
        CurrentStructureView.BondMutex.ReleaseMutex();
#endif
        Profiler.EndSample();
    }

    internal void RenderSingleAtom(AtomView atom, Vector3 pos, (int, int, int) cellIndex, float radius)
    {
        var props = new MaterialPropertyBlock();
        props.SetColor(BaseColor, GetAtomColor(atom, cellIndex.Item1, cellIndex.Item2, cellIndex.Item3));
        props.SetColor(RimColor, GetRimColor(atom.SourceAtom.Flags));
        renderParams.matProps = props;
        var transformMat = Matrix4x4.TRS(pos, Quaternion.identity, radius * BallRadiusScale * Vector3.one);
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