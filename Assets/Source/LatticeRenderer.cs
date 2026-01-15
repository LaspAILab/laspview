using System.Collections.Generic;
using UnityEngine;

public class LatticeRenderer : MonoBehaviour
{
    public GameObject latticeLinePrefab;
    public GameObject dottedLatticeLinePrefab;
    public GameObject latticeLineParent;
    
    public float LineWidth
    {
        get => lineWidth;
        set
        {
            if (Mathf.Approximately(value, lineWidth))
            {
                return;
            }
            
            lineWidth = value;
            UpdateLineProperties();
        }
    }

    public Color LineColor
    {
        get => lineColor;
        set
        {
            if (lineColor == value)
            {
                return;
            }
            
            lineColor = value;
            UpdateLineProperties();
        }
    }

    public bool UseDottedLines
    {
        get => useDottedLines;
        set
        {
            if (useDottedLines == value)
            {
                return;
            }
            
            useDottedLines = value;
            RegenerateLines();
        }
    }

    public float DashScale
    {
        get => dashScale;
        set
        {
            if (Mathf.Approximately(value, dashScale))
            {
                return;
            }
            
            dashScale = value;
            RegenerateLines();
        }
    }

    private float lineWidth = 0.05f;
    private float dashScale = 20f;
    private Color lineColor = Color.white;
    private BoxCollider boxCollider;
    private bool isEnabled;
    
    // Call set lattice to update lines
    private bool useDottedLines = false;

    void Awake()
    {
        isEnabled = true;
    }

    private void AddLine(Vector3 from, Vector3 to, bool dotted = false)
    {
        LineRenderer line;
        if (dotted)
        {
            var go = Instantiate(dottedLatticeLinePrefab, latticeLineParent.transform);
            var dottedLineScript = go.GetComponent<DottedLineScript>();
            dottedLineScript.Init();
            dottedLineScript.DotLengthScale = dashScale;
            dottedLineScript.SetLine(from, to);
            
            line = go.GetComponent<LineRenderer>();
        }
        else
        {
            line = Instantiate(latticeLinePrefab, latticeLineParent.transform).GetComponent<LineRenderer>();
            line.SetPositions(new[] {from, to});
        }
        
        line.startWidth = LineWidth;
        line.endWidth = LineWidth;
        line.startColor = LineColor;
        line.endColor = LineColor;
    }

    public void SetLattice(CrystalStructure structure, int repeatA, int repeatB, int repeatC)
    {
        if (boxCollider is null)
        {
            boxCollider = latticeLineParent.GetComponent<BoxCollider>();
        }

        // Remove existing children
        for (int i = latticeLineParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(latticeLineParent.transform.GetChild(i).gameObject);
        }

        Vector3 furthestPoint = Vector3.zero;

        for (int i = 0; i <= repeatA; i++)
        {
            for (int j = 0; j <= repeatB; j++)
            {
                for (int k = 0; k <= repeatC; k++)
                {
                    Vector3 from = CrystalStructureView.SwapYZ(
                        structure.FractionalToCartesian(new Vector3(i, j, k)));
                    if (i == 0)
                    {
                        Vector3 to = CrystalStructureView.SwapYZ(
                            structure.FractionalToCartesian(new Vector3(repeatA, j, k)));
                        furthestPoint = Vector3.Max(furthestPoint, to);
                        AddLine(from, to, useDottedLines);
                    }

                    if (j == 0)
                    {
                        Vector3 to = CrystalStructureView.SwapYZ(
                            structure.FractionalToCartesian(new Vector3(i, repeatB, k)));
                        furthestPoint = Vector3.Max(furthestPoint, to);
                        AddLine(from, to, useDottedLines);
                    }

                    if (k == 0)
                    {
                        Vector3 to = CrystalStructureView.SwapYZ(
                            structure.FractionalToCartesian(new Vector3(i, j, repeatC)));
                        furthestPoint = Vector3.Max(furthestPoint, to);
                        AddLine(from, to, useDottedLines);
                    }
                }
            }
        }

        // Set bounding box
        boxCollider.center = furthestPoint / 2;
        boxCollider.size = furthestPoint;

        if (!isEnabled)
        {
            SetDisplay(false);
        }
    }

    public void SetDisplay(bool show)
    {
        foreach (var line in latticeLineParent.GetComponentsInChildren<LineRenderer>())
        {
            line.enabled = show;
        }
        
        isEnabled = show;
    }

    private void UpdateLineProperties()
    {
        foreach (var line in latticeLineParent.GetComponentsInChildren<LineRenderer>())
        {
            line.startWidth = LineWidth;
            line.endWidth = LineWidth;
            line.startColor = LineColor;
            line.endColor = LineColor;
        }
    }
    
    private void RegenerateLines()
    {
        List<(Vector3, Vector3)> linePositions = new();
        
        foreach (var line in latticeLineParent.GetComponentsInChildren<LineRenderer>())
        {
            linePositions.Add((line.GetPosition(0), line.GetPosition(1)));
            Destroy(line.gameObject);
        }
        
        foreach (var (from, to) in linePositions)
        {
            AddLine(from, to, useDottedLines);
        }
    }
}