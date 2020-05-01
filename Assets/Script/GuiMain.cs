using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using B83.Win32;



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

    //********************* VAR for MOUSE CONTROL*********************
    syfDragUI dragCom = null;
    Vector3 selectGOOffset;
    public bool rayStartOnUI = false;

    public Transform cameraTransform;
    private Transform cameraCenter;
    private Vector3 mouseLastPos;

    public bool updateBond;
    public bool editEnable;          //Pick Enalble
    private bool rotationMode3d = false;    //for rotate
    //Local Var used for Camera Control
    private Vector3 pickAtomAvgPos;         //for pick rotate
    private Vector3 offsetTemp;             //for tranlate    
    public List<GameObject> pickGO;         //for Pick
    public List<GameObject> copyGO;         //for Pick
    public GameObject expandAtom;           //for RayCastPick, expand Atom
    private Vector3 expandAtomScale = new Vector3(0.3f, 0.3f, 0.3f);//for RayCastPick, expand Atom
    private float mouse0PressedTime;        //for Pick
    private Vector3 mouseStart;             //for Pick
    public LineRenderer[] mouseBox;         //for Pick
    static Vector3 copyBias = new Vector3(2500f, 0, 0);
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
            btn.image.color = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));
        }
        else
        {
            go.SetActive(true);
            btn.image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
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
            s.image.color = new Color((195 / 255f), (195 / 255f), (195 / 255f), (255 / 255f));
            s.interactable = true;
        }
        else
        {
            s.image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
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
        float mouse_x = (Input.mousePosition - mouseLastPos).x / Screen.width * 24;
        float mouse_y = -(Input.mousePosition - mouseLastPos).y / Screen.height * 24;
        mouseLastPos = Input.mousePosition;


        //Vector3 mouse_world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GameObject castGO = null;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
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
            if (true)
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

            if (editEnable)
            {
                //DELETE/Backspace删除原子
                if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    foreach (GameObject go in pickGO)
                    {
                        MainScr.S.CurrentStructure().atomList.Remove(go.GetComponent<Atom>());
                        Object.Destroy(go.transform.parent.gameObject);
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
                            Object.Destroy(go.transform.parent.gameObject);
                        }
                    }
                    copyGO = new List<GameObject>();

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
                    List<GameObject> pasteGO = new List<GameObject>();
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
                            Object.Destroy(go.transform.parent.gameObject);
                        }
                    }
                    copyGO = new List<GameObject>();

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
        else
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
                //鼠标左键按下，无LeftCronl: 取消所有选中的原子，开始计时
                if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftControl))
                {
                    mouse0PressedTime = Time.time;
                    mouseStart = Input.mousePosition;
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

                    //mouseBox reset
                    if (mouseBox.Length == 0)
                    {
                        mouseBox = new LineRenderer[4];
                    }
                }
                //鼠标左键按下，有LeftCronl: 不取消所有选中的原子，开始计时（增加选中的原子）
                else if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKey(KeyCode.LeftControl))
                {
                    mouse0PressedTime = Time.time;
                    mouseStart = Input.mousePosition;

                    //mouseBox reset
                    if (mouseBox.Length == 0)
                    {
                        mouseBox = new LineRenderer[4];
                    }
                }
                //按下鼠标时：按下的时间短，单选射线拾取选中的原子；按下事件长，框选中心位置在框内的原子，并更新颜色和框
                else if (Input.GetKey(KeyCode.Mouse0))
                {
                    //shout presse, Select the castGO
                    if (Time.time - mouse0PressedTime < 0.2f)
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
                    else
                    {
                        foreach (Atom a in MainScr.S.CurrentStructure().atomList)
                        {
                            Vector3 screenPos = Camera.main.WorldToScreenPoint(a.transform.position);
                            if ((screenPos.x - mouseStart.x) * (screenPos.x - Input.mousePosition.x) < 0)
                                if ((screenPos.y - mouseStart.y) * (screenPos.y - Input.mousePosition.y) < 0)
                                    if (!pickGO.Contains(a.gameObject))
                                        pickGO.Add(a.gameObject);
                        }
                        foreach (ExtendStructrue ext in MainScr.S.CurrentStructure().extendStructs)
                        {
                            if (ext == null) continue;
                            foreach (Atom a in ext.extendAtomList)
                            {
                                Vector3 screenPos = Camera.main.WorldToScreenPoint(a.transform.position);
                                if ((screenPos.x - mouseStart.x) * (screenPos.x - Input.mousePosition.x) < 0)
                                    if ((screenPos.y - mouseStart.y) * (screenPos.y - Input.mousePosition.y) < 0)
                                        if (!pickGO.Contains(a.gameObject))
                                            pickGO.Add(a.gameObject);
                            }
                        }

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


                        foreach (LineRenderer r in mouseBox)
                        {
                            if (r != null)
                            {
                                Object.Destroy(r.gameObject);
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
                //鼠标左键抬起时：确保所有选中原子的颜色，移去框
                else if (Input.GetKeyUp(KeyCode.Mouse0))
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


                    foreach (LineRenderer r in mouseBox)
                    {
                        if (r != null)
                        {
                            Object.Destroy(r.gameObject);
                        }
                    }

                    this.MeasurePickAtoms();
                }

                //如果没有LCtrl，则为常规镜头旋转、平移、缩放操作
                else if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.LeftControl)))
                {
                    // When Mouse1 / LShit+mous0 was clicked(GetKeyDown) ,check rotation mode(鼠标右键可用LShift替换)
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        //Right click of mouse: Rotation. Within tha lattice radius, 3D rotate. Beyond the radius, 2D rotate.
                        //Judge By Ray Pickup
                        if (Physics.Raycast(mouseRay, out hitInfo, 500f, 1 << LayerMask.NameToLayer("Default")))
                        {
                            //对"SturcturePrefab(Clone)"的拾取，决定旋转模式
                            this.rotationMode3d = true;
                        }
                        else
                        {
                            //2D rotate
                            this.rotationMode3d = false;
                        }
                    }
                    //when mouse1 / LShit+mous0 is pressed, rotate(鼠标右键可用LShift替换)
                    else if (Input.GetKey(KeyCode.Mouse1))
                    {
                        if (this.rotationMode3d)
                        {
                            //3D rotate
                            cameraTransform.RotateAround(cameraCenter.position, cameraTransform.up, mouse_x * 5);
                            cameraTransform.RotateAround(cameraCenter.position, cameraTransform.right, mouse_y * 5);
                        }
                        else
                        {
                            //2D rotate
                            Vector3 mouseViewport = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                            float y = (mouseViewport.y - 0.5f);// theta of mouse to center
                            float x = (mouseViewport.x - 0.5f);
                            float square = y * y + x * x;//theta to rotate
                            float degree = (y / square * mouse_x + x / square * mouse_y) * 2.4f;
                            cameraTransform.RotateAround(cameraCenter.position, cameraTransform.forward, degree);
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Mouse2) || (Input.GetKeyDown(KeyCode.LeftShift)))
                    {
                        offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    }
                    else if (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift))
                    {
                        cameraTransform.position += (offsetTemp - Camera.main.ScreenToWorldPoint(Input.mousePosition));
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
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.LeftControl))
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
                            Vector3 pickAtomAvgPosScreen = Camera.main.WorldToScreenPoint(pickAtomAvgPos);
                            float pickAtomFurthestDis = 0f;
                            foreach (GameObject ga in pickGO)
                            {
                                Vector3 vec = Camera.main.WorldToScreenPoint(ga.transform.position);
                                print(vec);
                                pickAtomFurthestDis = Mathf.Max((vec - pickAtomAvgPosScreen).magnitude, pickAtomFurthestDis);
                            }
                            pickAtomFurthestDis = Mathf.Max(100.0f, pickAtomFurthestDis);
                            if ((Input.mousePosition - pickAtomAvgPosScreen).magnitude < pickAtomFurthestDis)
                            {
                                rotationMode3d = true;
                            }
                            else
                            {
                                rotationMode3d = false;
                            }
                            //print((Input.mousePosition - pickAtomAvgPosScreen).magnitude);
                            //print(pickAtomFurthestDis);
                            //print(pickAtomAvgPosScreen);
                            //print(rotationMode3d);
                            MainScr.S.CurrentStructure().PickControlStart(pickGO);

                        }
                        //LC+鼠标右键按住时：旋转选中原子
                        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse1))
                        {
                            if (this.rotationMode3d)
                            {
                                //3D rotate
                                foreach (GameObject ga in pickGO)
                                {
                                    //3D rotate
                                    ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.up, -mouse_x * 8);
                                    ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.right, -mouse_y * 8);
                                }
                            }
                            else
                            {
                                //2D rotate
                                foreach (GameObject ga in pickGO)
                                {
                                    Vector3 mouseViewport = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                                    float y = (mouseViewport.y - 0.5f);// theta of mouse to center
                                    float x = (mouseViewport.x - 0.5f);
                                    float square = y * y + x * x;//theta to rotate
                                    float degree = (y / square * mouse_x + x / square * mouse_y) * 2.4f;
                                    ga.transform.parent.RotateAround(pickAtomAvgPos, cameraTransform.forward, -degree);
                                }
                            }
                            MainScr.S.CurrentStructure().PickControlUpdate();


                        }
                        //松开LC或鼠标右键：收尾
                        else if ((Input.GetKeyUp(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse1))
                              || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.Mouse1)))
                        {

                            MainScr.S.CurrentStructure().PickControlEnd();
                            EditSaveSturc();
                        }

                        //LC(LeftControl)+鼠标中键按下： 准备开始平移选中原子(鼠标右键可用LShift替换)
                        else if (Input.GetKey(KeyCode.LeftControl) && (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.LeftShift))
                              || Input.GetKeyDown(KeyCode.LeftControl) && (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift)))
                        {
                            offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            MainScr.S.CurrentStructure().PickControlStart(pickGO);
                        }
                        //LC(LeftControl)+鼠标中键按下： 平移选中原子(鼠标右键可用LShift替换)
                        else if (Input.GetKey(KeyCode.LeftControl) && (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift)))
                        {
                            Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offsetTemp;
                            foreach (GameObject ga in pickGO)
                            {
                                ga.transform.parent.position += vec;
                            }

                            offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                            MainScr.S.CurrentStructure().PickControlUpdate();
                        }
                        //松开LC或鼠标中键：收尾(鼠标右键可用LShift替换)
                        else if ((Input.GetKeyUp(KeyCode.LeftControl) && (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.LeftShift)))
                              || (Input.GetKey(KeyCode.LeftControl) && (Input.GetKeyUp(KeyCode.Mouse2) || Input.GetKeyUp(KeyCode.LeftShift))))
                        {
                            MainScr.S.CurrentStructure().PickControlEnd();
                            EditSaveSturc();
                        }
                    }
                }
            }
        }
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
        line.startWidth = 0.01f;//设置直线宽度
        line.endWidth = 0.05f;

        //设置指示线的起点和终点
        line.SetPosition(0, Camera.main.ScreenToWorldPoint(v1) + Camera.main.transform.forward);
        line.SetPosition(1, Camera.main.ScreenToWorldPoint(v2) + Camera.main.transform.forward);
        return line;
    }

    private void MeasurePickAtoms()
    {
        if (pickGO.Count == 1)
        {
            string st = string.Format("{0} position: {1:f4} {2:f4} {3:f4}",  pickGO[0].GetComponent<Atom>().ele, pickGO[0].transform.position.x, pickGO[0].transform.position.y, pickGO[0].transform.position.z);
            ConFun.S.printStatusInfo(st);
            //{ pickGO[0].transform.position.y}
            //);
        }
        else if (pickGO .Count == 2)
        {
            string st = string.Format("{0}-{1} distance:   {2:f4} ", pickGO[0].GetComponent<Atom>().ele, pickGO[1].GetComponent<Atom>().ele, (pickGO[0].transform.position - pickGO[1].transform.position).magnitude);
            ConFun.S.printStatusInfo(st);
        }
        else if (pickGO.Count == 3)
        {
            string st = string.Format("{0}-{1}-{2}  angle: {3:f4} ", pickGO[0].GetComponent<Atom>().ele, pickGO[1].GetComponent<Atom>().ele, pickGO[2].GetComponent<Atom>().ele,
                  Vector3.Angle(pickGO[0].transform.position - pickGO[1].transform.position, pickGO[2].transform.position - pickGO[1].transform.position));
            ConFun.S.printStatusInfo(st);       
        }
        else if (pickGO.Count == 4)
        {
            string st = string.Format("{0}-{1}-{2}-{3} torison: {4:f4} ", pickGO[0].GetComponent<Atom>().ele, pickGO[1].GetComponent<Atom>().ele, pickGO[2].GetComponent<Atom>().ele, pickGO[3].GetComponent<Atom>().ele,
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

