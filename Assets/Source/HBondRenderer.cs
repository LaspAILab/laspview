using System.Collections.Generic;
using UnityEngine;

public struct HBond
{
    public AtomView HAtom;

    public AtomView AcceptorAtom;

    // If the bond is across the lattice, this is the translation of the H atom
    public Vector3? TranslationVector;
}

public static class CrystalHBondExtension
{
    public static void UpdateHBondSingleAtom(this CrystalStructureView structureView, AtomView atom,
        ref List<HBond> bondList, float maxDistance)
    {
        if (atom.Symbol != "H")
        {
            return;
        }

        List<AtomView> nearbyAtoms = new();
        // Check bond across lattice
        var adjacentCellCount = CrystalStructureView.AdjacentCellCounts[(int)CrystalStructureView.CrossCellPolicy];

        structureView.AccelerationOctree.GetNearbyNonAlloc(atom.Position, maxDistance, nearbyAtoms);
        foreach (var otherAtom in nearbyAtoms)
        {
            if (otherAtom.Symbol is not ("F" or "O" or "N"))
            {
                continue;
            }

            bondList.Add(new HBond
            {
                HAtom = atom,
                AcceptorAtom = otherAtom,
                TranslationVector = null
            });
        }

        for (int i = 0; i < adjacentCellCount; i++)
        {
            var translatedCoord = atom.Position + structureView.TranslationVectorsCartesian[i];
            structureView.AccelerationOctree.GetNearbyNonAlloc(translatedCoord, maxDistance, nearbyAtoms);

            foreach (var otherAtom in nearbyAtoms)
            {
                if (otherAtom.Symbol is not ("F" or "O" or "N"))
                {
                    continue;
                }

                bondList.Add(new HBond
                {
                    HAtom = atom,
                    AcceptorAtom = otherAtom,
                    TranslationVector = structureView.TranslationVectorsCartesian[i]
                });
            }
        }
    }

    public static void GenerateHBonds(this CrystalStructureView structureView, ref List<HBond> bondList,
        float maxDistance)
    {
        bondList.Clear();

        foreach (var atom in structureView.Atoms)
        {
            structureView.UpdateHBondSingleAtom(atom, ref bondList, maxDistance);
        }
    }
}

public class HBondRenderer : MonoBehaviour
{
    const float HBOND_THRESHOLD = 2f;

    public static HBondRenderer Instance;

    public GameObject dottedLinePrefab;
    public GameObject dottedLineParent;

    private bool isEnabled = true;

    private CrystalStructureView structureView;
    private GameObjectPool<DottedLineScript> dottedLinePool;
    private List<HBond> hydrogenBonds = new();
    private List<DottedLineScript> bondObjects = new();
    private Matrix4x4[,,] latticeTransforms = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        dottedLinePool = new(dottedLinePrefab, dottedLineParent);
        Instance = this;
    }

    void AddHBond(Vector3 startPos, Vector3 endPos)
    {
        DottedLineScript dottedLineScript = dottedLinePool.BorrowObject();
        dottedLineScript.SetLine(startPos, endPos);
        bondObjects.Add(dottedLineScript);
    }

    void RemoveAddHBonds()
    {
        foreach (var hBond in bondObjects)
        {
            dottedLinePool.DropObject(hBond);
        }

        bondObjects.Clear();
    }

    private void CreateLines()
    {
        RemoveAddHBonds();

        if (latticeTransforms == null)
        {
            latticeTransforms = new Matrix4x4[1, 1, 1];
            latticeTransforms[0, 0, 0] = Matrix4x4.identity;
        }

        int repeatA = latticeTransforms.GetLength(0);
        int repeatB = latticeTransforms.GetLength(1);
        int repeatC = latticeTransforms.GetLength(2);
        foreach (var bond in hydrogenBonds)
        {
            bool isCrossLattice = bond.TranslationVector.HasValue;
            // Cast to Vector4 to do proper matrix multiplication
            Vector4 startPos = bond.HAtom.Position, endPos = bond.AcceptorAtom.Position;
            startPos.w = 1;
            endPos.w = 1;
            Vector4 midPoint = Vector4.zero;
            if (isCrossLattice)
            {
                midPoint = (startPos + endPos + (Vector4)bond.TranslationVector.Value) / 2;
                midPoint.w = 1;
            }

            for (int i = 0; i < repeatA; i++)
            {
                for (int j = 0; j < repeatB; j++)
                {
                    for (int k = 0; k < repeatC; k++)
                    {
                        if (isCrossLattice)
                        {
                            // As for cross-lattice bonds, split into two lines
                            AddHBond(latticeTransforms[i, j, k] * startPos,
                                latticeTransforms[i, j, k] * (midPoint - (Vector4)bond.TranslationVector.Value));
                            AddHBond(latticeTransforms[i, j, k] * midPoint, latticeTransforms[i, j, k] * endPos);
                        }
                        else
                        {
                            AddHBond(latticeTransforms[i, j, k] * startPos, latticeTransforms[i, j, k] * endPos);
                        }
                    }
                }
            }
        }
    }

    public void SetStructure(CrystalStructureView structureView, Matrix4x4[,,] newLatticeTransforms)
    {
        this.structureView = structureView;
        latticeTransforms = newLatticeTransforms;

        if (isEnabled)
        {
            Regenerate();
        }
    }

    private void Regenerate()
    {
        structureView.GenerateHBonds(ref hydrogenBonds, HBOND_THRESHOLD);
        CreateLines();
    }
    
    // Caution: this function doesn't recalculate bonds, so only use this when 
    public void OnLatticeRepeatChange(Matrix4x4[,,] newLatticeTransforms)
    {
        latticeTransforms = newLatticeTransforms;
        
        if (isEnabled)
        {
            CreateLines();
        }
    }

    public void OnAtomRemove(AtomView atom)
    {
        if (!isEnabled)
        {
            return;
        }
        
        hydrogenBonds.RemoveAll(x => x.HAtom == atom || x.AcceptorAtom == atom);
        CreateLines();
    }

    public void OnAtomUpdate(CrystalStructureView structureView, AtomView atom)
    {
        if (!isEnabled)
        {
            return;
        }
        
        if (atom.Symbol is not ("H" or "O" or "N" or "F"))
        {
            return;
        }
        
        structureView.GenerateHBonds(ref hydrogenBonds, HBOND_THRESHOLD);
     
        CreateLines();
    }

    public void SetDisplay(bool show)
    {
        if (isEnabled == show)
        {
            return;
        }
        
        isEnabled = show;
        if (isEnabled)
        {
            Regenerate();
        }
        else
        {
            RemoveAddHBonds();
            hydrogenBonds.Clear();
        }
    }
}