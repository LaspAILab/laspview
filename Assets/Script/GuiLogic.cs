using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ViewerNetwork;
using Lasp4Win;


public class GuiLogic : MonoBehaviour
{
    static public GuiLogic S;

    public GameObject listCellPrefab;
    public GameObject nodePrefab;
    public GameObject mouseBoxLinePrefab;
    public GameObject laspInEditorWidget;


    static public GameObject menuPanel;
    static public GameObject statusPanel;
    static public GameObject toolbarPaenl;
    static public GameObject serverPanel;
    static public GameObject laspPanel;
    static public syfPanelPara laspPanelPara;
    static public GameObject projectPanel;
    static public GameObject fileList;
    static public GameObject laspInEditorpanel;

    static public InputField laspInEditorText;
    static public InputField openbFilePath;

    public bool menuBtnStatus = false;
    public bool lclSrvrListening = false;

    //********************* VAR for MOUSE CONTROL*********************
    syfDragUI dragCom = null;
    Vector3 selectGOOffset;
    public bool rayStartOnUI = false;

    public Transform cameraTransform;
    private Transform cameraCenter;
    private Vector3 mouseLastPos;

    public bool updateBond;
    public bool pickEnable;          //Pick Enalble
    private bool rotationMode3d = false;    //for rotate
    //Local Var used for Camera Control
    private Vector3 pickAtomAvgPos;         //for pick rotate
    private Vector3 offsetTemp;             //for tranlate    
    public List<GameObject> pickGO;         //for Pick
    public GameObject expandAtom;           //for RayCastPick, expand Atom
    private Vector3 expandAtomScale = new Vector3(0.3f, 0.3f, 0.3f);//for RayCastPick, expand Atom
    private float mouse0PressedTime;        //for Pick
    private Vector3 mouseStart;             //for Pick
    public LineRenderer[] mouseBox;         //for Pick

    //************************** END  *****************************


    void Awake()
    {
        S = this;
        menuPanel = GameObject.Find("MenuPanel");
        statusPanel = GameObject.Find("StatusPanel");
        toolbarPaenl = GameObject.Find("ToolbarPanel");
        serverPanel = GameObject.Find("ServerPanel");
        laspPanel = GameObject.Find("LASPPanel");
        projectPanel = GameObject.Find("ProjectPanel");
        fileList = projectPanel.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;

        cameraTransform = Camera.main.transform;
        //use a void GameObject as cameraCenter
        cameraCenter = Instantiate(nodePrefab).GetComponent<Transform>();
        cameraCenter.gameObject.name = "NodePrefab(CameraCenter)";

    }
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_WEBGL
        laspInEditorpanel.SetActive(false);
#endif
        openbFilePath = GameObject.Find("OpenFileInputField").GetComponent<InputField>();
        SetMenuBtn();
    }

    //relate Button with GameObjects, mainly panels, that can be hidden.
    void BtnHideObject(Button btn, GameObject go = null)
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
        // Read Text.text of the Button, Search Panel GameObjct by the text, and AddListener 
        foreach (Button btn in menuPanel.GetComponentsInChildren<Button>())
        {
            string panelName = btn.GetComponentInChildren<Text>().text + "Panel";
            GameObject panel = GameObject.Find(panelName);
            btn.onClick.AddListener(() => BtnHideObject(btn, panel));
        }



        //Set Edit panel
        GameObject.Find("EditBtn").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (pickEnable) pickEnable = false;
            else pickEnable = true;
        });
        pickEnable = false;

        //Set Server panel
#if !UNITY_WEBGL
        //print default remote ip on ServerIP inputfield;
        serverPanel.transform.GetChild(1).GetChild(1).gameObject.GetComponent<InputField>().text = ConFun.S.globalPara["remote_ip"];
        //check local IP   
        NetLocalBtn1();
        //Set LaspInEditor
        //laspInEditorpanel = GameObject.Find("LaspInEditor");
        //laspInEditorText = laspInEditorpanel.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<InputField>();
        //NetEditLaspIn();
#endif


#if !UNITY_WEBGL
        //Set LASP Panel
        GameObject.Find("LaspBtn").GetComponent<Button>().onClick.AddListener(() =>
        {
            //print(laspPanel.active);
            if (laspPanel.active == true)
            {
                LaspSetPanel();

            }
        });
        laspPanelPara = laspPanel.GetComponent<syfPanelPara>();
        laspPanelPara.SetParaDict();
