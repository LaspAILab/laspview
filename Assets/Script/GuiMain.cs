using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using B83.Win32;
using System.Linq;
using System;

public class GuiMain : MonoBehaviour
{
    static public GuiMain S;
    public GameObject B_ToolbarPanelPrefab;
    public GameObject L_DisplayPanelPrefab;
    public GameObject L_EditPanelPrefab;
    public GameObject L_FilePanelPrefab;
    public GameObject L_ProjectPanelPrefab;
    public GameObject M_AddAtomWidgetPrefab;
    public GameObject M_EditLatticeWidgetPrefab;
#if !UNITY_WEBGL
    public GameObject M_LaspInWidgetPrefab;
    public GameObject R_LASPPanelPrefab;
    public GameObject R_ServerPanelPrefab;
#endif

    public GameObject nodePrefab;
    public GameObject atomPrefab;
    public GameObject mouseBoxLinePrefab;

    GameObject menuPanel;
    public GameObject statusPanel;
    public GameObject leftCanvas;
    public GameObject rightCanvas;

    public bool menuBtnStatus = false;

    private Color UiPressedColor = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
    private Color UiUnPressedColor = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));

    //********************* VAR for MOUSE CONTROL*********************
    syfDragUI dragCom = null;
    Vector3 selectGOOffset;
    public bool rayStartOnUI = false;

    public Transform cameraTransform;
    private Transform cameraCenter;
    private Vector3 mouseLastPos;
    private float noOperationTimeSince;

    GameObject castGO = null;
    Ray mouseRay;
    RaycastHit hitInfo;

    public bool editEnable;          //Pick Enalble
    public bool onMove = false;
    public bool onCameraRotate = false;
    public bool onCameraTranslate = false;
    public bool onAtomSelect = false;
    public bool onAtomRotate = false;
    public bool onAtonTranslate = false;
    public float onMoveCollectTime;

    //Local Var used for Camera Control
    private Vector3 pickAtomAvgPos;         //for pick rotate
    private Vector3 pickAtomAvgPosScreen;
    private Vector3 offsetTemp;             //for tranlate    


    public List<GameObject> pickGO = new List<GameObject>();         //for Pick
    public List<GameObject> copyGO = new List<GameObject>();         //for Pick
    private List<GameObject> pasteGO = new List<GameObject>();
    public GameObject expandAtom;           //for RayCastPick, expand Atom
    private Vector3 expandAtomScale = new Vector3(0.3f, 0.3f, 0.3f);//for RayCastPick, expand Atom
    private float mouse0PressedTime;        //for Pick
    private Vector3 mouseStart;             //for Pick
    public LineRenderer[] mouseBox;         //for Pick
    static Vector3 copyBias = new Vector3(2500f, 0, 0);
    private List<Atom> pickAtomOrigin = new List<Atom>(); //for pick Atom
    private List<Atom> pickAtomDerived = new List<Atom>();
    //private List<>
    private List<Bond> changeBond = new List<Bond>();
    private List<Bond> changeBondDerived = new List<Bond>();


    private Dictionary<Vector3, Atom> atomScreenPosTemp = new Dictionary<Vector3, Atom>();
//************************** END  *****************************


