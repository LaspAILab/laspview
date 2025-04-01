using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;


public class ColliderManager : MonoBehaviour
{
    const float MinColliderRadius = 1f;

    public static ColliderManager Instance;

    public GameObject colliderPrefab;
    public GameObject colliderParent;

    private ColliderPool colliderPool;

    public Dictionary<AtomView, ColliderScript[,,]> colliderRegistry = new();

    // To keep track of possible leaks
    private int collidersAlive;
    private Matrix4x4[,,] latticeTransforms = null;
    private int repeatA, repeatB, repeatC;

    void Awake()
    {
        colliderPool = new(colliderPrefab, colliderParent);
        Instance = this;
    }

    public void SetLatticeTransforms(Matrix4x4[,,] newLatticeTransforms)
    {
        latticeTransforms = newLatticeTransforms;

        repeatA = latticeTransforms.GetLength(0);
        repeatB = latticeTransforms.GetLength(1);
        repeatC = latticeTransforms.GetLength(2);

        foreach (var atomView in colliderRegistry.Keys.ToList())
        {
            DropColliderForAtom(atomView);
            CreateColliderForAtom(atomView);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateColliderForAtom(AtomView atomView)
    {
        var atomColliders = new ColliderScript[repeatA, repeatB, repeatC];
        for (int i = 0; i < repeatA; i++)
        {
            for (int j = 0; j < repeatB; j++)
            {
                for (int k = 0; k < repeatC; k++)
                {
                    atomColliders[i, j, k] = colliderPool.BorrowCollider();
                    collidersAlive++;
                    var atomPos = new Vector4(atomView.Position.x, atomView.Position.y, atomView.Position.z, 1);
                    atomColliders[i, j, k].SetAtom(atomView, latticeTransforms[i, j, k] * atomPos,
                        Mathf.Max(atomView.Radius, MinColliderRadius));
                }
            }
        }

        colliderRegistry.Add(atomView, atomColliders);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateColliderForAtom(AtomView atomView)
    {
        var atomColliders = colliderRegistry[atomView];
        for (int i = 0; i < repeatA; i++)
        {
            for (int j = 0; j < repeatB; j++)
            {
                for (int k = 0; k < repeatC; k++)
                {
                    var atomPos = new Vector4(atomView.Position.x, atomView.Position.y, atomView.Position.z, 1);
                    atomColliders[i, j, k].SetAtom(atomView, latticeTransforms[i, j, k] * atomPos,
                        Mathf.Max(atomView.Radius, MinColliderRadius));
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DropColliderForAtom(AtomView atomView)
    {
        DropColliders(colliderRegistry[atomView]);
        colliderRegistry.Remove(atomView);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DropColliders(ColliderScript[,,] colliders)
    {
        foreach (var item in colliders)
        {
            colliderPool.DropCollider(item);
            collidersAlive--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateCollidersForStructure(ArcStructureView arcStructureView)
    {
        DropAll();

        foreach (var atomView in arcStructureView.Atoms)
        {
            CreateColliderForAtom(atomView);
        }

        colliderPool.ShrinkCapacity();
    }

    public void DropAll()
    {
        foreach (var kv in colliderRegistry)
        {
            DropColliders(kv.Value);
        }

        Assert.AreEqual(0, collidersAlive);
        colliderRegistry.Clear();
    }
}