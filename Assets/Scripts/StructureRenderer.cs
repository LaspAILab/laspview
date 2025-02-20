using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

// Batch of atoms and bonds that belong to the same element, so that they could be rendered in batch
class DrawBatch
{
    public List<Matrix4x4> AtomTransforms = new();
    public List<Matrix4x4> BondTransforms = new();
}

public class StructureRenderer : MonoBehaviour
{
    const string RENDERMAT_SELECTION = "select";

    static Dictionary<string, Material> ElementMaterials = new();
    Dictionary<string, DrawBatch> ElementDrawBatches = new();

    public Material RenderMaterial;
    public Mesh AtomMesh;
    public Mesh BondMesh;

    public GameObject ColliderPrefab;
    public GameObject ColliderParent;

    public ArcStructureView CurrentStructureView
    {
        get;
        private set;
    } = null;

    private int repeatA = 1, repeatB = 1, repeatC = 1;
    private float bondTolerance = 1.26f, stickRadius = 0.35f, radiusScale = 1f;
    private bool bondRecalculationRunning = false, bondToleranceDirtyFlag = false;
    private Matrix4x4[,,] latticeTransforms = null;

    public float BondTolerance
    {
        get => bondTolerance;
        set
        {
            if (bondTolerance == value)
            {
                return;
            }

            bondTolerance = value;
            RecalculateBonds();
        }
    }

    private void RecalculateBonds()
    {
        if (bondRecalculationRunning)
        {
            // Hint background task to rerun
            bondToleranceDirtyFlag = true;
        }
        else
        {
            bondRecalculationRunning = true;
            bondToleranceDirtyFlag = false;
            Task.Run(() =>
            {
                CurrentStructureView.GenerateBonds(bondTolerance);
                CurrentStructureView.SortLists();

                MainScr.callbackQueue.Enqueue(() =>
                {
                    CollectDrawBatches();
                    bondRecalculationRunning = false;
                    if (bondToleranceDirtyFlag)
                    {
                        RecalculateBonds();
                    }
                });
            });
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ColliderPool.GlobalPool = new(ColliderPrefab, ColliderParent);
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

        CollectDrawBatches();
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

        CollectDrawBatches();
    }

    public void SetLatticeRepeat(int? repeatA = null, int? repeatB = null, int? repeatC = null)
    {
        Debug.Log($"Setting lattice repeat {repeatA}, {repeatB}, {repeatC}");
        bool modified = false;
        if (repeatA != null)
        {
            modified = modified || this.repeatA != repeatA.Value;
            this.repeatA = repeatA.Value;
        }

        if (repeatB != null)
        {
            modified = modified || this.repeatB != repeatB.Value;
            this.repeatB = repeatB.Value;
        }

        if (repeatC != null)
        {
            modified = modified || this.repeatC != repeatC.Value;
            this.repeatC = repeatC.Value;
        }

        if (!modified)
        {
            return;
        }

        latticeTransforms = new Matrix4x4[this.repeatA, this.repeatB, this.repeatC];
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

        CollectDrawBatches();
    }

    // Duplicate render items, to render multiple lattices
    private void ReplicateBuffer(ref List<Matrix4x4> buffer)
    {
        if (latticeTransforms == null || latticeTransforms.Length == 1)
        {
            return;
        }

        int length = buffer.Count;
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

    private void SubmitAtomBuffer(string renderMat, List<Matrix4x4> buffer)
    {
        if (!ElementDrawBatches.ContainsKey(renderMat))
        {
            ElementDrawBatches[renderMat] = new();
        }
        ReplicateBuffer(ref buffer);
        ElementDrawBatches[renderMat].AtomTransforms = buffer;

        if (!ElementMaterials.ContainsKey(renderMat))
        {
            Material material = new(RenderMaterial);
            if(renderMat == RENDERMAT_SELECTION)
            {
                material.SetColor("_BaseColor", new Color());
            }
            else
            {
                material.SetColor("_BaseColor", ConFun.Elements[renderMat].Color);
            }
                
            ElementMaterials[renderMat] = material;
        }
    }

    private void SubmitBondBuffer(string renderMat, List<Matrix4x4> buffer)
    {
        ReplicateBuffer(ref buffer);
        ElementDrawBatches[renderMat].BondTransforms = buffer;
    }

    public void LoadStructure(ArcStructureView structureView)
    {
        CurrentStructureView?.ReleaseColliders();

        CurrentStructureView = structureView;
        CurrentStructureView.GenerateBonds(bondTolerance);
        CurrentStructureView.SortLists();
        CurrentStructureView.CreateColliders();

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

        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
        {
            if (hitInfo.collider.gameObject.TryGetComponent<ColliderScript>(out var colliderScript))
            {
                var atomView = CurrentStructureView.Atoms[colliderScript.AtomIndex];
                RenderParams renderParams = new(ElementMaterials[atomView.RenderMat]);
                var transformMat = Matrix4x4.TRS(atomView.Position, Quaternion.identity, (0.5f + atomView.Radius) * radiusScale * Vector3.one);
                Graphics.RenderMesh(renderParams, AtomMesh, 0, transformMat);
            }
        }
    }
}
