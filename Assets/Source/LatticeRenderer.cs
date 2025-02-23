using UnityEngine;

public class LatticeRenderer : MonoBehaviour
{
    public GameObject LatticeLinePrefab;
    public GameObject LatticeLineParent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    private void AddLine(Vector3 from, Vector3 to, float thickness = 0.2f)
    {
        LineRenderer line = Instantiate(LatticeLinePrefab, LatticeLineParent.transform).GetComponent<LineRenderer>();
        line.startWidth = thickness;
        line.endWidth = thickness;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    public void RenderLattice(ArcStructure structure, int repeatA, int repeatB, int repeatC)
    {
        // Remove existing children
        for (int i = LatticeLineParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(LatticeLineParent.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i <= repeatA; i++)
        {
            for (int j = 0; j <= repeatB; j++)
            {
                for (int k = 0; k <= repeatC; k++)
                {
                    Vector3 from = structure.FractionalToCartesian(new Vector3(i, j, k));
                    if (i == 0)
                    {
                        Vector3 to = structure.FractionalToCartesian(new Vector3(repeatA, j, k));
                        AddLine(from, to);
                    }

                    if (j == 0)
                    {
                        Vector3 to = structure.FractionalToCartesian(new Vector3(i, repeatB, k));
                        AddLine(from, to);
                    }

                    if (k == 0)
                    {
                        Vector3 to = structure.FractionalToCartesian(new Vector3(i, j, repeatC));
                        AddLine(from, to);
                    }
                }
            }
        }
    }
}