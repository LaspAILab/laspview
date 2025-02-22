using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

enum ToolType
{
    SELECT,
    MOVE_VIEW,
    ROTATE_VIEW,
    MOVE_ATOM,
    RENAME_ATOM
}

public class ViewportController : MonoBehaviour
{
    const float DOUBLE_CLICK_THRESHOLD = 0.2f;
    
    public GameObject colliderPrefab;
    public GameObject colliderParent;
    
    [NonSerialized] public StructureRenderer structureRenderer;
    [NonSerialized] public LatticeRenderer latticeRenderer;
    [NonSerialized] public SelectionManager selectionManager;
    [NonSerialized] private Camera activeCamera;

    private float bondTolerance = 1.26f;

    private bool bondRecalculationRunning = false, bondToleranceDirtyFlag = false;

    // Whether to update bonds in realtime when moving atoms
    private bool updateBondsRealtime = true;
    private int repeatA = 1, repeatB = 1, repeatC = 1;

    private Vector3 cameraCenter;

    // Angles in sphere coordinates, in radians!
    private float cameraAltitude = 0f, cameraAzimuth = 0f;

    // How far the camera is from the focal center
    private float cameraViewportSize = 0f;
    private Vector3 prevMousePos;
    private ToolType currentTool = ToolType.SELECT;

    public GUIStyle labelStyle = new(), textboxStyle = new();

    private bool isDraggingAtom = false;

    // Trace last clicked atom and click time to detect double clicks
    private AtomView lastClickedAtom = null;
    private float lastClickTime = 0f;

    public ArcStructureView CurrentStructureView { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selectionManager = new();
        structureRenderer = GetComponent<StructureRenderer>();
        latticeRenderer = GetComponent<LatticeRenderer>();
        activeCamera = Camera.main;
        ColliderPool.GlobalPool = new(colliderPrefab, colliderParent);
    }

    public float BondTolerance
    {
        get => bondTolerance;
        set
        {
            if (Mathf.Approximately(bondTolerance, value))
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
                    else
                    {
                        structureRenderer.RecollectDrawBatches();
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

        structureRenderer.SetStructureView(structureView);
        // Since the new lattice may have different size as the old one, we have to recalculate lattice translations
        structureRenderer.SetLatticeRepeat(repeatA, repeatB, repeatC);
        latticeRenderer.RenderLattice(structureView.Structure, repeatA, repeatB, repeatC);
        RefocusCamera();
    }

    private void RefocusCamera()
    {
        cameraCenter = CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC)) / 2;
        cameraViewportSize = cameraCenter.magnitude + 3.5f;

        UpdateCameraTransform();
    }

    private void UpdateCameraTransform()
    {
        float cosAltitude = Mathf.Cos(cameraAltitude);
        var pos = new Vector3(cosAltitude * Mathf.Sin(cameraAzimuth), Mathf.Sin(cameraAltitude),
            -cosAltitude * Mathf.Cos(cameraAzimuth));
        pos *= (4 * cameraCenter.magnitude);
        pos += cameraCenter;

        var rot = Quaternion.Euler(cameraAltitude * Mathf.Rad2Deg, -cameraAzimuth * Mathf.Rad2Deg, 0);
        activeCamera.transform.SetPositionAndRotation(pos, rot);
        activeCamera.orthographicSize = cameraViewportSize;
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
            modified = this.repeatA != repeatA.Value;
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

        structureRenderer.SetLatticeRepeat(this.repeatA, this.repeatB, this.repeatC);
        latticeRenderer.RenderLattice(CurrentStructureView.Structure, this.repeatA, this.repeatB, this.repeatC);
        RefocusCamera();
    }

    void MoveAtoms(IEnumerable<AtomView> atoms, Vector3 translation)
    {
        foreach (var item in atoms)
        {
            item.SetPosition(item.Position + translation, CurrentStructureView.Structure);
        }
    }

