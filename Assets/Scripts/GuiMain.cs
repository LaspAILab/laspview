using System.Collections;
using System.Collections.Generic;

using System.Text;

using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using B83.Win32;
using System.Linq;
using System;
using TMPro;

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
    public GameObject M_SupercellWidgetPrefab;
#if !UNITY_WEBGL
    //public GameObject M_LaspInWidgetPrefab;
    //public GameObject R_LASPPanelPrefab;
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

    //public bool editEnable;          //Pick Enalble
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
    private List<AtomScript> pickAtomOrigin = new List<AtomScript>(); //for pick Atom
    private List<AtomScript> pickAtomDerived = new List<AtomScript>();
    //private List<>
    private List<BondScript> changeBond = new List<BondScript>();
    private List<BondScript> changeBondDerived = new List<BondScript>();


    private Dictionary<Vector3, AtomScript> atomScreenPosTemp = new Dictionary<Vector3, AtomScript>();
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
        GameObject m_SupercellWidget = Instantiate(M_SupercellWidgetPrefab);
#if !UNITY_WEBGL
        GameObject r_ServerPanel = Instantiate(R_ServerPanelPrefab);
        //GameObject r_LASPPanel = Instantiate(R_LASPPanelPrefab);
        //GameObject m_LaspInWidget = Instantiate(M_LaspInWidgetPrefab);
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
    public void SetUIInteract(Selectable s, bool interactable)
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

    public void CameraActicate()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        noOperationTimeSince = Time.time;
#endif
    }

    private LineRenderer mouseBoxLine(Vector3 v1, Vector3 v2)
    {
        //drawLine
        LineRenderer line;
        line = Instantiate(mouseBoxLinePrefab).GetComponent<LineRenderer>();
        //line.positionCount = 2;//设置两点
        //line.startColor = Color.white; //设置直线颜色
        //line.endColor = Color.white;
        line.startWidth = 0.002f * Camera.main.orthographicSize;//设置直线宽度
        line.endWidth = 0.002f * Camera.main.orthographicSize;

        //设置指示线的起点和终点
        line.SetPosition(0, Camera.main.ScreenToWorldPoint(v1) + Camera.main.transform.forward);
        line.SetPosition(1, Camera.main.ScreenToWorldPoint(v2) + Camera.main.transform.forward);
        return line;
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
        // Handle drop file
    }
#endif
}
