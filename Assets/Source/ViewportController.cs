using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

internal enum ToolType
{
    Select,
    BoxSelect,
    MoveView,
    RotateView,
    MoveAtom,
    RotateAtom
}

public class ViewportController : MonoBehaviour
{
    const float BOX_SELECT_THRESHOLD = 0.2f;
    const float PAN_SPEED = 1f;

    public GameObject colliderPrefab;
    public GameObject colliderParent;

    [NonSerialized] public StructureRenderer structureRenderer;
    [NonSerialized] public LatticeRenderer latticeRenderer;
    [NonSerialized] public SelectionManager selectionManager = null;
    [NonSerialized] public UndoManager undoManager = null;
    [NonSerialized] private Camera activeCamera;
    [NonSerialized] private SelectionBoxDrawer selectionBoxDrawer;

    public GUIStyle selectionLabelStyle, coordinateLabelStyle;

    private ScreenSpaceLineRenderer coordinatesRenderer;
    private int atomsLayer, latticeLayer;

    // Whether to update bonds in realtime when moving atoms
    private bool updateBondsRealtime = true;
    private bool bondRecalculationRunning = false, bondDirtyFlag = false;

    private int repeatA = 1, repeatB = 1, repeatC = 1;
    private Vector3 latticeCorner;

    private Vector3 cameraCenter;
    private Vector2 coordinatesOrigin;

    // How far the camera is from the focal center
    private float cameraDistance;
    private float cameraViewportSize = 0f;

    private Vector3 prevMousePos, opStartMousePos;
    private Ray mouseRay;
    private ToolType currentTool = ToolType.Select;

    private Vector3 rotateCenter;
    private bool isMovingAtom = false, isRotatingAtom = false;

    // Record mouse down point to draw selection box
    private Vector3 mouseDownPos;
    private float mouseDownTime = 0f;

    // Position to render the text `a`, `b` and `c`
    private Vector2 xAxisEnd, yAxisEnd, zAxisEnd;

    // Create a temporary selection manager for box selection, will be merged to the main selection when mouse up
    private SelectionManager temporarySelectionManager;

    private bool shallUpdateRenderer = false, shallUpdateLattice = false;

    public ArcStructureView CurrentStructureView { get; private set; } = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        structureRenderer = GetComponent<StructureRenderer>();
        latticeRenderer = GetComponent<LatticeRenderer>();
        activeCamera = Camera.main;
        selectionBoxDrawer = GameObject.Find("SelectionBoxContainer").GetComponent<SelectionBoxDrawer>();
        ColliderPool.GlobalPool = new(colliderPrefab, colliderParent);

        atomsLayer = LayerMask.NameToLayer("Default");
        latticeLayer = LayerMask.NameToLayer("TransparentFX");

        coordinatesRenderer = new ScreenSpaceLineRenderer();
        coordinatesOrigin = new(-activeCamera.aspect + 0.2f, -1 + 0.3f);
    }

    public void RecalculateBonds()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

#if UNITY_WEBGL
        CurrentStructureView.GenerateBonds();
        CurrentStructureView.SortAtoms();
        shallUpdateRenderer = true;
#else
        if (bondRecalculationRunning)
        {
            // Hint background task to rerun
            bondDirtyFlag = true;
        }
        else
        {
            bondRecalculationRunning = true;
            bondDirtyFlag = false;
            Task.Run(() =>
            {
                CurrentStructureView.GenerateBonds();

                App.Instance.ScheduleTask(() =>
                {
                    bondRecalculationRunning = false;
                    if (bondDirtyFlag)
                    {
                        RecalculateBonds();
                    }
                    else
                    {
                        shallUpdateRenderer = true;
                    }
                });
            });
        }