    // Update is called once per frame
    void Update()
    {
        List<AtomView> atomsHovered = new();

        var raycasts = Physics.RaycastAll(activeCamera.ScreenPointToRay(Input.mousePosition));
        foreach (var hitInfo in raycasts)
        {
            if (hitInfo.collider.gameObject.TryGetComponent<ColliderScript>(out var colliderScript))
            {
                atomsHovered.Add(colliderScript.Atom);
                structureRenderer.RenderSingleAtom(colliderScript.Atom.RenderMat, colliderScript.Atom.Position,
                    colliderScript.Atom.Radius + 0.5f);
            }
        }

        HandleMouseEvents(atomsHovered);
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

    void HandleMouseEvents(List<AtomView> raycastAtom)
    {
        AtomView clickedAtom = raycastAtom.Count > 0 ? raycastAtom[0] : null;
        bool cameraRotated = false, cameraZoomed = Mathf.Abs(Input.mouseScrollDelta.y) > 1e-5, cameraMoved = false;
        bool shallMoveAtom = false, rendererShallUpdate = false;
        var mouseWorldPos = activeCamera.ScreenToWorldPoint(Input.mousePosition);

        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (Input.GetKeyUp(KeyCode.Mouse0) && currentTool == ToolType.SELECT)
        {
            if (raycastAtom.Count > 0)
            {
                if (selectionManager.IsSelected(clickedAtom))
                {
                    if (shiftDown)
                    {
                        selectionManager.Deselect(clickedAtom);
                    }
                    // Do nothing if the same atom is selected again without Shift pressed
                }
                else
                {
                    // Replace selection if Shift not pressed
                    if (!shiftDown)
                    {
                        selectionManager.ClearSelection();
                    }

                    selectionManager.Select(clickedAtom);
                }

                rendererShallUpdate = true;
            }
            else if (!shiftDown && !altDown)
            {
                if (selectionManager.ClearSelection())
                {
                    rendererShallUpdate = true;
                }
            }
        }

        if (altDown)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                switch (currentTool)
                {
                    case ToolType.RENAME_ATOM:
                    case ToolType.ROTATE_VIEW:
                    case ToolType.MOVE_VIEW:
                    case ToolType.SELECT:
                        currentTool = ToolType.MOVE_ATOM;
                        break;
                    case ToolType.MOVE_ATOM:
                        shallMoveAtom = true;
                        break;
                }
            }
            else
            {
                if (currentTool == ToolType.MOVE_ATOM)
                {
                    shallMoveAtom = true;
                    currentTool = ToolType.SELECT;
                }
            }
        }
        else
        {
            // Viewport op
            if (Input.GetKey(KeyCode.Mouse1))
            {
                switch (currentTool)
                {
                    case ToolType.RENAME_ATOM:
                    case ToolType.MOVE_ATOM:
                    case ToolType.MOVE_VIEW:
                    case ToolType.SELECT:
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
                    currentTool = ToolType.SELECT;
                }
            }

            if (Input.GetKey(KeyCode.Mouse2))
            {
                switch (currentTool)
                {
                    case ToolType.RENAME_ATOM:
                    case ToolType.MOVE_ATOM:
                    case ToolType.ROTATE_VIEW:
                    case ToolType.SELECT:
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
                    currentTool = ToolType.SELECT;
                }
            }

            if (cameraRotated || cameraMoved || cameraZoomed)
            {
                float? deltaAlt = cameraRotated ? Input.mousePositionDelta.y * -0.01f : null;
                float? deltaAzi = cameraRotated ? Input.mousePositionDelta.x * -0.01f : null;
                float? deltaViewportSize = cameraZoomed ? Input.mouseScrollDelta.y * -5 : null;

                Vector3? deltaCenter =
                    cameraMoved ? (activeCamera.ScreenToWorldPoint(prevMousePos) - mouseWorldPos) : null;
                UpdateCamera(deltaAlt, deltaAzi, deltaViewportSize, deltaCenter);
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            var time = Time.time;
            if (clickedAtom != null && clickedAtom == lastClickedAtom && time - lastClickTime < DOUBLE_CLICK_THRESHOLD)
            {
                // Handle double click
                Debug.Log($"Double click on {clickedAtom.Symbol}");
                currentTool = ToolType.RENAME_ATOM;
            }

            lastClickTime = time;
            lastClickedAtom = clickedAtom;
        }

        if (shallMoveAtom)
        {
            var translation = mouseWorldPos - activeCamera.ScreenToWorldPoint(prevMousePos);

            // If we are dragging one atom, then we don't want to switch to another one when hovering on another atom
            if (raycastAtom.Count > 0 && (selectionManager.IsEmpty || (selectionManager.Count == 1 && !isDraggingAtom)))
            {
                // If one or none atoms are selected, then the atom at cursor is moved
                selectionManager.ReplaceSelection(clickedAtom);
            }

            MoveAtoms(selectionManager.Selection, translation);
            if (updateBondsRealtime)
            {
                CurrentStructureView.BondMutex.WaitOne();
                foreach (var item in selectionManager.Selection)
                {
                    CurrentStructureView.UpdateAtomBonds(item, bondTolerance, true);
                }

                CurrentStructureView.BondMutex.ReleaseMutex();
            }

            isDraggingAtom = true;
            rendererShallUpdate = true;
        }
        else
        {
            // Defer bond recalculation to the time when the mouse is released
            if (isDraggingAtom && !updateBondsRealtime)
            {
                CurrentStructureView.BondMutex.WaitOne();
                foreach (var item in selectionManager.Selection)
                {
                    CurrentStructureView.UpdateAtomBonds(item, bondTolerance, true);
                }

                CurrentStructureView.BondMutex.ReleaseMutex();
                rendererShallUpdate = true;
            }

            isDraggingAtom = false;
        }

        if (rendererShallUpdate)
        {
            structureRenderer.RecollectDrawBatches(true);
        }

        prevMousePos = Input.mousePosition;
    }

    private void OnGUI()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

        if (currentTool != ToolType.RENAME_ATOM)
        {
            for (int i = 0; i < selectionManager.Count; i++)
            {
                var posScreen = activeCamera.WorldToScreenPoint(selectionManager.Selection[i].Position);
                posScreen.y = Screen.height - posScreen.y;
                if (!Screen.safeArea.Contains(new Vector2(posScreen.x, posScreen.y)))
                {
                    continue;
                }

                GUI.Label(new Rect(posScreen.x - 10, posScreen.y - 10, 100, 100), $"[{i}]", labelStyle);
            }
        }

        if (currentTool == ToolType.RENAME_ATOM && lastClickedAtom != null)
        {
            var posScreen = activeCamera.WorldToScreenPoint(lastClickedAtom.Position);
            posScreen.y = Screen.height - posScreen.y;

            GUI.TextField(new Rect(posScreen.x - 40, posScreen.y - 15, 80, 30), lastClickedAtom.Symbol);
        }
    }
}