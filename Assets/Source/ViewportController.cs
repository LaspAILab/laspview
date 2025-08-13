using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RuntimeSceneGizmo;
using UnityEngine;
using UnityEngine.EventSystems;
using B83.Win32;
using Random = System.Random;

public enum DisplayStyleType
{
    BallsAndSticks,
    OnlyBalls,
    OnlySticks
}

public enum CrossCellBondPolicy
{
    None,
    Adjacent,
    Diagonal,
    BodyDiagonal,
}

public enum ToolType
{
    Select,
    BoxSelect,
    MoveView,
    RotateView,
    MoveAtom,
    RotateAtom,
    ReplaceAtom
}

public class ViewportController : MonoBehaviour
{
    const float BOX_SELECT_THRESHOLD = 0.2f;
    const float PAN_SPEED = 1f;

    [NonSerialized] public StructureRenderer structureRenderer;
    [NonSerialized] public HBondRenderer hBondRenderer;
    [NonSerialized] public LatticeRenderer latticeRenderer;
    [NonSerialized] public SelectionManager selectionManager = null;
    [NonSerialized] public ColliderManager colliderManager;
    [NonSerialized] public UndoManager undoManager = null;
    [NonSerialized] private Camera activeCamera;
    [NonSerialized] private SelectionBoxDrawer selectionBoxDrawer;

    public GUIStyle selectionLabelStyle;

    private Random randomGenerator = new();

    // Workaround for MacOS command issue
    // All keys pressed while command is held will be retained even if the key has already released, 
    // so we have to avoid responding to a hotkey too many times
    private List<KeyCode> ignoreKeyCodes = new();

    private int atomsLayer, latticeLayer;

    // Whether to update bonds in realtime when moving atoms
    private bool updateBondsRealtime = true;
#if !UNITY_WEBGL
    private bool bondRecalculationRunning = false, bondDirtyFlag = false;
#endif

    private int repeatA = 1, repeatB = 1, repeatC = 1;
    private Vector3 latticeCorner;
    private Vector3 cameraCenter;

    // How far the camera is from the focal center
    private float cameraDistance;
    private float cameraViewportSize = 0f;

    private Vector3 pasteOffset;

    private Vector3 prevMousePos, opStartMousePos;
    private Ray mouseRay;
    private ToolType currentTool = ToolType.Select;

    private Vector3 rotateCenter;

    // Position of selected atoms relative to the rotational center
    private Vector3[] rotationalRelativePos;
    private SelectionItem[] rotatedAtoms;

    private bool isMovingAtom = false, isRotatingAtom = false;

    // Record mouse down point to draw selection box
    private Vector3 mouseDownPos;
    private float mouseDownTime = 0f;

    // Create a temporary selection manager for box selection, will be merged to the main selection when mouse up
    private SelectionManager temporarySelectionManager;

    private bool shallUpdateRenderer = false, shallUpdateLattice = false;
    private List<Atom> clipboard = new();

    private Matrix4x4[,,] latticeTransforms = null;

    public Matrix4x4[,,] LatticeTransforms
    {
        get
        {
            if (latticeTransforms == null)
            {
                UpdateLatticeTransformMatrix();
            }

            return latticeTransforms;
        }
    }

    public CrystalStructureView CurrentStructureView { get; private set; } = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        structureRenderer = GetComponent<StructureRenderer>();
        hBondRenderer = GetComponent<HBondRenderer>();
        latticeRenderer = GetComponent<LatticeRenderer>();
        colliderManager = GetComponent<ColliderManager>();
        activeCamera = Camera.main;
        selectionBoxDrawer = GameObject.Find("SelectionBoxContainer").GetComponent<SelectionBoxDrawer>();

        atomsLayer = LayerMask.NameToLayer("Default");
        latticeLayer = LayerMask.NameToLayer("TransparentFX");
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
    void OnEnable ()
    {
        // must be installed on the main thread to get the right thread id.
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }

    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
    }
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
    void OnFiles(List<string> aFiles, POINT aPos)
    {
        // do something with the dropped file names. aPos will contain the 
        // mouse position within the window where the files has been dropped.
        OverlayUiController.Instance.PrintStatusLog("Dropped files: " + string.Join(" ", aFiles));
        FilePanelController.FilesRead(aFiles.ToArray());
    }
#endif

    public void RecalculateBonds()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

