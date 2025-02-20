using System.Collections.Generic;
using System.Drawing.Printing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

enum ToolType
{
    NONE,
    MOVE_VIEW,
    ROTATE_VIEW,
}

public class ViewportController : MonoBehaviour
{
    public StructureRenderer StructureRenderer;
    public Camera ActiveCamera;

    private float bondTolerance = 1.26f;
    private bool bondRecalculationRunning = false, bondToleranceDirtyFlag = false;
    private int repeatA = 1, repeatB = 1, repeatC = 1;
    private List<AtomView> selection = new();

    private Vector3 cameraCenter;
    // Angles in sphere coordinates, in radians!
    private float cameraAltitude = 0f, cameraAzimuth = 0f;
    // How far the camera is from the focal center
    private float cameraViewportSize = 0f;
    private Vector3 prevMousePos;
    private ToolType currentTool = ToolType.NONE;

    public ArcStructureView CurrentStructureView
    {
        get;
        private set;
    } = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StructureRenderer = GetComponent<StructureRenderer>();
        ActiveCamera = Camera.main;
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
        cameraCenter = CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC)) / 2;
        cameraViewportSize = cameraCenter.magnitude + 3.5f;

        UpdateCameraTransform();
    }

    public void UpdateCameraTransform()
    {
        float cosAltitude = Mathf.Cos(cameraAltitude);
        var pos = new Vector3(cosAltitude * Mathf.Sin(cameraAzimuth), Mathf.Sin(cameraAltitude), -cosAltitude * Mathf.Cos(cameraAzimuth));
        pos *= (4 * cameraCenter.magnitude);
        pos += cameraCenter;

        var rot = Quaternion.Euler(cameraAltitude * Mathf.Rad2Deg, -cameraAzimuth * Mathf.Rad2Deg, 0);
        ActiveCamera.transform.SetPositionAndRotation(pos, rot);
        ActiveCamera.orthographicSize = cameraViewportSize;
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

        HandleMouseEvents();
    }

    void UpdateCamera(float? deltaAltitude, float? deltaAzimuth, float? deltaViewportSize, Vector3? deltaCenterPos)
    {
        if (deltaAltitude.HasValue)
        {
            cameraAltitude = Mathf.Clamp(cameraAltitude + deltaAltitude.Value, -Mathf.PI / 2, Mathf.PI / 2);
        }

        if (deltaAzimuth.HasValue)
        {
            cameraAzimuth = (cameraAzimuth + deltaAzimuth.Value) % (2f * Mathf.PI);
        }

        if (deltaViewportSize.HasValue)
        {
            cameraViewportSize = Mathf.Clamp(cameraViewportSize + deltaViewportSize.Value, 0, 10000f);
        }

        if (deltaCenterPos.HasValue)
        {
            cameraCenter += deltaCenterPos.Value;
        }

        UpdateCameraTransform();
    }

    void HandleMouseEvents()
    {
        bool cameraRotated = false, cameraZoomed = Mathf.Abs(Input.mouseScrollDelta.y) > 1e-5, cameraMoved = false;
        var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        if (Input.GetKey(KeyCode.LeftAlt))
        {

        }
        else
        {
            // Viewport op
            if (Input.GetKey(KeyCode.Mouse1))
            {
                switch (currentTool)
                {
                    case ToolType.MOVE_VIEW:
                    case ToolType.NONE:
                        currentTool = ToolType.ROTATE_VIEW;
                        break;
                    case ToolType.ROTATE_VIEW:
                        cameraRotated = true;
                        break;
                }
            }
            else
            {
                if (currentTool == ToolType.ROTATE_VIEW)
                {
                    cameraRotated = true;
                    currentTool = ToolType.NONE;
                }
            }

            if (Input.GetKey(KeyCode.Mouse2))
            {
                switch (currentTool)
                {
                    case ToolType.ROTATE_VIEW:
                    case ToolType.NONE:
                        currentTool = ToolType.MOVE_VIEW;
                        break;
                    case ToolType.MOVE_VIEW:
                        cameraMoved = true;
                        break;
                }
            }
            else
            {
                if (currentTool == ToolType.MOVE_VIEW)
                {
                    cameraMoved = true;
                    currentTool = ToolType.NONE;
                }
            }

            if (cameraRotated || cameraMoved || cameraZoomed)
            {
                float? deltaAlt = cameraRotated ? Input.mousePositionDelta.y * -0.01f : null;
                float? deltaAzi = cameraRotated ? Input.mousePositionDelta.x * -0.01f : null;
                float? deltaViewportSize = cameraZoomed ? Input.mouseScrollDelta.y * -5 : null;
                
                Vector3? deltaCenter = cameraMoved ? (ActiveCamera.ScreenToWorldPoint(prevMousePos) - mouseWorldPos) : null;
                UpdateCamera(deltaAlt, deltaAzi, deltaViewportSize, deltaCenter);
            }

            prevMousePos = Input.mousePosition;
        }
    }
}