#endif
    }

    public void OpenStructureView(ArcStructureView structureView)
    {
        CurrentStructureView?.ReleaseColliders();

        CurrentStructureView = structureView;
        CurrentStructureView.CreateColliders();
        CurrentStructureView.GenerateBonds();

        R_AtomEditPanelSrc.S.SetCurrentStructure(CurrentStructureView.Structure);
        undoManager = new(CurrentStructureView);
        selectionManager = new(R_AtomEditPanelSrc.S.UpdateSelection);

        R_LatticeEditPanelSrc.S.UpdateLattice(structureView.Structure);
        structureRenderer.SetStructureView(structureView);
        // Since the new lattice may have different size as the old one, we have to recalculate lattice translations
        structureRenderer.SetLatticeRepeat(repeatA, repeatB, repeatC);
        latticeRenderer.SetLattice(structureView.Structure, repeatA, repeatB, repeatC);
        latticeCorner = CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC));
    }

    public void SubmitOperation(IOperation op)
    {
        undoManager.SubmitOperation(op);
        OnOperationDone(op);
    }

    public void RefocusCamera()
    {
        cameraCenter = CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC)) / 2;
        cameraViewportSize = cameraCenter.magnitude + 3.5f;
        activeCamera.orthographicSize = cameraViewportSize;
        cameraDistance = 4 * cameraCenter.magnitude;

        UpdateCameraPos();
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
        latticeRenderer.SetLattice(CurrentStructureView.Structure, this.repeatA, this.repeatB, this.repeatC);
        latticeCorner =
            CurrentStructureView.Structure.FractionalToCartesian(new Vector3(this.repeatA, this.repeatB, this.repeatC));
        RefocusCamera();
    }

    void ResizeViewport(float deltaViewportSize)
    {
        cameraViewportSize = Mathf.Clamp(cameraViewportSize + deltaViewportSize, 1f, 10000f);
        activeCamera.orthographicSize = cameraViewportSize;
    }

    void UpdateCameraPos()
    {
        Vector3 translation = activeCamera.transform.worldToLocalMatrix *
                              new Vector4(cameraCenter.x, cameraCenter.y, cameraCenter.z, 1);
        // Do not change depth
        translation.z -= cameraDistance;
        activeCamera.transform.Translate(translation, Space.Self);
    }

    void RotateCamera(Vector3 mousePos)
    {
        if (Physics.Raycast(mouseRay, out _, activeCamera.farClipPlane, 1 << latticeLayer))
        {
            Vector3 deltaMouse = mousePos - prevMousePos;
            float rotateSpeed = 360f / Screen.height * activeCamera.orthographicSize / latticeCorner.magnitude;
            activeCamera.transform.RotateAround(cameraCenter, activeCamera.transform.up, deltaMouse.x * rotateSpeed);
            activeCamera.transform.RotateAround(cameraCenter, activeCamera.transform.right,
                -deltaMouse.y * rotateSpeed);
        }
        else
        {
            Vector3 centerScreen = activeCamera.WorldToScreenPoint(0.5f * latticeCorner);
            Vector3 a = centerScreen - mousePos;
            Vector3 b = centerScreen - prevMousePos;
            float degree = Mathf.Sign(Vector3.Cross(a, b).z) * Vector3.Angle(a, b);
            activeCamera.transform.RotateAround(cameraCenter, activeCamera.transform.forward, degree);
        }
    }

    bool UpdateBoxSelection(Rect currentRect)
    {
        bool selectionChanged = false;
        foreach (var item in CurrentStructureView.Atoms)
        {
            Vector3 pos = activeCamera.WorldToScreenPoint(item.Position);
            bool isSelected = temporarySelectionManager.IsSelected(item);
            bool isInRect = currentRect.Contains(pos);

            if (isInRect && !isSelected)
            {
                temporarySelectionManager.Select(item);
                selectionChanged = true;
                continue;
            }

            if (!isInRect && isSelected)
            {
                temporarySelectionManager.Deselect(item);
                selectionChanged = true;
            }
        }

        return selectionChanged;
    }

    void DrawCoordinates()
    {
        coordinatesRenderer.Clear();

        var xAxis = (Vector2)(activeCamera.transform.InverseTransformDirection(Vector3.right).normalized);
        xAxisEnd = coordinatesOrigin + xAxis * 0.2f;
        coordinatesRenderer.SubmitLine(new LineStructure
        {
            start = coordinatesOrigin,
            end = xAxisEnd,
            color = Color.red,
            thickness = 0.002f
        });

        var yAxis = (Vector2)(activeCamera.transform.InverseTransformDirection(Vector3.up).normalized);
        yAxisEnd = coordinatesOrigin + yAxis * 0.2f;
        coordinatesRenderer.SubmitLine(new LineStructure
        {
            start = coordinatesOrigin,
            end = yAxisEnd,
            color = Color.green,
            thickness = 0.002f
        });

        var zAxis = (Vector2)(activeCamera.transform.InverseTransformDirection(Vector3.forward).normalized);
        zAxisEnd = coordinatesOrigin + zAxis * 0.2f;
        coordinatesRenderer.SubmitLine(new LineStructure
        {
            start = coordinatesOrigin,
            end = zAxisEnd,
            color = Color.cyan,
            thickness = 0.002f
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

        List<AtomView> atomsHovered = new();
        bool raycastOnUI = EventSystem.current.IsPointerOverGameObject();

        if (raycastOnUI)
        {
            // Cancel any ongoing mouse operations
            currentTool = ToolType.Select;

            // Submit operation if mouse has released
            if (isMovingAtom)
            {
                OnMoveAtoms(activeCamera.ScreenToWorldPoint(Input.mousePosition), true);
            }

            if (isRotatingAtom)
            {
                OnRotateAtoms(Input.mousePosition, true);
            }
        }
        else
        {
            mouseRay = activeCamera.ScreenPointToRay(Input.mousePosition);
            var raycasts = Physics.RaycastAll(mouseRay, activeCamera.farClipPlane, 1 << atomsLayer);
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

        HandleKeyboardEvents();
        DrawCoordinates();

        if (shallUpdateLattice)
        {
            structureRenderer.SetLatticeRepeat(repeatA, repeatB, repeatC);
            latticeRenderer.SetLattice(CurrentStructureView.Structure, repeatA, repeatB, repeatC);
            latticeCorner =
                CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC));
            RefocusCamera();
            shallUpdateLattice = false;
        }

        if (shallUpdateRenderer)
        {
            structureRenderer.RecollectDrawBatches(true);
            shallUpdateRenderer = false;
        }
    }

    void OnOperationDone(IOperation op)
    {
        switch (op.Flags)
        {
            case UpdateFlags.UpdateAtom:
                R_AtomEditPanelSrc.S.UpdateSelection(selectionManager.Selection);
                shallUpdateRenderer = true;
                break;
            case UpdateFlags.UpdateLattice:
                R_LatticeEditPanelSrc.S.UpdateLattice(CurrentStructureView.Structure);
                // Update display properties since they may have changed due to lattice reform
                R_AtomEditPanelSrc.S.DisplayActiveAtomProperties();
                RecalculateBonds();
                shallUpdateRenderer = true;
                shallUpdateLattice = true;
                break;
        }
    }

    void HandleKeyboardEvents()
    {
        var move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
        if (move.magnitude > 1e-5)
        {
            var deltaCenter = activeCamera.transform.TransformDirection(PAN_SPEED * move);
            cameraCenter += deltaCenter;
            UpdateCameraPos();
        }

        bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrlDown)
        {
            IOperation op = null;

            if (Input.GetKeyUp(KeyCode.Z))
            {
                // Undo
                ConFun.UpdateStatusLabel("Undo!");
                op = undoManager.Undo();
            }
            else if (Input.GetKeyUp(KeyCode.Y))
            {
                ConFun.UpdateStatusLabel("Redo!");
                op = undoManager.Redo();
            }

            if (op != null)
            {
                OnOperationDone(op);
            }
        }
    }

    void OnMoveAtoms(Vector3 mouseWorldPos, bool submit = false)
    {
        var dragTotalTranslation = mouseWorldPos - activeCamera.ScreenToWorldPoint(opStartMousePos);
        if (submit)
        {
            // Submit move

            // Careful with assignment behavior: we have to copy assign the selection here! 
            IOperation op = new TranslateAtomsOperation(new(selectionManager.Selection),
                dragTotalTranslation);
            SubmitOperation(op);

            isMovingAtom = false;
        }
        else
        {
            // Preview, move AtomView only
            foreach (var item in selectionManager.Selection)
            {
                CurrentStructureView.SetAtomPosition(item, item.SourceAtom.Coordinates + dragTotalTranslation, true);
            }
        }
    }

    void OnRotateAtoms(Vector3 mousePos, bool submit = false)
    {
        if (submit)
        {
            var newPositions = selectionManager.Selection.Select(x => x.Position).ToList();
            // Careful with assignment behavior: we have to copy assign the selection here! 
            IOperation op = new RepositionAtomsOperation(new(selectionManager.Selection), newPositions);
            SubmitOperation(op);

            isRotatingAtom = false;
        }
        else
        {
            // Preview, move AtomView only

            // Area for drag rotation
            Vector3 boxExtend = new(200, 200, 0);
            Vector3 boxCenter = activeCamera.WorldToScreenPoint(rotateCenter);

            Rect rect = Rect.MinMaxRect(boxCenter.x - boxExtend.x, boxCenter.y - boxExtend.y, boxCenter.x + boxExtend.x,
                boxCenter.y + boxExtend.y);

            bool rotationIsValid = true;
            List<Vector3> newPositions = new List<Vector3>(selectionManager.Selection.Count);

            if (rect.Contains(mousePos))
            {
                Vector3 deltaMouse = mousePos - prevMousePos;
                float rotateSpeed = 100f / Screen.height * activeCamera.orthographicSize;
                
                // Vertical rotation
                Quaternion rot = Quaternion.AngleAxis(deltaMouse.y * rotateSpeed, activeCamera.transform.right);
                // Horizontal rotation
                rot *= Quaternion.AngleAxis(-deltaMouse.x * rotateSpeed, activeCamera.transform.up);

                foreach (var item in selectionManager.Selection)
                {
                    var relPos = item.Position - rotateCenter;
                    newPositions.Add(rot * relPos + rotateCenter);

                    if (!CurrentStructureView.IsPositionInLattice(newPositions[^1]))
                    {
                        rotationIsValid = false;
                        break;
                    }
                }
            }
            else
            {
                Vector3 centerScreen = activeCamera.WorldToScreenPoint(rotateCenter);
                Vector3 a = centerScreen - mousePos;
                Vector3 b = centerScreen - prevMousePos;
                float degree = Mathf.Sign(Vector3.Cross(a, b).z) * Vector3.Angle(a, b);
                Quaternion rot = Quaternion.AngleAxis(-degree, activeCamera.transform.forward);

                foreach (var item in selectionManager.Selection)
                {
                    var relPos = item.Position - rotateCenter;
                    newPositions.Add(rot * relPos + rotateCenter);

                    if (!CurrentStructureView.IsPositionInLattice(newPositions[^1]))
                    {
                        rotationIsValid = false;
                        break;
                    }
                }
            }

            // Prevent some of the atoms moved out of the lattice
            if (rotationIsValid)
            {
                for (var i = 0; i < selectionManager.Selection.Count; i++)
                {
                    CurrentStructureView.SetAtomPosition(selectionManager.Selection[i], newPositions[i], true);
                }
            }
        }
    }

    void HandleMouseEvents(List<AtomView> raycastAtom)
    {
        AtomView clickedAtom = raycastAtom.Count > 0 ? raycastAtom[0] : null;
        bool cameraRotated = false, cameraZoomed = Mathf.Abs(Input.mouseScrollDelta.y) > 1e-5, cameraMoved = false;
        bool shallMoveAtom = false, shallRotateAtom = false;
        var mouseWorldPos = activeCamera.ScreenToWorldPoint(Input.mousePosition);

        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            mouseDownTime = Time.time;
            mouseDownPos = Input.mousePosition;
        }

        if (Input.GetKeyUp(KeyCode.Mouse0) && currentTool == ToolType.Select)
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

                shallUpdateRenderer = true;
            }
            else if (!shiftDown && !altDown)
            {
                if (selectionManager.ClearSelection())
                {
                    shallUpdateRenderer = true;
                }
            }
        }

        if (altDown)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                currentTool = ToolType.MoveAtom;
                shallMoveAtom = true;
            }
            else
            {
                if (currentTool == ToolType.MoveAtom)
                {
                    shallMoveAtom = true;
                    currentTool = ToolType.Select;
                }
            }

            if (Input.GetKey(KeyCode.Mouse1) && !selectionManager.IsEmpty)
            {
                currentTool = ToolType.RotateAtom;
                shallRotateAtom = true;
            }
            else
            {
                if (currentTool == ToolType.RotateAtom)
                {
                    shallRotateAtom = true;
                    currentTool = ToolType.Select;
                }
            }
        }
        else
        {
            // Viewport op
            if (Input.GetKey(KeyCode.Mouse1))
            {
                currentTool = ToolType.RotateView;
                cameraRotated = true;
            }
            else
            {
                if (currentTool == ToolType.RotateView)
                {
                    cameraRotated = true;
                    currentTool = ToolType.Select;
                }
            }

            if (Input.GetKey(KeyCode.Mouse2))
            {
                currentTool = ToolType.MoveView;
                cameraMoved = true;
            }
            else
            {
                if (currentTool == ToolType.MoveView)
                {
                    cameraMoved = true;
                    currentTool = ToolType.Select;
                }
            }
        }

        if (cameraMoved)
        {
            cameraCenter += (activeCamera.ScreenToWorldPoint(prevMousePos) - mouseWorldPos);
            UpdateCameraPos();
        }

        if (cameraZoomed)
        {
            ResizeViewport(Input.mouseScrollDelta.y * -2);
        }

        if (cameraRotated)
        {
            RotateCamera(Input.mousePosition);
        }

        if (currentTool == ToolType.BoxSelect)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                var mousePos = Input.mousePosition;
                // Handle box select
                var (minX, maxX) = mousePos.x > mouseDownPos.x
                    ? (mouseDownPos.x, mousePos.x)
                    : (mousePos.x, mouseDownPos.x);
                var (minY, maxY) = mousePos.y > mouseDownPos.y
                    ? (mouseDownPos.y, mousePos.y)
                    : (mousePos.y, mouseDownPos.y);
                Rect boxRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
                selectionBoxDrawer.DrawBox(boxRect);
                shallUpdateRenderer = shallUpdateRenderer || UpdateBoxSelection(boxRect);
            }
            else
            {
                // End box select
                selectionBoxDrawer.Hide();
                currentTool = ToolType.Select;

                selectionManager.MergeSelection(temporarySelectionManager);
                temporarySelectionManager = null;
            }
        }
        else if (Input.GetKey(KeyCode.Mouse0) && !isMovingAtom && Time.time - mouseDownTime > BOX_SELECT_THRESHOLD)
        {
            selectionBoxDrawer.Display();
            Rect boxRect = Rect.MinMaxRect(mouseDownPos.x, mouseDownPos.y, mouseDownPos.x, mouseDownPos.y);
            selectionBoxDrawer.DrawBox(boxRect);
            currentTool = ToolType.BoxSelect;

            // Clear the current selection if shift down
            if (!shiftDown)
            {
                selectionManager.ClearSelection();
            }

            temporarySelectionManager = new SelectionManager();
        }

        if (shallMoveAtom || shallRotateAtom)
        {
            // If we are dragging one atom, then we don't want to switch to another one when hovering on another atom
            if (raycastAtom.Count > 0 && (selectionManager.IsEmpty || (selectionManager.Count == 1 && !isMovingAtom)))
            {
                // If one or none atoms are selected, then the atom at cursor is moved
                selectionManager.ReplaceSelection(clickedAtom);
            }

            if (shallMoveAtom)
            {
                if (!isMovingAtom)
                {
                    opStartMousePos = Input.mousePosition;
                    isMovingAtom = true;
                }

                OnMoveAtoms(mouseWorldPos);
            }
            else if (shallRotateAtom)
            {
                if (!isRotatingAtom)
                {
                    opStartMousePos = Input.mousePosition;
                    isRotatingAtom = true;

                    rotateCenter = Vector3.zero;
                    foreach (var item in selectionManager.Selection)
                    {
                        rotateCenter += item.Position;
                    }

                    rotateCenter /= selectionManager.Selection.Count;
                }

                OnRotateAtoms(Input.mousePosition);
            }

            if (updateBondsRealtime)
            {
#if !UNITY_WEBGL
                CurrentStructureView.BondMutex.WaitOne();
#endif
                foreach (var item in selectionManager.Selection)
                {
                    CurrentStructureView.UpdateAtomBondsNoLock(item, App.Instance.BondTolerance, true);
                }
#if !UNITY_WEBGL
                CurrentStructureView.BondMutex.ReleaseMutex();
#endif
            }

            shallUpdateRenderer = true;
        }
        else
        {
            // Submit operation if mouse has released
            if (isMovingAtom)
            {
                OnMoveAtoms(mouseWorldPos, true);
            }
            else if (isRotatingAtom)
            {
                OnRotateAtoms(Input.mousePosition, true);
            }
        }

        if (Input.GetKeyUp(KeyCode.Delete) && !selectionManager.IsEmpty)
        {
            IOperation op = new RemoveAtomsOperation(new(selectionManager.Selection));
            selectionManager.ClearSelection();
            SubmitOperation(op);
        }

        prevMousePos = Input.mousePosition;
    }

    Vector2 GLCoordsToGUICoords(Vector2 glCoords)
    {
        return new Vector2((glCoords.x + activeCamera.aspect) / (2 * activeCamera.aspect) * Screen.width,
            (1f - (glCoords.y + 1f) / 2f) * Screen.height);
    }

    private void OnGUI()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

        for (int i = 0; i < selectionManager.Count; i++)
        {
            var posScreen = activeCamera.WorldToScreenPoint(selectionManager.Selection[i].Position);
            posScreen.y = Screen.height - posScreen.y;
            if (!Screen.safeArea.Contains(new Vector2(posScreen.x, posScreen.y)))
            {
                continue;
            }

            GUI.Label(new Rect(posScreen.x - 15, posScreen.y - 15, 100, 100),
                $"[{selectionManager.Selection[i].Symbol}]", selectionLabelStyle);
        }

        // Draw coordinates text
        Vector2 offset = new(10, -10), size = new(100, 100);
        coordinateLabelStyle.normal.textColor = Color.red;
        GUI.Label(new Rect(GLCoordsToGUICoords(xAxisEnd) + offset, size), "x", coordinateLabelStyle);
        coordinateLabelStyle.normal.textColor = Color.green;
        GUI.Label(new Rect(GLCoordsToGUICoords(yAxisEnd) + offset, size), "y", coordinateLabelStyle);
        coordinateLabelStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(GLCoordsToGUICoords(zAxisEnd) + offset, size), "z", coordinateLabelStyle);
    }
}