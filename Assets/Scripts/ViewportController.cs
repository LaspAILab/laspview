using System.Threading.Tasks;
using UnityEngine;

public class ViewportController : MonoBehaviour
{
    public StructureRenderer StructureRenderer;

    private float bondTolerance = 1.26f;
    private bool bondRecalculationRunning = false, bondToleranceDirtyFlag = false;
    private int repeatA = 1, repeatB = 1, repeatC = 1;

    public ArcStructureView CurrentStructureView
    {
        get;
        private set;
    } = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StructureRenderer = GetComponent<StructureRenderer>();
    }

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
                    bondRecalculationRunning = false;
                    if (bondToleranceDirtyFlag)
                    {
                        RecalculateBonds();
                    }
                });
            });
        }
    }

    public void OpenStructureView(ArcStructureView structureView)
    {
        CurrentStructureView?.ReleaseColliders();

        CurrentStructureView = structureView;
        CurrentStructureView.GenerateBonds(bondTolerance);
        CurrentStructureView.CreateColliders();

        StructureRenderer.SetStructureView(structureView);
        RefocusCamera();
    }

    void RefocusCamera()
    {

    }

    /// <summary>
    /// Set the repeat count of the lattice, along 3 axis
    /// </summary>
    /// <param name="repeatA">Repeat count on a axis</param>
    /// <param name="repeatB">Repeat count on b axis</param>
    /// <param name="repeatC">Repeat count on c axis</param>
    public void SetLatticeRepeat(int? repeatA = null, int? repeatB = null, int? repeatC = null)
    {
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

        StructureRenderer.SetLatticeRepeat(this.repeatA, this.repeatB, this.repeatC);
        RefocusCamera();
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
        {
            if (hitInfo.collider.gameObject.TryGetComponent<ColliderScript>(out var colliderScript))
            {
                var atomView = colliderScript.Atom;
                StructureRenderer.RenderSingleAtom(atomView.RenderMat, atomView.Position, atomView.Radius + 0.5f);
            }
        }
    }
}