#endif



        // Set Panel active False
        foreach (Button btn in menuPanel.GetComponentsInChildren<Button>())
        {
            string panelName = btn.GetComponentInChildren<Text>().text + "Panel";
            GameObject panel = GameObject.Find(panelName);
            panel.SetActive(false);
        }
    }




    //*************************** CAMERA CONTROL **********************************
    //******************************** & ******************************************
    //************************** MOUSE INTERACTION *******************************+
    void Update()
    {
        

        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Mouse2) || Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                rayStartOnUI = true;

                foreach (RaycastResult r in ClickObject())
                {
                    dragCom = r.gameObject.GetComponent<syfDragUI>();
                    //When GameObject contain syfDragUIComponent, it can be Dragged
                    if (dragCom!=null)
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

        //To move a Draggable Widget(GameObject that syfDragUI component poiting to)
        if (rayStartOnUI && Input.GetKey(KeyCode.Mouse2) && dragCom != null)
        {
            Vector3 screemScale = GuiLogic.S.gameObject.transform.localScale;
            Vector3 widgetPos = Input.mousePosition + selectGOOffset;

            //MakeSure Draggable widget is still in the frame
            //A east home-made rectangle position limiter. If need update, you can write a UICollider youself. Use para in RectTransform
            widgetPos.y = Mathf.Max(widgetPos.y, dragCom.lowerBound * screemScale.y);
            widgetPos.y = Mathf.Min(widgetPos.y, Screen.height - dragCom.upperBound * screemScale.y);
            widgetPos.x = Mathf.Max(widgetPos.x, dragCom.leftBound * screemScale.x);
            widgetPos.x = Mathf.Min(widgetPos.x, Screen.width - dragCom.rightBound * screemScale.x);
            dragCom.draggedGameObject.transform.position = widgetPos;
        }
        else if (rayStartOnUI == false)
        {
            MouseOnScene();
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

    public void MouseOnScene() //摄像机围绕目标旋转操作
    {

        //transform.RotateAround(cameraCenter.position, Vector3.up, speed * Time.deltaTime); //摄像机围绕目标旋转
        //float mouse_x = Input.GetAxis("Mouse X");//获取鼠标X轴移动
        //float mouse_y = -Input.GetAxis("Mouse Y");//获取鼠标Y轴移动
        //print(mouse_x);
        if (mouseLastPos == null)
        {
            mouseLastPos = Input.mousePosition;
        }
        float mouse_x = (Input.mousePosition - mouseLastPos).x/Screen.width*24;
        float mouse_y = -(Input.mousePosition - mouseLastPos).y/Screen.height*24;
        mouseLastPos = Input.mousePosition;

        Vector3 mouse_world = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //************************** Pick Atom & Control  ******************************
        if (pickEnable)
        {
            GameObject castGO;
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if(Physics.Raycast(mouseRay,out hitInfo, 500f, 1 << LayerMask.NameToLayer("TransparentFX")))
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
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(a.nodeTrans.position);
                        if ((screenPos.x - mouseStart.x) * (screenPos.x - Input.mousePosition.x) < 0)
                            if ((screenPos.y - mouseStart.y) * (screenPos.y - Input.mousePosition.y) < 0)
                                if (!pickGO.Contains(a.gameObject))
                                    pickGO.Add(a.gameObject);
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
            }
            if (pickGO.Count != 0)
            {
                //LC(LeftControl)+鼠标右键按下： 准备开始旋转选中原子(鼠标右键可用LShift替换)
                if (Input.GetKey(KeyCode.LeftControl) && (Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.LeftShift)))
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
                        pickAtomFurthestDis = Mathf.Max((vec - pickAtomAvgPosScreen).magnitude, pickAtomFurthestDis);
                    }
                    if ((Input.mousePosition - pickAtomAvgPosScreen).magnitude < 1.5f * pickAtomFurthestDis)
                    {
                        rotationMode3d = true;
                    }
                    else
                    {
                        rotationMode3d = false;
                    }
                    MainScr.S.CurrentStructure().PickControlStart(pickGO);

                }
                //LC+鼠标右键按住时：旋转选中原子(鼠标右键可用LShift替换)
                else if (Input.GetKey(KeyCode.LeftControl) && (Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.LeftShift)))
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
                //松开LC或鼠标右键：收尾(鼠标右键可用LShift替换)
                else if ((Input.GetKeyUp(KeyCode.LeftControl) && (Input.GetKey(KeyCode.Mouse1)|| Input.GetKey(KeyCode.LeftShift)))
                      || (Input.GetKey(KeyCode.LeftControl) && (Input.GetKeyUp(KeyCode.Mouse1)|| Input.GetKeyUp(KeyCode.LeftShift))))
                {

                    MainScr.S.CurrentStructure().PickControlEnd();
                }

                //LC(LeftControl)+鼠标中键按下： 准备开始平移选中原子
                else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Mouse2))
                {
                    offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    MainScr.S.CurrentStructure().PickControlStart(pickGO);
                }
                //LC(LeftControl)+鼠标中键按下： 平移选中原子
                else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse2))
                {
                    Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offsetTemp;
                    foreach (GameObject ga in pickGO)
                    {
                        ga.transform.parent.position += vec;
                    }

                    offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                    MainScr.S.CurrentStructure().PickControlUpdate();
                }
                //松开LC或鼠标中键：收尾
                else if ((Input.GetKeyUp(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse2))
                      || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.Mouse2)))
                {
                    MainScr.S.CurrentStructure().PickControlEnd();
                }
                //DELETE/Backspace删除原子
                else if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    foreach(GameObject go in pickGO)
                    {
                        MainScr.S.CurrentStructure().atomList.Remove(go.GetComponent<Atom>());
                        Object.Destroy(go.transform.parent.gameObject);
                    }
                    MainScr.S.CurrentStructure().AddAllBond();
                    pickGO.Clear();
                }
            }
        }
        //Only process one kind of Input information at one time
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.LeftControl)) return;

        //************************** Pick Atom & Control End ******************************
        //*************************** Gernal Camera Control *******************************


        // When Mouse1 / LShit+mous0 was clicked(GetKeyDown) ,check rotation mode(鼠标右键可用LShift替换)
        if (Input.GetKeyDown(KeyCode.Mouse1) || (Input.GetKeyDown(KeyCode.LeftShift)))
        {
            //Right click of mouse: Rotation. Within tha lattice radius, 3D rotate. Beyond the radius, 2D rotate.
            //Judge By Ray Pickup
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hitInfo;
            hitInfo = Physics.RaycastAll(mouseRay, 500f, 1 << LayerMask.NameToLayer("Default"));

            if (hitInfo.Length != 0)
            {
                //对"SturcturePrefab(Clone)"的拾取，决定旋转模式
                this.rotationMode3d = false;
                foreach (RaycastHit r in hitInfo)
                {
                    this.rotationMode3d = true;
                    break;
                }
            }
            else
            {
                //2D rotate
                this.rotationMode3d = false;
            }
        }
        //when mouse1 / LShit+mous0 is pressed, rotate(鼠标右键可用LShift替换)
        else if (Input.GetKey(KeyCode.Mouse1)|| (Input.GetKey(KeyCode.LeftShift)))
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
                float degree = (y / square * mouse_x + x / square * mouse_y)*2.4f;
                cameraTransform.RotateAround(cameraCenter.position, cameraTransform.forward, degree);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetKey(KeyCode.Mouse2))
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

    public void FocusCameraCenter()
    {
        Vector3 vec = MainScr.S.CurrentStructure().GetCellTransfer(1, 1, 1) / 2;
        cameraCenter.position = vec;
        cameraTransform.position = vec;
        cameraTransform.Translate(Vector3.back.normalized * vec.magnitude * 4);
        Camera.main.orthographicSize = vec.magnitude;
    }

    //****************** CAMERA CONTROL & MOUSE INTERACTION ***********************
    //****************************** END ******************************************


    //**************************** Player Bar **************************************

    public void OnPlayerFocusBtnClicked()
    {
        this.FocusCameraCenter();
    }

    public void OnPlayerHeadBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure(0));
    }

    public void OnPlayeTailBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        ArcStructures arc = MainScr.S.CurrentArc();
        PlayerBtnChecked(arc.DrawStructure(arc.lenStructures - 1));
    }

    public void OnPlayeForwardBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure('+'));
    }

    public void OnPlayeBackBtnClicked()
    {
        //draw the first sturcture, then check the player button status
        PlayerBtnChecked(MainScr.S.CurrentArc().DrawStructure('-'));
    }

    public void PlayerBtnChecked((bool lower, bool upper) playStatus)
    {
        //check if the player Button should be de-activate or re-activate
        if (playStatus.lower == true)
        {
            toolbarPaenl.transform.GetChild(0).gameObject.GetComponent<Button>().interactable = true;
            toolbarPaenl.transform.GetChild(1).gameObject.GetComponent<Button>().interactable = true;
        }
        else
        {
            toolbarPaenl.transform.GetChild(0).gameObject.GetComponent<Button>().interactable = false;
            toolbarPaenl.transform.GetChild(1).gameObject.GetComponent<Button>().interactable = false;
        }
        if (playStatus.upper == true)
        {
            //print(toolbarPaenl.transform.GetChild(1));
            toolbarPaenl.transform.GetChild(3).gameObject.GetComponent<Button>().interactable = true;
            toolbarPaenl.transform.GetChild(4).gameObject.GetComponent<Button>().interactable = true;
        }
        else
        {
            toolbarPaenl.transform.GetChild(3).gameObject.GetComponent<Button>().interactable = false;
            toolbarPaenl.transform.GetChild(4).gameObject.GetComponent<Button>().interactable = false;
        }
    }

    //**************************** Player Bar End **************************************

    //**************************** LASP Panel **************************************

    public void LaspSetPanel()
    {
        //Set Lasp Panel, Mainly Selectable Item in Prefav dir
        Dropdown prefabDirDp = laspPanelPara.paraDict["prefab dir"].GetComponentInParent<Dropdown>();
        //print(laspPanelPara.paraDict["Prefab dir"].transform);
        List<string> prefabNames = new List<string>(Directory.GetDirectories("./LaspPrefab"));
        List<Dropdown.OptionData> prefabDrop = new List<Dropdown.OptionData>();
        foreach (string str in prefabNames)
        {
            string[] spl = str.Split('\\');
            prefabDrop.Add(new Dropdown.OptionData(spl[spl.Length - 1]));
        }
        prefabDirDp.options = prefabDrop;
        this.LaspChangePrefab();
    }

    public void LaspChangePrefab()
    {
        //Open target lasp.in
        string dirName = laspPanelPara.paraDict["prefab dir"].GetComponentInParent<Dropdown>().captionText.text;
        string[] lines = File.ReadAllLines("./LaspPrefab/" + dirName + "/lasp.in");

        //Read lasp.in
        Dictionary<string, int> uiPara = new Dictionary<string, int>
                { { "potential", 0 } ,{ "explore_type",0 },{ "%block netinfo",0 },{ "run_type",0 }, {"ssw.sswsteps",0 } };
        int i = 0;
        StringBuilder otherPara = new StringBuilder { };

        //while (!Regex.IsMatch(lines[i], @"\#*AutoGenerate*\#*") && i < lines.Length)
        //{
        //    otherPara.Append(lines[i]);
        //    i++;
        //}
        ////print(i);
        //while (!Regex.IsMatch(lines[i], @"\#*End*\#*") && i < lines.Length)
        //{
        for(i = 0; i< lines.Length; i++)
        {
            string[] para = ConFun.S.PySplit(lines[i].ToLower());
            if (uiPara.ContainsKey(para[0]) || (para[0] == "%block" && para[1] == "netinfo"))
            {
                if (para[0] == "potential")
                {
                    Dropdown dp = laspPanelPara.paraDict["potential"].GetComponent<Dropdown>();
                    if (para[1] == "nn")
                    {
                    }
                    else
                    {
                        ConFun.S.printStatusInfo("Only NN is allowed for potential");
                    }
                }
                else if (para[0] == "explore_type")
                {
                    Dropdown dp = laspPanelPara.paraDict["explore_type"].GetComponent<Dropdown>();
                    if (para[1] == "ssw")
                    {
                    }
                    else
                    {
                        ConFun.S.printStatusInfo("Only SSW is allowed for explore_type");
                    }
                }
                else if (para[0] == "run_type")
                {
                    Dropdown dp = laspPanelPara.paraDict["run_type"].GetComponent<Dropdown>();
                    Dictionary<string, int> available = new Dictionary<string, int> { { "5", 0 }, { "15", 1 }, { "2", 2 }, { "12", 3 }, { "1", 4 }, { "11", 5 } };
                    if (available.ContainsKey(para[1]))
                    {
                        dp.value = available[para[1]];
                    }
                    else
                    {
                        ConFun.S.printStatusInfo("Only 5, 15, 2, 12, 1, 11 is allowed for run_type");
                    }
                }
                else if (para[0] == "%block" && para[1] == "netinfo")
                {
                    LaspSetNNPot();
                    Dropdown dp = laspPanelPara.paraDict["nn pot"].GetComponent<Dropdown>();
                    i++;

                    para = ConFun.S.PySplit(lines[i].ToLower());
                    int j = 0;
                    for (; j < dp.options.Count; j++)
                    {
                        if (dp.options[j].text.ToLower() == para[2])
                        {
                            dp.value = j;
                            break;
                        }
                    }
                    if (j == dp.options.Count)
                    {
                        ConFun.S.printStatusInfo("NN pot not available in ./LaspNNPot");
                    }
                    while (!Regex.IsMatch(lines[i], @"%endblock netinfo") && i < lines.Length)
                    {
                        i++;
                    }
                }
                else if (para[0] == "ssw.sswsteps")
                {
                    InputField fld = laspPanelPara.paraDict["ssw.sswsteps"].GetComponent<InputField>();
                    fld.text = para[1];
                }
            }
            else
            {
                if (!Regex.IsMatch(lines[i], @"^\#\#\#(.)*\#\#\#$"))
                {
                    otherPara.AppendLine(lines[i]);
                }
            }
        }
        //print(otherPara.ToString());
        //LaspWriteLaspin("./LaspPrefab/" + dirName + "/lasp.in", otherPara);

    }

    public void LaspSetNNPot()
    {
        //Set Lasp Panel, Mainly Selectable Item in Prefav dir
        Dropdown prefabDirDp = laspPanelPara.paraDict["nn pot"].transform.GetComponentInParent<Dropdown>();
        //print(laspPanelPara.paraDict["NN Pot"]);
        List<string> prefabNames = new List<string>(Directory.GetFiles("./LaspNNPot"));
        List<Dropdown.OptionData> prefabDrop = new List<Dropdown.OptionData>();
        foreach (string str in prefabNames)
        {
            string[] spl = str.Split('\\');
            prefabDrop.Add(new Dropdown.OptionData(spl[spl.Length - 1]));
        }
        prefabDirDp.options = prefabDrop;
    }

    public void LaspWriteLaspin(string filename = null, StringBuilder otherPara = null)
    {
        
        if (otherPara == null)
        {
            string dirName = laspPanelPara.paraDict["prefab dir"].GetComponentInParent<Dropdown>().captionText.text;
            string[] lines = File.ReadAllLines("./LaspPrefab/" + dirName + "/lasp.in");
            otherPara = new StringBuilder { };
            //Read lasp.in
            Dictionary<string, int> uiPara = new Dictionary<string, int>
                { { "potential", 0 } ,{ "explore_type",0 },{ "%block netinfo",0 },{ "run_type",0 }, {"ssw.sswsteps",0 } };
            int i = 0;
            for (i = 0; i < lines.Length; i++)
            {
                string[] para = ConFun.S.PySplit(lines[i].ToLower());
                para[0] = para[0];
                if (uiPara.ContainsKey(para[0]) || (para[0] == "%block" && para[1] == "netinfo"))
                {
                    if (para[0] == "%block" && para[1] == "netinfo")
                    {
                        while (!Regex.IsMatch(lines[i], @"%endblock netinfo") && i < lines.Length)
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    if (!Regex.IsMatch(lines[i], @"^\#(.)*\#$"))
                    {
                        otherPara.AppendLine(lines[i]);
                        //print(lines[i]);
                    }
                }
            }
        }

        StringBuilder outputlines = new StringBuilder { };
        outputlines.AppendLine("######AutoGernerate######");
        foreach (string key in laspPanelPara.paraDict.Keys)
        {
            if (key == "prefab dir") continue;
            else if (key == "nn pot")
            {
                outputlines.AppendLine("%block netinfo");
                string name = laspPanelPara.paraDict[key].GetComponentInChildren<Dropdown>().captionText.text;
                foreach (Match ele in Regex.Matches(name.Replace(".pot", ""), @"([A-Z][a-z]?)"))
                {
                    outputlines.Append("    " + ele + "   " + name + "\n");
                }
                outputlines.AppendLine("%endblock netinfo");
            }
            else if (laspPanelPara.paraDict[key].GetComponentInChildren<Dropdown>() != null)
            {
                outputlines.Append(key+ "    " + laspPanelPara.paraDict[key].GetComponentInChildren<Dropdown>().captionText.text+"\n");
            }
            else if (laspPanelPara.paraDict[key].GetComponentInChildren<InputField>() != null)
            {
                outputlines.Append(key + "    " + laspPanelPara.paraDict[key].GetComponentInChildren<InputField>().text + "\n");
            }
        }
        outputlines.Append("######EndGernerate######\n");

        outputlines.Append(otherPara);


        if (filename == null)
        {
            filename = "./LaspPrefab/" + laspPanelPara.paraDict["prefab dir"].GetComponentInChildren<InputField>().text + "/lasp.in";
        }
        File.WriteAllText(filename, outputlines.ToString());
    }

    public void LaspEditLaspIn()
    {
        Button btn = laspPanel.transform.GetChild(2).GetChild(4).gameObject.GetComponent<Button>();
        laspInEditorpanel = Instantiate(laspInEditorWidget);
        laspInEditorpanel.transform.SetParent(GameObject.Find("Canvas").transform);
        laspInEditorpanel.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        
        laspInEditorText = laspInEditorpanel.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<InputField>();
        string dirName = laspPanelPara.paraDict["prefab dir"].GetComponentInParent<Dropdown>().captionText.text;
        string lines = File.ReadAllText("./LaspPrefab/" + dirName + "/lasp.in");

        
        btn.image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
        laspInEditorText.text = lines;
        laspInEditorpanel.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "./"+dirName + "/lasp.in";
        laspInEditorpanel.transform.GetChild(0).GetChild(1).GetComponent<Button>().onClick.AddListener(() =>
        {    
            File.WriteAllText("./LaspPrefab/" + dirName + "/lasp.in", laspInEditorText.text);
        });
        laspInEditorpanel.transform.GetChild(0).GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
        {
            btn.interactable = true;
            btn.image.color = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));
            Object.Destroy(laspInEditorpanel);
        });
    }

    public void LaspRun()
    {
        
        if (false == System.IO.Directory.Exists("./LaspRun"))
        {
            Directory.CreateDirectory("./LaspRun");
        }
        //creat LASP Work Directory
        string targetDir = "./LaspRun/" + string.Format("{0:d5}-{1:d}", Random.Range(10000, 99999),
                System.DateTime.Now.ToString("MMddhhmmss"))+"/";
        string prefabDir = "./LaspPrefab/" + laspPanelPara.paraDict["prefab dir"].GetComponentInParent<Dropdown>().captionText.text;
        Directory.CreateDirectory(targetDir);

        //Copy prefab / lasp.in
        string[] fileList = Directory.GetFiles(prefabDir, "*");
        foreach (string f in fileList)
        {
            // Remove path from the file name.
            print(f);
            string fName = f.Substring(prefabDir.Length + 1);
            print(fName);
            //Use the Path.Combine method to safely append the file name to the path.
            // Will overwrite if the destination file already exists.
            File.Copy(Path.Combine(prefabDir, fName), Path.Combine(targetDir, fName), true);
        }

        //cp NN Pot
        string pot = "";
        string[] lines = File.ReadAllLines(prefabDir + "/lasp.in");
        for (int i = 0; i < lines.Length; i++)
        {
            string[] para = ConFun.S.PySplit(lines[i]);
            if (para[0] == "%block" && para[1] == "netinfo")
            {
                i++;
                pot = ConFun.S.PySplit(lines[i])[2];
                break;
            }
        }
        print(pot);
        File.Copy(Path.Combine("./LaspNNPot", pot), Path.Combine(targetDir, pot), true);

        //Write current arc
        string arc = MainScr.S.CurrentArc().ArcWrite();
        File.WriteAllText(Path.Combine(targetDir, "input.arc"), arc);

        //run lasp
        LaspWinProgress pro = new LaspWinProgress();
        pro.ExecuteProgram(args:targetDir+"> output.txt 2>&1");

    }

    //**************************** LASP Panel End **************************************