#if UNITY_WEBGL
        CurrentStructureView.GenerateBonds();
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

    public void OpenStructureView(CrystalStructureView structureView)
    {
        CurrentStructureView = structureView;
        // Update UI
        OverlayUiController.Instance.UpdateStructureInfo(CurrentStructureView.Structure);
        EditPanelController.Instance.UpdateLattice(CurrentStructureView.Structure);
        CurrentStructureView.GenerateBonds();

        undoManager = new(CurrentStructureView);
        selectionManager = new(OverlayUiController.Instance.UpdateSelectionInfo);

        latticeTransforms = null;
        colliderManager.SetLatticeTransforms(LatticeTransforms);
        colliderManager.CreateCollidersForStructure(CurrentStructureView);

        structureRenderer.SetStructureView(structureView, LatticeTransforms);
        latticeRenderer.SetLattice(structureView.Structure, repeatA, repeatB, repeatC);
        latticeCorner = CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC));
        hBondRenderer.SetStructure(CurrentStructureView, LatticeTransforms);
    }

    public void SubmitOperation(IOperation op)
    {
        undoManager.SubmitOperation(op);
        OnOperationDone(op);
    }

    public void UpdateLatticeTransformMatrix()
    {
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

        latticeTransforms = null;
        structureRenderer.SetLatticeTransforms(LatticeTransforms);
        colliderManager.SetLatticeTransforms(LatticeTransforms);
        hBondRenderer.OnLatticeRepeatChange(LatticeTransforms);
        latticeRenderer.SetLattice(CurrentStructureView.Structure, this.repeatA, this.repeatB, this.repeatC);
        latticeCorner =
            CurrentStructureView.Structure.FractionalToCartesian(new Vector3(this.repeatA, this.repeatB, this.repeatC));

        // Check if selected atoms are still visible, if not, change the selection to the original atom
        var currentSelection = selectionManager.Selection.ToList();
        foreach (var item in currentSelection)
        {
            // Is the selected atom out of the current bounds?
            if (item.Position.x > latticeCorner.x || item.Position.y > latticeCorner.y ||
                item.Position.z > latticeCorner.z)
            {
                // Transfer selection to the root version of the atom
                selectionManager.Selection.Remove(item);
                selectionManager.Selection.Add(new SelectionItem { Atom = item.Atom, LatticeOffset = Vector3.zero, CellIndex = (0, 0, 0) });
            }
        }

        RefocusCamera();
    }

    public void RefocusCamera()
    {
        cameraCenter = CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC)) / 2;
        cameraViewportSize = cameraCenter.magnitude + 3.5f;
        activeCamera.orthographicSize = cameraViewportSize;
        cameraDistance = 4 * cameraCenter.magnitude;

        UpdateCameraPos();
    }

    public void SetCameraViewport(GizmoComponent direction)
    {
        Vector3 forward = Vector3.forward, up = Vector3.up;
        switch (direction)
        {
            case GizmoComponent.None:
            case GizmoComponent.Center:
                break;
            case GizmoComponent.XPositive:
                forward = Vector3.left;
                break;
            case GizmoComponent.XNegative:
                forward = Vector3.right;
                break;
            case GizmoComponent.YPositive:
                forward = Vector3.down;
                up = Vector3.forward;
                break;
            case GizmoComponent.YNegative:
                forward = Vector3.up;
                up = Vector3.back;
                break;
            case GizmoComponent.ZPositive:
                forward = Vector3.back;
                break;
            case GizmoComponent.ZNegative:
                forward = Vector3.up;
                break;
        }

        activeCamera.transform.localRotation = Quaternion.LookRotation(forward, up);
        UpdateCameraPos();
    }

    public void BeginReplaceAtom()
    {
        ReplaceCurrentTool(ToolType.ReplaceAtom);
    }

    public void EndReplaceAtom()
    {
        if (currentTool == ToolType.ReplaceAtom)
        {
            ReplaceCurrentTool(ToolType.Select);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ResizeViewport(float deltaViewportSize)
    {
        cameraViewportSize = Mathf.Clamp(cameraViewportSize + deltaViewportSize, 1f, 10000f);
        activeCamera.orthographicSize = cameraViewportSize;
    }

    // Call after changing the size or rotation of the camera, 
    // this function will adjust the position of camera to look at the given center
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        foreach (var item in colliderManager.colliderRegistry)
        {
            foreach (var col in item.Value)
            {
                Vector3 pos = activeCamera.WorldToScreenPoint(col.ActualPosition);
                bool isSelected = temporarySelectionManager.IsSelected(item.Key, col.CellIndex);
                if (currentRect.Contains(pos))
                {
                    if (!isSelected)
                    {
                        temporarySelectionManager.Select(SelectionItem.CreateFromCollider(col));
                        selectionChanged = true;
                    }
                }
                else
                {
                    if (isSelected)
                    {
                        temporarySelectionManager.Deselect(item.Key, col.CellIndex);
                        selectionChanged = true;
                    }
                }
            }
        }

        return selectionChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

        List<SelectionItem> raycastResult = new();
        bool raycastOnUI = EventSystem.current.IsPointerOverGameObject();

        if (raycastOnUI)
        {
            if (currentTool != ToolType.ReplaceAtom)
            {
                // Cancel any ongoing mouse operations
                ReplaceCurrentTool(ToolType.Select);
            }
            
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
                    raycastResult.Add(SelectionItem.CreateFromCollider(colliderScript));
                    structureRenderer.RenderSingleAtom(colliderScript.Atom, colliderScript.ActualPosition,
                        colliderScript.CellIndex, colliderScript.Atom.Radius + 0.5f);
                }
            }

            HandleMouseEvents(raycastResult);
        }

        var currentSelectedElement = EventSystem.current.currentSelectedGameObject;
        bool isKeyboardCapturedByUI = currentSelectedElement != null &&
                                      currentSelectedElement.GetComponent<RectTransform>() != null;
        // Ignore keyboard events if a UI element is focused
        if (!isKeyboardCapturedByUI)
        {
            HandleKeyboardEvents();
        }

        if (shallUpdateLattice)
        {
            latticeTransforms = null;
            structureRenderer.SetLatticeTransforms(LatticeTransforms);
            hBondRenderer.SetStructure(CurrentStructureView, LatticeTransforms);
            colliderManager.SetLatticeTransforms(LatticeTransforms);
            latticeRenderer.SetLattice(CurrentStructureView.Structure, repeatA, repeatB, repeatC);
            latticeCorner =
                CurrentStructureView.Structure.FractionalToCartesian(new Vector3(repeatA, repeatB, repeatC));
            RefocusCamera();
            shallUpdateLattice = false;
        }

        if (shallUpdateRenderer)
        {
            structureRenderer.RecollectDrawBatches();
            shallUpdateRenderer = false;
        }
    }

    void OnOperationDone(IOperation op)
    {
        selectionManager.RemoveInvalid();

        switch (op.Flags)
        {
            case UpdateFlags.UpdateAtom:
                OverlayUiController.Instance.UpdateStructureInfo(CurrentStructureView.Structure);
                OverlayUiController.Instance.UpdateSelectionInfo(selectionManager.Selection.ToList());
                EditPanelController.Instance.UpdateActiveAtom();
                shallUpdateRenderer = true;
                break;
            case UpdateFlags.UpdateLattice:
                EditPanelController.Instance.UpdateLattice(CurrentStructureView.Structure);
                OverlayUiController.Instance.UpdateStructureInfo(CurrentStructureView.Structure);
                // Update display properties since they may have changed due to lattice reform
                EditPanelController.Instance.UpdateActiveAtom();
                RecalculateBonds();
                shallUpdateRenderer = true;
                shallUpdateLattice = true;
                break;
        }
    }

    void OnMoveAtoms(Vector3 mouseWorldPos, bool submit = false)
    {
        var dragTotalTranslation = mouseWorldPos - activeCamera.ScreenToWorldPoint(opStartMousePos);
        if (submit)
        {
            // Submit move

            // Careful with assignment behavior: we have to copy assign the selection here! 
            IOperation op = new TranslateAtomsOperation(selectionManager.SelectedAtoms,
                dragTotalTranslation);
            SubmitOperation(op);

            isMovingAtom = false;
        }
        else
        {
            // Preview, move AtomView only
            foreach (var item in selectionManager.Selection)
            {
                CurrentStructureView.SetAtomPosition(item.Atom, item.Atom.SourceAtom.Coordinates + dragTotalTranslation,
                    true);
            }
        }
    }

    void OnRotateAtoms(Vector3 mousePos, bool submit = false)
    {
        if (submit)
        {
            var newPositions = rotationalRelativePos.Select(x => rotateCenter + x).ToList();
            // Careful with assignment behavior: we have to copy assign the selection here! 
            IOperation op = new RepositionAtomsOperation(rotatedAtoms.Select(x => x.Atom).ToList(), newPositions);
            SubmitOperation(op);

            isRotatingAtom = false;
        }
        else
        {
            // Area for drag rotation
            Vector3 boxExtend = new(200, 200, 0);
            Vector3 boxCenter = activeCamera.WorldToScreenPoint(rotateCenter);
            Rect rect = Rect.MinMaxRect(boxCenter.x - boxExtend.x, boxCenter.y - boxExtend.y, boxCenter.x + boxExtend.x,
                boxCenter.y + boxExtend.y);

            Quaternion rot;
            if (rect.Contains(mousePos))
            {
                Vector3 deltaMouse = mousePos - prevMousePos;
                float rotateSpeed = 100f / Screen.height * activeCamera.orthographicSize;

                // Vertical rotation
                rot = Quaternion.AngleAxis(deltaMouse.y * rotateSpeed, activeCamera.transform.right);
                // Horizontal rotation
                rot *= Quaternion.AngleAxis(-deltaMouse.x * rotateSpeed, activeCamera.transform.up);
            }
            else
            {
                Vector3 centerScreen = activeCamera.WorldToScreenPoint(rotateCenter);
                Vector3 a = centerScreen - mousePos;
                Vector3 b = centerScreen - prevMousePos;
                float degree = Mathf.Sign(Vector3.Cross(a, b).z) * Vector3.Angle(a, b);
                rot = Quaternion.AngleAxis(-degree, activeCamera.transform.forward);
            }

            for (var i = 0; i < rotationalRelativePos.Length; i++)
            {
                rotationalRelativePos[i] = rot * rotationalRelativePos[i];
                CurrentStructureView.SetAtomPosition(rotatedAtoms[i].Atom, rotateCenter + rotationalRelativePos[i],
                    true);
            }
        }
    }

    Vector3 GetGeometryCenter(IEnumerable<SelectionItem> atoms)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var item in atoms)
        {
            center += item.Position;
            count++;
        }

        return center / count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SaveCurrentSelectionToClipboard()
    {
        // Atoms are added to clipboard by reference, remember to copy them when pasting! 
        clipboard = selectionManager.DistinctSelection.Select(x => x.Atom.SourceAtom).ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReplaceCurrentTool(ToolType newTool)
    {
        switch (currentTool)
        {
            case ToolType.BoxSelect:
                if (temporarySelectionManager != null)
                {
                    temporarySelectionManager.ClearSelection();
                    temporarySelectionManager = null;
                }

                Debug.Log("End box select");
                selectionBoxDrawer.Hide();
                mouseDownTime = -1;
                shallUpdateRenderer = true;
                break;
            case ToolType.ReplaceAtom:
                EditPanelController.Instance.EndReplaceAtom();
                break;
            // Add any tool that may require manual canceling
        }

        currentTool = newTool;
        OverlayUiController.Instance.OnToolChange(newTool);
    }

    void HandleKeyboardEvents()
    {
        // DebugKeyboard();
        var move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);

        if (App.IsRunningOnMacOS)
        {
            move = -move;
        }

        if (move.magnitude > 1e-5)
        {
            var deltaCenter = activeCamera.transform.TransformDirection(PAN_SPEED * move);
            cameraCenter += deltaCenter;
            UpdateCameraPos();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReplaceCurrentTool(ToolType.Select);
        }

        if ((Input.GetKeyUp(KeyCode.Delete) || Input.GetKeyUp(KeyCode.Backspace)) && !selectionManager.IsEmpty)
        {
            IOperation op = new RemoveAtomsOperation(selectionManager.SelectedAtoms);
            selectionManager.ClearSelection();
            SubmitOperation(op);
        }

        bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (App.IsRunningOnMacOS)
        {
            ctrlDown = ctrlDown || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        }

        if (ctrlDown)
        {
            IOperation op = null;

            // WebGL build running on Mac OS cannot report key up/down signals correctly, 
            // This is a workaround
            if (Input.GetKey(KeyCode.Z) && !ignoreKeyCodes.Contains(KeyCode.Z))
            {
                // Undo
                OverlayUiController.Instance.PrintStatusLog("Undo!");
                op = undoManager.Undo();
                selectionManager.RemoveInvalid();
                ignoreKeyCodes.Add(KeyCode.Z);
            }
            else if (Input.GetKey(KeyCode.Y) && !ignoreKeyCodes.Contains(KeyCode.Y))
            {
                OverlayUiController.Instance.PrintStatusLog("Redo!");
                op = undoManager.Redo();
                selectionManager.RemoveInvalid();
                ignoreKeyCodes.Add(KeyCode.Y);
            }
            else if (Input.GetKey(KeyCode.X) && !ignoreKeyCodes.Contains(KeyCode.X))
            {
                OverlayUiController.Instance.PrintStatusLog("Cut!");
                SaveCurrentSelectionToClipboard();
                SubmitOperation(new RemoveAtomsOperation(selectionManager.SelectedAtoms));
                selectionManager.ClearSelection();
                ignoreKeyCodes.Add(KeyCode.X);
            }
            else if (Input.GetKey(KeyCode.C) && !ignoreKeyCodes.Contains(KeyCode.C))
            {
                OverlayUiController.Instance.PrintStatusLog("Copy!");
                SaveCurrentSelectionToClipboard();
                pasteOffset = Vector3.zero;
                selectionManager.ClearSelection();
                ignoreKeyCodes.Add(KeyCode.C);
            }
            else if (Input.GetKey(KeyCode.V) && !ignoreKeyCodes.Contains(KeyCode.V))
            {
                OverlayUiController.Instance.PrintStatusLog("Paste!");
                // Add some noise to avoid pasting to the same position
                float rand = (float)randomGenerator.NextDouble();
                pasteOffset += new Vector3(0.8f + 0.4f * rand, 0.8f + 0.4f * rand, 0);

                // Perform a deep copy of clipboard atoms to avoid strange problems
                var newAtoms = clipboard.Select(x => new Atom
                {
                    // Apply an offset
                    Coordinates = x.Coordinates + pasteOffset,
                    Flags = x.Flags,
                    Symbol = x.Symbol
                }).ToList();

                SubmitOperation(new AddAtomsOperation(newAtoms));
                ignoreKeyCodes.Add(KeyCode.V);
            }

            if (op != null)
            {
                OnOperationDone(op);
            }
        }
        else
        {
            // Clean up keys
            ignoreKeyCodes.Clear();
        }
    }
    
    void HandleMouseEvents(List<SelectionItem> raycastAtom)
    {
        SelectionItem hoveredAtom = raycastAtom.Count > 0 ? raycastAtom[0] : new SelectionItem { Atom = null };
        // Forbid zooming when right key is pressed
        bool cameraRotated = false,
            cameraZoomed = !Input.GetKey(KeyCode.Mouse1) && Mathf.Abs(Input.mouseScrollDelta.y) > 1e-5,
            cameraMoved = false;
        bool shallMoveAtom = false, shallRotateAtom = false;
        var mouseWorldPos = activeCamera.ScreenToWorldPoint(Input.mousePosition);

        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            cameraZoomed = false;
            mouseDownTime = Time.time;
            mouseDownPos = Input.mousePosition;
        }

        if (!Input.GetKey(KeyCode.Mouse1))
        {
            // Reset mouse down time if mouse already released
            mouseDownTime = -1;
        }

        if (Input.GetKeyUp(KeyCode.Mouse1) && currentTool == ToolType.Select)
        {
            if (raycastAtom.Count > 0)
            {
                if (selectionManager.IsSelected(hoveredAtom.Atom, hoveredAtom.CellIndex))
                {
                    if (shiftDown)
                    {
                        selectionManager.Deselect(hoveredAtom.Atom, hoveredAtom.CellIndex);
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

                    selectionManager.Select(hoveredAtom);
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

        if (Input.GetKeyUp(KeyCode.Mouse1) && currentTool == ToolType.ReplaceAtom)
        {
            if (raycastAtom.Count > 0)
            {
                SubmitOperation(new SetElementOperation(hoveredAtom.Atom, EditPanelController.Instance.ReplacedElement));
            }
        }

        if (altDown)
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                ReplaceCurrentTool(ToolType.MoveAtom);
                shallMoveAtom = true;
            }
            else
            {
                if (currentTool == ToolType.MoveAtom)
                {
                    shallMoveAtom = true;
                    ReplaceCurrentTool(ToolType.Select);
                }
            }

            if (Input.GetKey(KeyCode.Mouse0) && !selectionManager.IsEmpty)
            {
                ReplaceCurrentTool(ToolType.RotateAtom);
                shallRotateAtom = true;
            }
            else
            {
                if (currentTool == ToolType.RotateAtom)
                {
                    shallRotateAtom = true;
                    ReplaceCurrentTool(ToolType.Select);
                }
            }
        }
        else
        {
            // Viewport op
            if (Input.GetKey(KeyCode.Mouse0))
            {
                ReplaceCurrentTool(ToolType.RotateView);
                cameraRotated = true;
            }
            else
            {
                if (currentTool == ToolType.RotateView)
                {
                    cameraRotated = true;
                    ReplaceCurrentTool(ToolType.Select);
                }
            }

            if (Input.GetKey(KeyCode.Mouse2))
            {
                ReplaceCurrentTool(ToolType.MoveView);
                cameraMoved = true;
            }
            else
            {
                if (currentTool == ToolType.MoveView)
                {
                    cameraMoved = true;
                    ReplaceCurrentTool(ToolType.Select);
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
            if (Input.GetKey(KeyCode.Mouse1))
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
                selectionManager.MergeSelection(temporarySelectionManager);
                temporarySelectionManager = null;

                ReplaceCurrentTool(ToolType.Select);
            }
        }
        else if (Input.GetKey(KeyCode.Mouse1) && !isMovingAtom && !isRotatingAtom &&
                 currentTool != ToolType.ReplaceAtom && mouseDownTime > 0 &&
                 Time.time - mouseDownTime > BOX_SELECT_THRESHOLD)
        {
            mouseDownTime = Time.time;
            Debug.Log("Begin box select");
            selectionBoxDrawer.Display();
            Rect boxRect = Rect.MinMaxRect(mouseDownPos.x, mouseDownPos.y, mouseDownPos.x, mouseDownPos.y);
            selectionBoxDrawer.DrawBox(boxRect);
            ReplaceCurrentTool(ToolType.BoxSelect);

            // Clear the current selection if shift down
            if (!shiftDown)
            {
                selectionManager.ClearSelection();
                shallUpdateRenderer = true;
            }

            temporarySelectionManager = new SelectionManager();
        }

        if (shallMoveAtom || shallRotateAtom)
        {
            // If we are dragging one atom, then we don't want to switch to another one when hovering on another atom
            if (raycastAtom.Count > 0 && (selectionManager.IsEmpty || (selectionManager.Count == 1 && !isMovingAtom)))
            {
                // If one or none atoms are selected, then the atom at cursor is moved
                selectionManager.ReplaceSelection(hoveredAtom);
                shallUpdateRenderer = true;
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
            else
            {
                if (!isRotatingAtom)
                {
                    opStartMousePos = Input.mousePosition;
                    isRotatingAtom = true;

                    // Acquire a distinct list of atoms selected
                    rotatedAtoms = selectionManager.DistinctSelection.ToArray();
                    rotationalRelativePos = new Vector3[rotatedAtoms.Length];

                    rotateCenter = GetGeometryCenter(rotatedAtoms);

                    for (var i = 0; i < rotatedAtoms.Length; i++)
                    {
                        rotationalRelativePos[i] = rotatedAtoms[i].Position - rotateCenter;
                    }
                }

                OnRotateAtoms(Input.mousePosition);
            }

            if (updateBondsRealtime)
            {
#if !UNITY_WEBGL
                CurrentStructureView.BondMutex.WaitOne();
#endif
                foreach (var item in selectionManager.DistinctSelection)
                {
                    CurrentStructureView.UpdateAtomBondsNoLock(item.Atom, App.Instance.BondTolerance, true);
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

        prevMousePos = Input.mousePosition;
    }

    private void OnGUI()
    {
        if (CurrentStructureView == null)
        {
            return;
        }

        foreach (var sel in selectionManager.Selection)
        {
            var posScreen = activeCamera.WorldToScreenPoint(sel.Position);
            posScreen.y = Screen.height - posScreen.y;
            if (!Screen.safeArea.Contains(new Vector2(posScreen.x, posScreen.y)))
            {
                continue;
            }

            GUI.Label(new Rect(posScreen.x - 15, posScreen.y - 15, 100, 100), $"[{sel.Atom.Symbol}]",
                selectionLabelStyle);
        }
    }
}