void Awake()
    {
        S = this;

        menuPanel = GameObject.Find("MenuPanel");
        statusPanel = GameObject.Find("StatusPanel");
        leftCanvas = GameObject.Find("LeftCanvas");
        rightCanvas = GameObject.Find("RightCanvas");
        cameraTransform = Camera.main.transform;
        SetMenuBtn();

        cameraCenter = Instantiate(nodePrefab).GetComponent<Transform>();
        cameraCenter.gameObject.name = "NodePrefab(CameraCenter)";
    }

    //relate Button with GameObjects, mainly panels, that can be hidden.
    public void BtnHideObject(Button btn, GameObject go = null)
    {
        if (go == null) return;
        if (go.activeSelf)
        {
            go.SetActive(false);
            btn.image.color = UiUnPressedColor;
        }
        else
        {
            go.SetActive(true);
            btn.image.color = UiPressedColor;
        }
    }

    public void SetMenuBtn()
    {
        GameObject b_ToolbarPanel = Instantiate(B_ToolbarPanelPrefab);
        GameObject l_ProjectPanel = Instantiate(L_ProjectPanelPrefab);
        GameObject l_FilePanel = Instantiate(L_FilePanelPrefab);
        GameObject l_DisplayPanel = Instantiate(L_DisplayPanelPrefab);
        GameObject m_AddAtomWidget = Instantiate(M_AddAtomWidgetPrefab);
        GameObject m_EditLatticeWidget = Instantiate(M_EditLatticeWidgetPrefab);
#if !UNITY_WEBGL
        GameObject r_ServerPanel = Instantiate(R_ServerPanelPrefab);
        GameObject r_LASPPanel = Instantiate(R_LASPPanelPrefab);
        GameObject m_LaspInWidget = Instantiate(M_LaspInWidgetPrefab);
#endif
        GameObject l_EditPanel = Instantiate(L_EditPanelPrefab);
    }

    public void SetAllUIInteract(GameObject go, bool interactable)
    {
        foreach (Selectable c in go.GetComponentsInChildren<Selectable>())
        {
            SetUIInteract(c, interactable);
        }
    }
    public  void SetUIInteract(Selectable s, bool interactable)
    {
        if (interactable == true)
        {
            s.image.color = UiUnPressedColor;
            s.interactable = true;
        }
        else
        {
            s.image.color = UiPressedColor;
            s.interactable = false;
        }
    }


    //*************************** CAMERA CONTROL **********************************
    //******************************** & ******************************************
    //************************** MOUSE INTERACTION *******************************+
    void Update()
    {

        //处理鼠标位置，如果鼠标没动就跳过一些处理
        if (mouseLastPos == null)
        {
            mouseLastPos = Input.mousePosition;
        }
        //print(Input.mousePosition / Screen.height);
        float mouse_x = (Input.mousePosition - mouseLastPos).x ;
        float mouse_y = -(Input.mousePosition - mouseLastPos).y ;



        //Vector3 mouse_world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        castGO = null;
        mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (mouseLastPos != Vector3.zero)
        {
            if (Physics.Raycast(mouseRay, out hitInfo, 500f, 1 << LayerMask.NameToLayer("TransparentFX")))
            {
                castGO = hitInfo.collider.gameObject;
            }
            else
            {
                castGO = null;
            }
            //鼠标位置上的原子变大
            if (onMove == false)
            {
                if (castGO == null)
                {
                    //No atom is cast
                    if (expandAtom != null)
                    {
                        // A -> null
                        expandAtom.transform.localScale -= expandAtomScale;
                        expandAtom = null;
                    }
                }
                else if (expandAtom == null)
                {
                    //null -> A
                    expandAtom = castGO;
                    castGO.transform.localScale += expandAtomScale;
                }
                else if (castGO == expandAtom)
                {
                    //cast atom unchange
                    //A -> A
                }
                else if (castGO != expandAtom)
                {
                    //cast atom change
                    //A -> B
                    expandAtom.transform.localScale -= expandAtomScale;
                    expandAtom = castGO;
                    castGO.transform.localScale += expandAtomScale;
                }
            }
        }

        // 如果没有鼠标操作(纯键盘操作)
        if (!(Input.GetKey(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse0) ||
            Input.GetKey(KeyCode.Mouse1) || Input.GetKeyUp(KeyCode.Mouse1) ||
            Input.GetKey(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.Mouse2) || Input.GetKeyUp(KeyCode.Mouse2) ||
            Input.GetAxis("Mouse ScrollWheel") != 0))
        {
            //Ctrl +CV delete
            if (editEnable)
            {
                //DELETE/Backspace删除原子
                if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    foreach (GameObject go in pickGO)
                    {
                        MainScr.S.CurrentStructure().atomList.Remove(go.GetComponent<Atom>());
                        UnityEngine.Object.Destroy(go.transform.parent.gameObject);
                    }
                    MainScr.S.CurrentStructure().AddAllBond();
                    pickGO.Clear();
                }
                //Ctrl_C 复制原子
                else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
                {
                    // clear copyGO
                    if (copyGO != null && copyGO.Count != 0)
                    {
                        foreach (GameObject go in copyGO)
                        {
                            UnityEngine.Object.Destroy(go.transform.parent.gameObject);
                        }
                    }
                    copyGO.Clear();

                    // pickGO.CopyTo(copyGO):Copy them to cdnt +copyBias
                    foreach (GameObject go in pickGO)
                    {
                        Atom a = go.GetComponent<Atom>();

                        GameObject copyNode = Instantiate(nodePrefab);
                        copyNode.transform.position = copyBias; //node坐标位于(copyBias, 实体坐标)
                        copyNode.transform.SetParent(this.transform);

                        Atom copyAtom = Instantiate(atomPrefab).GetComponent<Atom>();
                        copyAtom.Init(a.cdnt, a.ele, copyNode.transform, a.charge); //Atom坐标为cdnt
                        copyAtom.transform.SetParent(copyNode.transform, true);
                        copyAtom.transform.localPosition = Vector3.zero;

                        copyGO.Add(copyAtom.gameObject);
                    }
                }
                //Ctrl+V 粘贴原子
                else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
                {
                    pasteGO.Clear();
                    Vector3 pasteBias = Camera.main.transform.right.normalized + Camera.main.transform.up.normalized;
                    foreach (GameObject go in copyGO)
                    {
                        Atom a = go.GetComponent<Atom>();
                        Atom p = MainScr.S.CurrentStructure().AddAtom(a.ele, a.cdnt + 0.5f * pasteBias, a.charge);
                        pasteGO.Add(p.gameObject);
                    }

                    //Cancel Select pickGO
                    foreach (GameObject go in pickGO)
                    {
                        foreach (Renderer ren in go.transform.parent.GetComponentsInChildren<Renderer>())
                        {
                            ren.material.color = go.GetComponent<Atom>().color;
                        }
                    }
                    pickGO.Clear();
                    MainScr.S.CurrentArc().DrawStructure(MainScr.S.CurrentArc().currentStr);

                    //Select pastGO
                    pickGO = pasteGO;
                    foreach (GameObject go in pasteGO)
                    {
                        foreach (Renderer ren in go.transform.parent.GetComponentsInChildren<Renderer>())
                        {
                            ren.material.color = Color.yellow;
                        }
                    }

                    // clear copyGO
                    if (copyGO != null && copyGO.Count != 0)
                    {
                        foreach (GameObject go in copyGO)
                        {
                            UnityEngine.Object.Destroy(go.transform.parent.gameObject);
                        }
                    }
                    copyGO.Clear();

                    // pickGO.CopyTo(copyGO):Copy them to cdnt +copyBias
                    foreach (GameObject go in pickGO)
                    {
                        Atom a = go.GetComponent<Atom>();

                        GameObject copyNode = Instantiate(nodePrefab);
                        copyNode.transform.position = copyBias; //node坐标位于(copyBias, 实体坐标)
                        copyNode.transform.SetParent(this.transform);

                        Atom copyAtom = Instantiate(atomPrefab).GetComponent<Atom>();
                        copyAtom.Init(a.cdnt, a.ele, copyNode.transform, a.charge); //Atom坐标为cdnt
                        copyAtom.transform.SetParent(copyNode.transform, true);
                        copyAtom.transform.localPosition = Vector3.zero;

                        copyGO.Add(copyAtom.gameObject);
                    }
                }
                //Ctrl+Z 撤回Undo
                else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
                {
                    //wait!
                }
            }
        }
        // 如果有鼠标操作
        else if(onMove == false)
        {
            // 鼠标开始操作时 先判断是否在UI上
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) ||
                Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Mouse2) ||
                Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    rayStartOnUI = true;

                    //在UI上时，获得UI
                    foreach (RaycastResult r in ClickObject())
                    {
                        dragCom = r.gameObject.GetComponent<syfDragUI>();
                        //When GameObject contain syfDragUIComponent, it can be Dragged
                        if (dragCom != null)
                        {
                            //But actually dragged GameObject is set in syfDragUI.dragGameObjcet
                            selectGOOffset = dragCom.draggedGameObject.transform.position - Input.mousePosition;
                            break;
                        }
                    }
                }
                else
                {
                    rayStartOnUI = false;
                }
            }

            if (rayStartOnUI == true)
            {
                //To move a Draggable Widget(GameObject that syfDragUI component poiting to)
                if (Input.GetKey(KeyCode.Mouse2) && dragCom != null)
                {
                    Vector3 screemScale = this.gameObject.transform.localScale;
                    Vector3 widgetPos = Input.mousePosition + selectGOOffset;

                    //MakeSure Draggable widget is still in the frame
                    //A east home-made rectangle position limiter. If need update, you can write a UICollider youself. Use para in RectTransform
                    widgetPos.y = Mathf.Max(widgetPos.y, dragCom.lowerBound * screemScale.y);
                    widgetPos.y = Mathf.Min(widgetPos.y, Screen.height - dragCom.upperBound * screemScale.y);
                    widgetPos.x = Mathf.Max(widgetPos.x, dragCom.leftBound * screemScale.x);
                    widgetPos.x = Mathf.Min(widgetPos.x, Screen.width - dragCom.rightBound * screemScale.x);
                    dragCom.draggedGameObject.transform.position = widgetPos;
                }
            }
            else if (rayStartOnUI == false)
            {
                //鼠标左键按下，无LeftCronl: 准备开始选中原子：取消所有选中的原子，开始计时
                if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftControl))
                {
                    onMove = true;
                    onAtomSelect = true;

                    mouse0PressedTime = Time.time;
                    mouseStart = Input.mousePosition;
                    DisSelectPickGO();

                    //mouseBox reset
                    if (mouseBox.Length == 0)
                    {
                        mouseBox = new LineRenderer[4];
                    }
                }
                //鼠标左键按下，有LeftCronl: 准备开始选中原子：不取消所有选中的原子，开始计时（增加选中的原子）
                else if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKey(KeyCode.LeftControl))
                {
                    onMove = true;
                    onAtomSelect = true;

                    mouse0PressedTime = Time.time;
                    mouseStart = Input.mousePosition;

                    //mouseBox reset
                    if (mouseBox.Length == 0)
                    {
                        mouseBox = new LineRenderer[4];
                    }
                }


                //如果没有LCtrl，则为常规镜头旋转、平移、缩放操作
                else if (!(Input.GetKey(KeyCode.LeftControl)))
                {
                    // When Mouse1 / LShit+mous0 was clicked(GetKeyDown) ,check rotation mode(鼠标右键可用LShift替换)
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        Structure struc = MainScr.S.CurrentStructure();
                        offsetTemp = struc.GetCellTransfer((int)struc.strucModelPara["lat range a"], (int)struc.strucModelPara["lat range b"], (int)struc.strucModelPara["lat range c"]);
                        onMove = true;
                        onCameraRotate = true;
                    }
                    //when mouse1 / LShit+mous0 is pressed, rotate(鼠标右键可用LShift替换)
                    else if (Input.GetKeyDown(KeyCode.Mouse2) || (Input.GetKeyDown(KeyCode.LeftShift)))
                    {
                        offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        onMove = true;
                        onCameraTranslate = true;
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") > 0)
                    {
                        //cameraTransform.Translate(Vector3.forward * 0.5f);
                        Camera.main.orthographicSize += 0.5F;
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") < 0)
                    {
                        if (Camera.main.orthographicSize >= 1)
                            Camera.main.orthographicSize -= 0.5F;
                        //cameraTransform.Translate(Vector3.forward * -0.5f);
                    }
                }
                //如果没有LCtrl
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    //如在编辑模式下，为编辑原子旋转、平移操作
                    if (editEnable && pickGO.Count != 0)
                    {
                        //LC(LeftControl)+鼠标右键按下： 准备开始旋转选中原子
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Mouse1)
                         || Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse1))
                        {

                            pickAtomAvgPos = Vector3.zero;
                            //pickAtomAvgPos is in World Space
                            foreach (GameObject ga in pickGO)
                            {
                                pickAtomAvgPos += ga.transform.position;
                            }
                            pickAtomAvgPos /= pickGO.Count;
                            //pickAtomAvgPosScreen  is in Screen Space
                            pickAtomAvgPosScreen = Camera.main.WorldToScreenPoint(pickAtomAvgPos);
                            onMove = true;
                            onAtomRotate = true;

                            PickControlStart();

                        }


                        //LC(LeftControl)+鼠标中键按下： 准备开始平移选中原子(鼠标右键可用LShift替换)
                        else if (Input.GetKey(KeyCode.LeftControl) && (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.LeftShift))
                              || Input.GetKeyDown(KeyCode.LeftControl) && (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift)))
                        {
                            offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            onMove = true;
                            onAtonTranslate = true;

                            PickControlStart();
                        }

                    }
                }
            }
            onMoveCollectTime = Time.time;
        }
        else if (onMove)
        {
            //选中原子
            if (onAtomSelect)
            {
                //Update：按下鼠标时：按下的时间短，单选射线拾取选中的原子；按下事件长，框选中心位置在框内的原子，并更新颜色和框
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    //shout presse, Select the castGO
                    if (Time.time - mouse0PressedTime < 0.05f)
                    {
                        if (castGO != null)
                        {
                            if (!pickGO.Contains(castGO))
                            {
                                pickGO.Add(castGO);
                            }
                        }
                    }
                    //Long press, Project All the Atom and than Judge if should be Select
                    else if (Time.time - mouse0PressedTime >= 0.2f && atomScreenPosTemp.Count() == 0)
                    {
                        atomScreenPosTemp.Clear();
                        foreach (Atom at in MainScr.S.CurrentStructure().atomList)
                        {
                            atomScreenPosTemp.Add(Camera.main.WorldToScreenPoint(at.transform.position), at);

                        }

                        foreach (ExtendStructure ext in MainScr.S.CurrentStructure().extendStructs)
                        {
                            if (ext == null) continue;
                            foreach (Atom at in ext.extendAtomList)
                            {
                                atomScreenPosTemp.Add(Camera.main.WorldToScreenPoint(at.transform.position), at);
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<Vector3, Atom> kvpair in atomScreenPosTemp)
                        {
                            Vector3 screenPos = kvpair.Key;
                            if ((screenPos.x - mouseStart.x) * (screenPos.x - Input.mousePosition.x) < 0)
                                if ((screenPos.y - mouseStart.y) * (screenPos.y - Input.mousePosition.y) < 0)
                                    if (!pickGO.Contains(kvpair.Value.gameObject))
                                    {
                                        pickGO.Add(kvpair.Value.gameObject);
                                        SelectPickGO(kvpair.Value.gameObject);
                                    }
                        }

                        foreach (LineRenderer r in mouseBox)
                        {
                            if (r != null)
                            {
                                UnityEngine.Object.Destroy(r.gameObject);
                            }
                        }
                        Vector3 v1 = Input.mousePosition;
                        Vector3 v2 = mouseStart;
                        mouseBox[0] = this.mouseBoxLine(v1, new Vector3(v1.x, v2.y, 0f));
                        mouseBox[1] = this.mouseBoxLine(v1, new Vector3(v2.x, v1.y, 0f));
                        mouseBox[2] = this.mouseBoxLine(v2, new Vector3(v1.x, v2.y, 0f));
                        mouseBox[3] = this.mouseBoxLine(v2, new Vector3(v2.x, v1.y, 0f));
                    }
                }
                //End: 鼠标左键抬起时：确保所有选中原子的颜色，移去框
                else if (!Input.GetKey(KeyCode.Mouse0))
                {
                    atomScreenPosTemp.Clear();
                    SelectPickGO();

                    foreach (LineRenderer r in mouseBox)
                    {
                        if (r != null)
                        {
                            UnityEngine.Object.Destroy(r.gameObject);
                        }
                    }

                    this.MeasurePickAtoms();

                    onMove = false;
                    onAtomSelect = false;
                }

            }
            else if (onCameraRotate)
            {
                if (Input.GetKey(KeyCode.Mouse1))
                {
                    if (Physics.Raycast(mouseRay, out hitInfo, 500f, 1 << LayerMask.NameToLayer("Default")))
                    {
                        cameraTransform.RotateAround(cameraCenter.position, cameraTransform.up, mouse_x * 360 / Screen.height * Camera.main.orthographicSize / offsetTemp.magnitude); //我也不知道为啥是360，调出来就是(>.<)
                        cameraTransform.RotateAround(cameraCenter.position, cameraTransform.right, mouse_y * 360 / Screen.height * Camera.main.orthographicSize / offsetTemp.magnitude);
                    }
                    else
                    {
                        //2D rotate
                        Vector3 centerScreen = Camera.main.WorldToScreenPoint(offsetTemp/2);
                        Vector3 a = centerScreen - Input.mousePosition;
                        Vector3 b = centerScreen - mouseLastPos;
                        float degree;
                        if (Vector3.Cross(a, b).z > 0)
                        {
                            degree = Vector3.Angle(a, b);
                        }
                        else
                        {
                            degree = -Vector3.Angle(a, b);
                        }
                        cameraTransform.RotateAround(cameraCenter.position, cameraTransform.forward, degree);
                    }
                }
                else if (!Input.GetKey(KeyCode.Mouse1))
                {
                    onMove = false;
                    onCameraRotate = false;
                }
            }
            else if (onCameraTranslate)
            {
                if (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift))
                {
                    cameraTransform.position += (offsetTemp - Camera.main.ScreenToWorldPoint(Input.mousePosition));
                }
                else
                {
                    onMove = false;
                    onCameraTranslate = false;
                }
            }
            else if (onAtomRotate)
            {
                //LC+鼠标右键按住时：旋转选中原子
                if (Input.GetKey(KeyCode.Mouse1))
                {
                    float pickAtomFurthestDis = 0f;
                    foreach (GameObject ga in pickGO)
                    {
                        Vector3 vec = Camera.main.WorldToScreenPoint(ga.transform.position);
                        pickAtomFurthestDis = Mathf.Max((vec - pickAtomAvgPosScreen).magnitude, pickAtomFurthestDis);
                    }
                    pickAtomFurthestDis = Mathf.Max(100.0f, pickAtomFurthestDis);


                    if ((Input.mousePosition - pickAtomAvgPosScreen).magnitude < pickAtomFurthestDis)
                    {
                        //3D rotate
                        foreach (GameObject ga in pickGO)
                        {
                            //3D rotate
                            //ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.up, mouse_x * 360 / Screen.height * Camera.main.orthographicSize / (pickAtomFurthestDis / Screen.height * Camera.main.orthographicSize));
                            ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.up, -mouse_x / pickAtomFurthestDis * 90); //我也不知道为啥是90，调出来就是(>.<)
                            ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.right, -mouse_y / pickAtomFurthestDis * 90);
                            //ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.right, -mouse_y * 8);
                        }
                    }
                    else
                    {
                        //2D rotate
                        Vector3 centerScreen = Camera.main.WorldToScreenPoint(offsetTemp / 2);
                        Vector3 a = centerScreen - Input.mousePosition;
                        Vector3 b = centerScreen - mouseLastPos;
                        float degree;
                        if (Vector3.Cross(a, b).z > 0)
                        {
                            degree = Vector3.Angle(a, b);
                        }
                        else
                        {
                            degree = -Vector3.Angle(a, b);
                        }
                        foreach (GameObject ga in pickGO)
                        {
                            ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.forward, -degree);
                        }
                    }
                    PickControlUpdate();
                }
                //松开LC或鼠标右键：收尾
                else if (!Input.GetKey(KeyCode.Mouse1)) 
                {
                    PickControlEnd();
                    EditSaveSturc();
                    onMove = false;
                    onAtomRotate = false;
                }
            }
            else if (onAtonTranslate)
            {   //LC(LeftControl)+鼠标中键按下： 平移选中原子(鼠标右键可用LShift替换)
                if ((Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift)))
                {
                    Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offsetTemp;
                    foreach (GameObject go in pickGO)
                    {
                        go.transform.parent.position += vec;
                    }

                    offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                    PickControlUpdate();
                }
                //松开LC或鼠标中键：收尾(鼠标右键可用LShift替换)
                else if (!(Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift)))
                {
                    PickControlEnd();
                    EditSaveSturc();
                    onMove = false;
                    onAtonTranslate = false;
                }
            }

            if (Time.time - onMoveCollectTime > 0.1f)
            {
                GC.Collect();
                onMoveCollectTime = Time.time;
            }

        }


        bool getkey = (Input.GetKey(KeyCode.Delete) || Input.GetKey(KeyCode.Backspace) || Input.GetKey(KeyCode.LeftControl));
        bool mouseout = (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height);
        bool ifoperate = getkey || !(mouseout && onMove == false);// || (mouseLastPos != Input.mousePosition); 

        if (ifoperate)
        {
            noOperationTimeSince = Time.time;
        }
        if (Time.time - noOperationTimeSince > 1f)
        {
            //相机渲染画面到贴图，会自动暂停相机的目标屏幕渲染
            Camera.main.targetDisplay = 8;
        }
        else
        {
            //恢复相机
            Camera.main.targetDisplay = 0;
        }


        mouseLastPos = Input.mousePosition;
    }

    public void CameraActicate()
    {
        noOperationTimeSince = 0;
    }


    public List<RaycastResult> ClickObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results;
    }

    private LineRenderer mouseBoxLine(Vector3 v1, Vector3 v2)
    {
        //drawLine
        LineRenderer line;
        line = Instantiate(mouseBoxLinePrefab).GetComponent<LineRenderer>();
        //line.positionCount = 2;//设置两点
        //line.startColor = Color.white; //设置直线颜色
        //line.endColor = Color.white;
        line.startWidth = 0.02f;//设置直线宽度
        line.endWidth = 0.02f;

        //设置指示线的起点和终点
        line.SetPosition(0, Camera.main.ScreenToWorldPoint(v1) + Camera.main.transform.forward);
        line.SetPosition(1, Camera.main.ScreenToWorldPoint(v2) + Camera.main.transform.forward);
        return line;
    }

    public void SelectPickGO()
    {
        if (pickGO.Count != 0)
        {
            foreach (GameObject go in pickGO)
            {
                foreach (Renderer ren in go.transform.parent.GetComponentsInChildren<Renderer>())
                {
                    ren.material.color = Color.yellow;
                }
            }
        }
    }
    private void SelectPickGO(GameObject go)
    {
        foreach (Renderer ren in go.transform.parent.GetComponentsInChildren<Renderer>())
        {
            ren.material.color = Color.yellow;
        }
    }

    public void DisSelectPickGO()
    {
        if (pickGO.Count != 0)
        {
            //Cancel pickGO
            foreach (GameObject go in pickGO)
            {
                foreach (Renderer ren in go.transform.parent.GetComponentsInChildren<Renderer>())
                {
                    ren.material.color = go.GetComponent<Atom>().color;
                }
            }
            pickGO.Clear();
        }
    }

    private void MeasurePickAtoms()
    {
        if (pickGO.Count == 1)
        {
            string st = string.Format("{0} position: {1:f4} {2:f4} {3:f4}",  pickGO[0].GetComponent<Atom>().originAtom.ele, pickGO[0].transform.position.x, pickGO[0].transform.position.y, pickGO[0].transform.position.z);
            ConFun.S.printStatusInfo(st);
        }
        else if (pickGO .Count == 2)
        {
            string st = string.Format("{0}-{1} distance:   {2:f4} ", pickGO[0].GetComponent<Atom>().originAtom.ele, pickGO[1].GetComponent<Atom>().originAtom.ele, (pickGO[0].transform.position - pickGO[1].transform.position).magnitude);
            ConFun.S.printStatusInfo(st);
        }
        else if (pickGO.Count == 3)
        {
            string st = string.Format("{0}-{1}-{2}  angle: {3:f4} ", pickGO[0].GetComponent<Atom>().originAtom.ele, pickGO[1].GetComponent<Atom>().originAtom.ele, pickGO[2].GetComponent<Atom>().originAtom.ele,
                  Vector3.Angle(pickGO[0].transform.position - pickGO[1].transform.position, pickGO[2].transform.position - pickGO[1].transform.position));
            ConFun.S.printStatusInfo(st);       
        }
        else if (pickGO.Count == 4)
        {
            string st = string.Format("{0}-{1}-{2}-{3} torison: {4:f4} ", pickGO[0].GetComponent<Atom>().originAtom.ele, pickGO[1].GetComponent<Atom>().originAtom.ele, pickGO[2].GetComponent<Atom>().originAtom.ele, pickGO[3].GetComponent<Atom>().originAtom.ele,
                  Vector3.Angle(pickGO[0].transform.position - pickGO[1].transform.position, pickGO[3].transform.position - pickGO[2].transform.position));
            ConFun.S.printStatusInfo(st);
        }
    }

    public void FocusCameraCenter()
    {
        int a = MainScr.S.CurrentStructure().currentExtendA;
        int b = MainScr.S.CurrentStructure().currentExtendB;
        int c = MainScr.S.CurrentStructure().currentExtendC;
        Vector3 vec = MainScr.S.CurrentStructure().GetCellTransfer(a, b, c) / 2;
        cameraCenter.position = vec;
        cameraTransform.position = vec;
        cameraTransform.Translate(Vector3.back.normalized * vec.magnitude * 4);
        Camera.main.orthographicSize = vec.magnitude;
    }

    //****************** CAMERA CONTROL & MOUSE INTERACTION ***********************
    //****************************** END ******************************************

    public void EditSaveSturc()
    {
        foreach(Atom a in MainScr.S.CurrentStructure().atomList)
        {
            a.cdnt = a.transform.parent.position;
        }
    }


    public void PickControlStart()
    {
        Structure struc = MainScr.S.CurrentStructure();
        pickAtomOrigin.Clear();
        foreach (GameObject go in pickGO)
        {
            pickAtomOrigin.Add(go.GetComponent<Atom>().originAtom);
        }

        pickAtomDerived.Clear();
        foreach (GameObject go in pickGO)
        {
            pickAtomDerived = pickAtomDerived.Union(go.GetComponent<Atom>().originAtom.derivedAtom).ToList<Atom>();
        }
        foreach(GameObject go in pickGO)
        {
            pickAtomDerived.Remove(go.GetComponent<Atom>());
        }

        changeBond.Clear();
        foreach (Atom a in pickAtomOrigin)
        {
            foreach (Bond b1 in a.transform.parent.GetComponentsInChildren<Bond>())
            {
                if (pickAtomOrigin.Contains(b1.pointToAtom)) continue;
                struc.bondList.Remove(b1);
                changeBond.Add(b1);

                //remove Bond who is from Unpick Atom to Pick Atom a
                foreach (Bond b2 in b1.pointToAtom.gameObject.transform.parent.GetComponentsInChildren<Bond>())
                {
                    if (b2.pointToAtom == a)
                    {
                        struc.bondList.Remove(b2);
                        changeBond.Add(b2);
                    }
                }

            }
        }

        changeBondDerived.Clear();
        List<Bond> temp = new List<Bond>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    if (i == 0 && j == 0 && k == 0) continue;
                    else if (struc.extendStructs[i, j, k] != null)
                    {
                        temp.Clear();
                        foreach (Bond bdExtend in struc.extendStructs[i, j, k].extendBondList)
                        {
                            if (changeBond.Contains(bdExtend.originBond))
                            {
                                temp.Add(bdExtend);
                                changeBondDerived.Add(bdExtend);
                            }
                        }
                        foreach (Bond bd in temp)
                        {
                            struc.extendStructs[i, j, k].extendBondList.Remove(bd);
                        }
                    }
                }
            }
        }
        

    }

    public void PickControlUpdate()
    {
        Structure struc = MainScr.S.CurrentStructure();

        //make sure all origin Atom is moved
        foreach (GameObject go in pickGO)
        {
            Atom at = go.GetComponent<Atom>();
            if (at.extendParent != null)
            {
                at.originAtom.transform.parent.position = go.transform.position - at.extendParent.bias;
                at.originAtom.transform.parent.rotation = go.transform.rotation;
            }

        }
        // move pickAtomDerived if it is not an Origin Atom
        foreach (Atom at in pickAtomDerived)
        {
            if (at.originAtom != at)
            {
                at.transform.parent.position = at.originAtom.transform.position + at.extendParent.bias;
                at.transform.parent.rotation = at.originAtom.transform.rotation;
            }
        }



        Vector3 dieta;
        float rate;
        float bondBoundry = struc.strucModelPara["tolerance"];
        Matrix4x4 inverse = struc.lat.inverse;

        if (changeBond.Count != 0)
        {
            foreach (Bond b in changeBond)
            {
                ObjectPool.S.RestoreBond(b);
            }
        }
        changeBond.Clear();

        //print(changeBondDerived.Count);
        if (changeBondDerived.Count != 0)
        {
            foreach (Bond b in changeBondDerived)
            {
                ObjectPool.S.RestoreBond(b);
            }
        }
        changeBondDerived.Clear();
        //foreach (Bond bd in ObjectPool.S.bondPool.pool)
        //{
        //    print(bd.GetHashCode());
        //}
        //GC.Collect();

        if (struc.strucModelPara["display style"] != 1)
        {
            foreach (Atom a1 in pickAtomOrigin)
            {
                //print(a1.extendParent);
                Vector3 p1 = a1.transform.position;
                //print(p1);
                Vector3 fractionalPos = inverse * p1;
                //Vector3 fractionalPos = Vector3.zero;
                int[] vecs = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    vecs[i] = Mathf.FloorToInt(fractionalPos[i] + 1.5e-3f);
                    //print(vecs[i]);
                }

                foreach (Atom a2 in struc.atomList)
                {
                    Vector3 p2 = a2.transform.parent.position;

                    Vector3 backBias = struc.GetCellTransfer(vecs[0], vecs[1], vecs[2]);


                    if (a2 != a1 && struc.CheckBond(a1, a2, backBias, bondBoundry))
                    {
                        dieta = a2.transform.position - a1.transform.position;
                        rate = a1.radius / (a1.radius + a2.radius);
                        changeBond.Add(struc.AddBond(a1, a2, dieta + backBias, rate));
                        changeBond.Add(struc.AddBond(a2, a1, -dieta - backBias, 1 - rate));
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 vec = struc.cellTranslate[i];
                        if (struc.CheckBond(a1, a2, backBias + vec, bondBoundry))
                        {
                            dieta = a2.transform.position - a1.transform.position;
                            rate = a1.radius / (a1.radius + a2.radius);
                            changeBond.Add(struc.AddBond(a1, a2, dieta + backBias + vec, rate));
                            changeBond.Add(struc.AddBond(a2, a1, -dieta - backBias - vec, 1 - rate));
                        }
                        if (struc.CheckBond(a1, a2, backBias - vec, bondBoundry))
                        {
                            dieta = a2.transform.position - a1.transform.position;
                            rate = a1.radius / (a1.radius + a2.radius);
                            changeBond.Add(struc.AddBond(a1, a2, dieta + backBias - vec, rate));
                            changeBond.Add(struc.AddBond(a2, a1, -dieta - backBias + vec, 1 - rate));
                        }
                    }

                }
            }
            //print(ObjectPool.S.bondPool.pool.Count());
            //foreach(Atom at in pickAtomOrigin)
            //{
            //    print((pickAtomOrigin.IndexOf(at),"th", at.transform.parent.GetComponentsInChildren<Bond>().Length, "bond"));
            //}
            ////Update Extend Atom & Bond

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        if (i == 0 && j == 0 && k == 0) continue;
                        else if (struc.extendStructs[i, j, k] != null)
                        {
                            Vector3 bias = struc.GetCellTransfer(i, j, k);
                            foreach (Bond bd in changeBond)
                            {
                                Bond newBond = ObjectPool.S.RentBond();

                                int index = bd.fromAtom.index;
                                newBond.GetComponent<Renderer>().material.color = struc.extendStructs[i, j, k].extendAtomList[index].GetComponent<Renderer>().material.color;
                                changeBondDerived.Add(newBond);

      
                                newBond.transform.position = bd.transform.position + bias;
                                newBond.transform.rotation = bd.transform.rotation;
                                newBond.transform.localScale = bd.transform.localScale;

                                newBond.originBond = bd;
                                newBond.extendParent = struc.extendStructs[i, j, k];
                                newBond.fromAtom = null;
                                newBond.pointToAtom = null;


                            }
                        }
                    }
                }
            }
        }
        //GC.Collect();
        //print(ObjectPool.S.bondPool.pool.Count);
    }

    public void PickControlEnd()
    {
        Structure struc = MainScr.S.CurrentStructure();
        struc.MoveIntoLatticAtoms(struc.atomList);
        foreach (Atom at1 in struc.atomList)
        {
            foreach (Atom at2 in at1.derivedAtom)
            {
                at2.transform.parent.position = at1.transform.position + at2.extendParent.bias;
                //at2.index = at1.index;
            }
        }
        this.PickControlUpdate();

        //if (changeBond.Count != 0)
        //{
        //    foreach (Bond bd in changeBond)
        //    {
        //        //Object.Destroy(b.gameObject);
        //        struc.bondList.Add(bd);
        //        bd.transform.SetParent(bd.fromAtom.transform.parent);
        //    }
        //    changeBond.Clear();


        //    if (changeBondDerived.Count != 0)
        //    {
        //        foreach (Bond bd in changeBondDerived)
        //        {
        //            //Object.Destroy(b.gameObject);
        //            bd.extendParent.extendBondList.Add(bd);
        //            int index = bd.originBond.fromAtom.index;
        //            bd.transform.SetParent(bd.extendParent.extendAtomList[index].transform.parent);
        //        }
        //    }
        //    changeBondDerived.Clear();
        //}
        if (changeBond.Count != 0)
        {
            foreach (Bond bd in changeBond)
            {
                ObjectPool.S.RestoreBond(bd);
            }
        }
        changeBond.Clear();
        if (changeBondDerived.Count != 0)
        {
            foreach (Bond bd in changeBondDerived)
            {
                ObjectPool.S.RestoreBond(bd);
            }
        }
        changeBondDerived.Clear();

        //DisSelectPickGO();
        Dictionary<string, float> tempDic = new Dictionary<string, float>(struc.strucModelPara);
        tempDic["display style"] = 1;
        struc.ChangeLatRange(tempDic);
        struc.strucModelPara = null;
        struc.DrawModel();

    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
    void OnEnable()
    {
        // must be installed on the main thread to get the right thread id.
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }
    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, POINT aPos)
    {
        // do something with the dropped file names. aPos will contain the 
        // mouse position within the window where the files has been dropped.
        ConFun.S.printStatusInfo("Dropped " + string.Join(" ", aFiles) + " files");
        foreach (string file in aFiles)
        {
            MainScr.S.AddArc(file);
        }
    }
#endif
}