#if !UNITY_WEBGL
    //**************************** Server Panel **************************************

    //Description of three function of NetLocalBtn1/NetLocalBtn1/NetLocalBtnChange
    //when lclSrvrListening is true, Local Server is listening:
    //      Local IP DropDown = not interactable
    //      Btn1.text  = Stop Listening     Btn2 = not interactable     Btn1.text = Listenning...
    //when lclSrvrListening is false, Local Server is not working
    //      Local IP DropDown = interactable
    //      Btn1.text  = Check Local IP     Btn2 = interactable         Btn2.text = Start Listing
    public void NetLocalBtn1()
    {
        if (lclSrvrListening == false)
        {
            //when Btn1.text = "Check Local IP"
            Dropdown localAddrDp = serverPanel.GetComponentInChildren<Dropdown>();
            List<string> addrString = MainScr.S.localServer.GetIPAddress();
            List<Dropdown.OptionData> addrDrop = new List<Dropdown.OptionData>();
            foreach (string str in addrString)
            {
                addrDrop.Add(new Dropdown.OptionData(str));
            }
            localAddrDp.options = addrDrop;
        }
        else
        {
            //when Btn text = "Stop Listening"
            lclSrvrListening = false;
            MainScr.S.localServer.Quit2();
            //NetLocalBtnChange is called in LocalServer.Quit(), like a callback function
        }

    }
    public void NetLocalBtn2()
    {
        //Btn of StartListening
        if (lclSrvrListening == false)
        {
            Dropdown localAddrDp = serverPanel.GetComponentInChildren<Dropdown>();
            if (localAddrDp.options.Count == 0) return;
            string ip = localAddrDp.options[localAddrDp.value].text;
            //ip = "127.0.0.1";//for test
            MainScr.S.localServer.StartServer(ip);
            lclSrvrListening = true;
            NetLocalBtnChange();
        }
    }
    public void NetLocalBtnChange()
    {
        Button Btn1 = serverPanel.transform.GetChild(2).GetChild(0).GetComponent<Button>();
        Button Btn2 = serverPanel.transform.GetChild(2).GetChild(1).GetComponent<Button>();
        Dropdown localAddrDp = serverPanel.GetComponentInChildren<Dropdown>();
        if (lclSrvrListening == true)
        {
            Btn1.GetComponentInChildren<Text>().text = "Stop Listening";
            Btn2.GetComponentInChildren<Text>().text = "Listening...";
            Btn2.interactable = false;
            localAddrDp.interactable = false;
        }
        else
        {
            Btn1.GetComponentInChildren<Text>().text = "Check Local IP";
            Btn2.GetComponentInChildren<Text>().text = "Start Listening";
            Btn2.interactable = true;
            localAddrDp.interactable = true;
        }
    }

    async public void NetCheckIP2Remote()
    {
        //Btn of "Send Local IP"
        Button serverBtn = serverPanel.transform.GetChild(3).GetChild(0).gameObject.GetComponent<Button>();
        serverBtn.interactable = false;
        LocalClient client = gameObject.AddComponent<LocalClient>();
        string ip = serverPanel.transform.GetChild(1).GetComponentInChildren<InputField>().text;
        string str = null;
        await Task.Run(() =>
        {
            if (client.StartClient(ip) == true)
            {
                StringBuilder msgList = new StringBuilder("CheckIP=" + client.GetIPAddress()[0].ToString());
                msgList = client.AddTag(msgList, "sig");
                msgList = client.AddTag(msgList, "laspview", "user=" + ConFun.S.globalPara["user"]);
                str = client.SynSendMsg(msgList.ToString());
                
            }
        });
        if (str == null)
        {
            ConFun.S.printStatusInfo("Local IP has been sent.");
        }
        else
        {
            ConFun.S.printStatusInfo(str);
        }

        Object.Destroy(client);
        serverBtn.interactable = true;

    }
    //public void CheckIP2RemoteAsyn1()
    //{
    //    LocalClient client = new LocalClient();
    //    if (client.StartClient()== true)
    //    {
    //        StringBuilder msgList = client.AddTag(new StringBuilder("CheckIP"), "sig");
    //        msgList = client.AddTag(msgList, "laspview", "user=shiyf");
    //        client.AsynSendMsg(msgList.ToString());
    //        print("IP check sigma has been sent to Remote Server");
    //    }

    //}

    async public void NetSendStructure()
    {
        string text = MainScr.S.CurrentArc().ArcWrite();

        //Btn of "Send Structures"
        Button serverBtn = serverPanel.transform.GetChild(3).GetChild(1).gameObject.GetComponent<Button>();
        serverBtn.interactable = false;
        LocalClient client = gameObject.AddComponent<LocalClient>();
        string ip = serverPanel.transform.GetChild(1).GetComponentInChildren<InputField>().text;
        string str = null;
        string attr = string.Format("name={0:d5}-{1:d}.arc", Random.Range(10000, 99999),
                     System.DateTime.Now.ToString("MMddhhmmss"));
        await Task.Run(() =>
        {

            if (client.StartClient(ip) == true)
            {
                StringBuilder msgList = new StringBuilder(text);
                msgList = client.AddTag(msgList, "file", attr);
                msgList = client.AddTag(msgList, "laspview", "user=" + ConFun.S.globalPara["user"]);

                str = client.SynSendMsg(msgList.ToString());
            }
        });
        if (str == null)
        {
            ConFun.S.printStatusInfo(attr + " has been sent.");
        }
        else
        {
            ConFun.S.printStatusInfo(str);
        }

        Object.Destroy(client);
        serverBtn.interactable = true;

    }

    //public void NetEditLaspIn()
    //{

    //    Button btn = serverPanel.transform.GetChild(4).GetChild(0).gameObject.GetComponent<Button>();
        

    //    if (laspInEditorpanel.activeSelf)
    //    {
    //        laspInEditorpanel.SetActive(false);
    //        btn.image.color = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));
    //    }
    //    else
    //    {
    //        laspInEditorpanel.SetActive(true);
    //        btn.image.color = new Color((144 / 255f), (144 / 255f), (144 / 255f), (255 / 255f));
    //        laspInEditorText.text = File.ReadAllText(ConFun.S.globalPara["laspin_default"]);
    //    }
    //}

    //async public void NetSendLaspIn()
    //{
    //    string text = laspInEditorText.text;

    //    //Btn of "Send LaspIn"
    //    Button serverBtn = serverPanel.transform.GetChild(4).GetChild(1).gameObject.GetComponent<Button>();
    //    serverBtn.interactable = false;
    //    LocalClient client = gameObject.AddComponent<LocalClient>();
    //    string ip = serverPanel.transform.GetChild(1).GetComponentInChildren<InputField>().text;
    //    string str = null;
    //    string attr = string.Format("name={0:d5}-{1:d}.laspin", Random.Range(10000, 99999),
    //         System.DateTime.Now.ToString("MMddhhmmss"));
    //    await Task.Run(() =>
    //    {
    //        if (client.StartClient(ip) == true)
    //        {
    //            StringBuilder msgList = new StringBuilder(text);

    //            msgList = client.AddTag(msgList, "file", attr);
    //            msgList = client.AddTag(msgList, "laspview", "user="+ConFun.S.globalPara["user"]);

    //            str = client.SynSendMsg(msgList.ToString());

    //        }
    //    });
    //    if (str == null)
    //    {
    //        ConFun.S.printStatusInfo("lasp.in has been sent.");
    //    }
    //    else
    //    {
    //        ConFun.S.printStatusInfo(str);
    //    }

    //    Object.Destroy(client);
    //    serverBtn.interactable = true;
    //}

    //public void NetEditLaspInSave()
    //{
    //    File.WriteAllText(ConFun.S.globalPara["laspin_default"], laspInEditorText.text);
    //}

    //public void NetEditLaspInQuit()
    //{
    //    Button btn = serverPanel.transform.GetChild(4).GetChild(0).gameObject.GetComponent<Button>();
    //    laspInEditorpanel.SetActive(false);
    //    btn.image.color = new Color((203 / 255f), (203 / 255f), (203 / 255f), (203 / 255f));
    //}

    //**************************** Server Panel  End **************************************
#endif
    //**************************** Project Panel **************************************
    public void FileListAddCell(string filename)
    {
        GameObject newCell = Instantiate(listCellPrefab);
        newCell.transform.SetParent(fileList.transform);
        newCell.transform.localScale = Vector3.one;
        newCell.GetComponentInChildren<Text>().text = filename;
        newCell.GetComponent<Button>().onClick.AddListener(() => FileListChangeArc(newCell));
    }
    public void FileListPopCell(int i)
    {
        GameObject popCell = fileList.transform.GetChild(i).gameObject;
        Object.Destroy(popCell);
    }

    public void FileListChangeArc(GameObject obj)
    {
        //called by 
        int index = obj.transform.GetSiblingIndex();
        MainScr.S.DrawArc(index);
    }

    public void FileOpen()
    {
        MainScr.S.AddArc(openbFilePath.text);
    }

    public void FileSave()
    {
        string str = MainScr.S.CurrentArc().ArcWrite();
       File.WriteAllText(openbFilePath.text,str);
    }

    //**************************** Project Panel  End **************************************



    public void EditSaveSturc()
    {
        foreach(Atom a in MainScr.S.CurrentStructure().atomList)
        {
            a.cdnt = a.transform.parent.position;
        }
    }
}

