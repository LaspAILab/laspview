//Class for a SERIES of structures from a SINGLE arc file
//Manage class Structure, so that it can played like animation
//Interact with GUI


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;


public class ArcStructures : MonoBehaviour
{
    public GameObject StructurPrefabVar;
    //public Transform cameraTransform;
    private Transform cameraCenter;

    public string filename;
    public int currentStr;
    public List<Structure> structureList;
    public int lenStructures = 0;
    public Dictionary<string, float> modelPara;


    ////Local Var used for Camera Control
    //private bool rotationMode3d = false;    //for rotate
    //private Vector3 offsetTemp;             //for tranlate    
    //public bool pickEnable = true;          //Pick Enalble
    //public List<GameObject> pickAtom;       //for Pick
    //public GameObject expandAtom;           //for RayCastPick
    //private Vector3 expandAtomScale = new Vector3(0.3f, 0.3f, 0.3f);//for RayCastPick
    //private float mouse0PressedTime;        
    //private Vector3 mouseStart;
    //public GameObject mouseBoxLinePrefab;
    //public LineRenderer[] mouseBox;

    void Awake()
    {
        this.transform.position = Vector3.zero;


        cameraCenter = Instantiate(StructurPrefabVar).GetComponent<Transform>();
        cameraCenter.SetParent(this.transform);//use a void GameObject as cameraCenter
       


        this.modelPara = new Dictionary<string, float> { { "ball_radius", 0.5f }, { "bound_radius", 1.0f }, {"bound_boundry", 0.63f} };
    }


    public Structure CurrentStructure()
    {
        return structureList[currentStr];
    }

    public (bool, bool) DrawStructure(int i)
    {
        //print(currentStr);
        foreach (Bond b in structureList[currentStr].bondList)
        {
            //Destroy all bond before setActive False to save memory
            UnityEngine.Object.Destroy(b.gameObject);
        }
        structureList[currentStr].bondList.Clear();
        structureList[currentStr].gameObject.SetActiveRecursively (false);

        structureList[i].DrawModel(modelPara);
        currentStr = i;
        ConFun.S.printStructureInfo("Structure: "+(currentStr+1)+" of "+structureList.Count+"\t\t Energy  =  "+ structureList[currentStr].energy);
        GuiLogic.S.FocusCameraCenter();

        bool upper = true;
        bool lower = true;
        //used for PlayerBtnChecked()
        if (currentStr <= 0)
            lower = false;  //when at lower bound, false means deactivate lower player button
        if (currentStr >= lenStructures-1)
            upper = false;  //when at upper bound
        return (lower, upper);
    }

    public (bool, bool) DrawStructure(char ch)
    {
        //Only Allowed  '+', '-' as argument
        //an easy interface for forward & back Button
        if (ch is '+')
            return this.DrawStructure(currentStr + 1);
        if (ch is '-')
            return this.DrawStructure(currentStr - 1);
        return (false, false);
    }

    private Structure AddSturcture()
    {
        Structure  newStru = Instantiate(StructurPrefabVar).GetComponent<Structure>();
        structureList.Add(newStru);
        newStru.transform.SetParent(this.transform,false);
        lenStructures += 1;
        return newStru;
        
    }

    //async public void ArcRead(string filename, string[] lines, bool noE = false, string fname = null)
    //{
    //    await Task.Run(() =>
    //    {
    //        _ArcRead(filename, lines, noE, fname);
    //    });
    //}

    public void ArcRead(string filename, string[] lines, bool noE = false, string fname = null)
    {   //Initilization function outside Unity3d frame. Need to be call soon after Instantiation.
        this.filename = filename;
        currentStr = -1;
        int startingPoint = 0;
        //若文件行数大于(300个原子，100个结构，约等于30000行)，只执行25000个原子的结构
        if (lines.Length > 30000)
        {
            ConFun.S.printStatusInfo(filename + "is too long. Only last several structures is loaded.");
            startingPoint = lines.Length - 8000;

            for (; startingPoint < lines.Length - 1; startingPoint++)
            {
                print(3);
                if (lines[startingPoint].Contains("Energy")) break;
            }


        }
        print(lines[startingPoint]);
        try
        {
            for (int i = startingPoint; i <= lines.Length - 1; i++)
            {
                // add Structure
                if (!noE)
                {
                    if (lines[i].Contains("Energy") || lines[i].Contains("IS") || lines[i].Contains("TS") || (lines[i].Contains("SSW") && !lines[i].Contains("Str")))
                    {
                        currentStr += 1;
                        if (!lines[i].Contains("****"))
                        {
                            AddSturcture();
                            structureList[currentStr].energy = float.Parse(ConFun.S.PySplit(lines[i])[4]);
                            structureList[currentStr].conv = float.Parse(ConFun.S.PySplit(lines[i])[2]);
                            try
                            {
                                structureList[currentStr].pathid = int.Parse(ConFun.S.PySplit(lines[i])[1]);
                            }
                            catch (System.Exception)
                            {
                                structureList[currentStr].pathid = 0;
                            }
                        }
                        else
                        {
                            AddSturcture();
                            structureList[currentStr].conv = 9999.0F;
                        }

                    }
                    else if (lines[i].Contains("Str"))
                    {
                        if (!lines[i].Contains("****"))
                        {
                            AddSturcture();
                            structureList[currentStr].energy = float.Parse(ConFun.S.PySplit(lines[i])[4]);
                        }
                        else
                        {
                            AddSturcture();
                        }
                        currentStr += 1;
                        structureList[currentStr].nminimum = int.Parse(ConFun.S.PySplit(lines[i])[1]);
                        structureList[currentStr].numstr = int.Parse(ConFun.S.PySplit(lines[i])[2]);
                    }
                    else if (lines[i].Contains("React") || lines[i].Contains("TS"))
                    {
                        currentStr += 1;
                        AddSturcture();
                        structureList[currentStr].energy = float.Parse(ConFun.S.PySplit(lines[i])[4]);
                        structureList[currentStr].numstr = int.Parse(ConFun.S.PySplit(lines[i])[2]);
                        structureList[currentStr].nopathway = int.Parse(ConFun.S.PySplit(lines[i])[1]);
                        if (lines[i].Contains("React")) structureList[currentStr].ts = false;
                        else structureList[currentStr].ts = true;
                    }
                    else if (lines[i].Contains("CAR File"))
                    {
                        AddSturcture();
                        currentStr += 1;
                    }
                }
                else
                {
                    if (lines[i].Contains("DATE"))
                    {
                        AddSturcture();
                        currentStr += 1;
                    }
                }

                //complete information  in single structure
                if (lines[i].Contains("PBC") && !(lines[i].Contains("ON") || lines[i].Contains("OFF")))
                {
                    structureList[currentStr].AddLat(lines[i]);
                }
                else if (lines[i].Contains("CORE"))
                {
                    structureList[currentStr].AddAtom(lines[i]);
                }
            }
        }
        catch (Exception ex)
        {
            ConFun.S.printStatusInfo("Error: Read arc error. Maybe file is too long." + ex.Message);
            print(currentStr);
            GameObject.Destroy(structureList[currentStr].gameObject);
        }


        for (int i = 0; i < currentStr + 1; i++)
        {
            structureList[i].id = i;
        }
    }

    public string ArcWrite(int startStruc , int count = 0)
    {
        StringBuilder lines = new StringBuilder ();
        lines.Append("!BIOSYM archive 2\nPBC = ON\n");
        int id = 0;
        for(int i = startStruc ;i<= startStruc+count; i++, id++)
        {
            Structure struc = structureList[i];
            lines.Append(string.Format("                      Energy{0,10:d}{1,16:f4}{2,18:f6}\n", id, 0.0f, struc.energy));
            lines.Append("!DATE\n");
            lines.Append(string.Format("PBC{0,14:f8}{1,14:f8}{2,14:f8}{3,14:f8}{4,14:f8}{5,14:f8}\n", 
                                        struc.abc[0],struc.abc[1],struc.abc[2],struc.abc[3],struc.abc[4],struc.abc[5]));
            for(int h = 0; h < struc.atomList.Count; h++)
            {
                Atom at = struc.atomList[h];
                lines.Append(string.Format("{0,-2}{1,15:f9}{2,15:f9}{3,15:f9}", at.ele, at.cdnt[0], at.cdnt[1], at.cdnt[2]));
                lines.Append(string.Format(" CORE{0,5:d} {1,-3}{2,-3}{3,8:f4}{4,5:d}\n", h + 1, at.ele, at.ele, 0.0f, h + 1));
            }

            lines.Append("end\nend\n\n");
        }
        lines.Append("end\n");
        return lines.ToString();
    }
    public string ArcWrite()
    {
        return ArcWrite(currentStr);
    }


    //private void LateUpdate()
    //{
    //    //if (!GuiLogic.S.rayStartOnUI)
    //    //{
    //    //    MouseOnScene();
    //    //}
    //}

    //private void OnDrawGizmos()
    //{
    //    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    Vector3 mouse_world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    //对原子的拾取，取到就变色
    //    if (Input.GetKey(KeyCode.Mouse0))
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawRay(mouseRay.origin, mouseRay.origin+100000f*mouseRay.direction);
    //    }

    //}

    //public void MouseOnScene() //摄像机围绕目标旋转操作
    //{

    //    //transform.RotateAround(cameraCenter.position, Vector3.up, speed * Time.deltaTime); //摄像机围绕目标旋转
    //    var mouse_x = Input.GetAxis("Mouse X");//获取鼠标X轴移动
    //    var mouse_y = -Input.GetAxis("Mouse Y");//获取鼠标Y轴移动
    //    Vector3 mouse_world = Camera.main.ScreenToWorldPoint(Input.mousePosition);


    //    if (pickEnable)
    //    {
    //        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    //        RaycastHit[] hitInfo;
    //        Debug.DrawRay(mouseRay.origin, mouseRay.direction);
    //        hitInfo = Physics.RaycastAll(mouseRay, 500f, 1 << LayerMask.NameToLayer("TransparentFX"));

    //        GameObject castAtom = null;
    //        float nearestDis = 9999f;
    //        foreach (RaycastHit r in hitInfo)
    //        {
    //            if (r.distance < nearestDis)
    //            {
    //                castAtom = r.collider.gameObject;
    //                nearestDis = r.distance;
    //            }
    //        }
    //        //鼠标位置上的原子变大
    //        if (castAtom == null)
    //        {
    //            //No atom is cast
    //            if(expandAtom != null)
    //            {
    //                // A -> null
    //                expandAtom.transform.localScale -= expandAtomScale;
    //                expandAtom = null;
    //            }
    //        }
    //        else if (expandAtom == null)
    //        {
    //            //null -> A
    //            expandAtom = castAtom;
    //            castAtom.transform.localScale += expandAtomScale;                
    //        }
    //        else if (castAtom == expandAtom)
    //        {
    //            //cast atom unchange
    //            //A -> A
    //        }
    //        else if (castAtom != expandAtom)
    //        {
    //            //cast atom change
    //            //A -> B
    //            expandAtom.transform.localScale -= expandAtomScale;
    //            expandAtom = castAtom;
    //            castAtom.transform.localScale += expandAtomScale;
    //        }

    //        if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftControl))
    //        {
    //            mouse0PressedTime = Time.time;
    //            mouseStart = Input.mousePosition;
    //            if (pickAtom.Count != 0)
    //            {
    //                //Cancel pickAtom
    //                foreach (GameObject atom in pickAtom)
    //                {
    //                    foreach (Renderer ren in atom.transform.parent.GetComponentsInChildren<Renderer>())
    //                    {
    //                        ren.material.color = atom.GetComponent<Atom>().color;
    //                    }
    //                }
    //                pickAtom.Clear();
    //            }

    //            //mouseBox
    //            if (mouseBox.Length == 0)
    //            {
    //                mouseBox = new LineRenderer[4];
    //            }
    //        }
    //        else if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKey(KeyCode.LeftControl))
    //        {
    //            mouse0PressedTime = Time.time;
    //            mouseStart = Input.mousePosition;

    //            // will not Cancel Pick
    //            //if (pickAtom.Count != 0)
    //            //{
    //            //    //Cancel pickAtom
    //            //    foreach (GameObject atom in pickAtom)
    //            //    {
    //            //        foreach (Renderer ren in atom.transform.parent.GetComponentsInChildren<Renderer>())
    //            //        {
    //            //            ren.material.color = atom.GetComponent<Atom>().color;
    //            //        }
    //            //    }
    //            //    pickAtom.Clear();
    //            //}

    //            //mouseBox
    //            if (mouseBox.Length == 0)
    //            {
    //                mouseBox = new LineRenderer[4];
    //            }
    //        }
    //        //对原子的拾取，取到就变色
    //        else if (Input.GetKey(KeyCode.Mouse0))
    //        {
    //            //shout presse, Select the castAtom
    //            if (Time.time - mouse0PressedTime < 0.2f)
    //            {
    //                if (castAtom != null)
    //                {
    //                    if (!pickAtom.Contains(castAtom))
    //                    {
    //                        pickAtom.Add(castAtom);
    //                    }
    //                }
    //            }
    //            //Long press, Project All the Atom and than Judge if should be Select
    //            else
    //            {
    //                foreach (Atom a in CurrentStructure().atomList)
    //                {
    //                    Vector3 screenPos = Camera.main.WorldToScreenPoint(a.nodeTrans.position);
    //                    if ((screenPos.x - mouseStart.x) * (screenPos.x - Input.mousePosition.x) < 0)
    //                        if ((screenPos.y - mouseStart.y) * (screenPos.y - Input.mousePosition.y) < 0)
    //                            if (!pickAtom.Contains(a.gameObject))
    //                                pickAtom.Add(a.gameObject);
    //                }
    //                if (pickAtom.Count != 0)
    //                {
    //                    foreach (GameObject a in pickAtom)
    //                    {
    //                        foreach (Renderer ren in a.transform.parent.GetComponentsInChildren<Renderer>())
    //                        {
    //                            ren.material.color = Color.yellow;
    //                        }
    //                    }
    //                }


    //                foreach (LineRenderer r in mouseBox)
    //                {
    //                    if (r != null)
    //                    {
    //                        Object.Destroy(r.gameObject);
    //                    }
    //                }
    //                Vector3 v1 = Input.mousePosition;
    //                Vector3 v2 = mouseStart;
    //                mouseBox[0] = this.mouseBoxLine(v1, new Vector3(v1.x, v2.y, 0f));
    //                mouseBox[1] = this.mouseBoxLine(v1, new Vector3(v2.x, v1.y, 0f));
    //                mouseBox[2] = this.mouseBoxLine(v2, new Vector3(v1.x, v2.y, 0f));
    //                mouseBox[3] = this.mouseBoxLine(v2, new Vector3(v2.x, v1.y, 0f));


    //            }
    //        }
    //        //对原子的拾取，取到就变色
    //        else if (Input.GetKeyUp(KeyCode.Mouse0))
    //        {
    //            if (pickAtom.Count != 0)
    //            {
    //                foreach (GameObject a in pickAtom)
    //                {
    //                    foreach (Renderer ren in a.transform.parent.GetComponentsInChildren<Renderer>())
    //                    {
    //                        ren.material.color = Color.yellow;
    //                    }
    //                }
    //            }


    //            foreach (LineRenderer r in mouseBox)
    //            {
    //                if (r != null)
    //                {
    //                    Object.Destroy(r.gameObject);
    //                }
    //            }
    //        }
    //        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Mouse1))
    //        {

    //        }
    //        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse1))
    //        {

    //        }
    //        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Mouse2)) 
    //        {
    //            offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        }
    //        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Mouse2))
    //        {
    //            foreach(GameObject ga in pickAtom)
    //            cameraTransform.position += (offsetTemp - Camera.main.ScreenToWorldPoint(Input.mousePosition));
    //        }
    //    }

    //    //Only process one kind of Input information at one time
    //    if (Input.GetKey(KeyCode.Mouse0)) return;

    //    // When Mouse was clicked(GetKeyDown) ,change Mode
    //    if (Input.GetKeyDown(KeyCode.Mouse1))
    //    {
    //        //Right click of mouse: Rotation. Within tha lattice radius, 3D rotate. Beyond the radius, 2D rotate.
    //        //Judge By Ray Pickup
    //        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    //        RaycastHit[] hitInfo;
    //        hitInfo = Physics.RaycastAll(mouseRay, 500f, 1 << LayerMask.NameToLayer("Default"));

    //        if (hitInfo.Length != 0)
    //        {
    //            //对"SturcturePrefab(Clone)"的拾取，决定旋转模式
    //            this.rotationMode3d = false;
    //            foreach (RaycastHit r in hitInfo)
    //            {
    //                this.rotationMode3d = true;
    //                break;
    //            }
    //        }
    //        else
    //        {
    //            //2D rotate
    //            this.rotationMode3d = false;
    //        }
    //    }
    //    //when mouse is pressed, run in mode
    //    else if (Input.GetKey(KeyCode.Mouse1))
    //    {
    //        if (this.rotationMode3d)
    //        {
    //            //3D rotate
    //            cameraTransform.RotateAround(cameraCenter.position, cameraTransform.up, mouse_x * 5);
    //            cameraTransform.RotateAround(cameraCenter.position, cameraTransform.right, mouse_y * 5);
    //        }
    //        else
    //        {
    //            //2D rotate
    //            Vector3 mouseViewport = Camera.main.ScreenToViewportPoint(Input.mousePosition);
    //            float y = (mouseViewport.y - 0.5f);// theta of mouse to center
    //            float x = (mouseViewport.x - 0.5f);
    //            float square = y * y + x * x;//theta to rotate
    //            float degree = (y / square * mouse_x + x / square * mouse_y) * 2.1F;
    //            cameraTransform.RotateAround(cameraCenter.position, cameraTransform.forward, degree);
    //        }
    //    }
    //    else if (Input.GetKeyDown(KeyCode.Mouse2))
    //    {
    //        offsetTemp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    }
    //    else if (Input.GetKey(KeyCode.Mouse2))
    //    {
    //        cameraTransform.position += (offsetTemp-Camera.main.ScreenToWorldPoint(Input.mousePosition));
    //    }
    //    else if (Input.GetAxis("Mouse ScrollWheel") > 0)
    //    {
    //        //cameraTransform.Translate(Vector3.forward * 0.5f);
    //        Camera.main.orthographicSize += 0.5F;
    //    }
    //    else if (Input.GetAxis("Mouse ScrollWheel") < 0)
    //    {
    //        if (Camera.main.orthographicSize >= 1)
    //            Camera.main.orthographicSize -= 0.5F;
    //        //cameraTransform.Translate(Vector3.forward * -0.5f);
    //    }
    //}

    //private LineRenderer mouseBoxLine(Vector3 v1, Vector3 v2)
    //{
    //    //drawLine
    //    LineRenderer line;
    //    line = Instantiate(mouseBoxLinePrefab).GetComponent<LineRenderer>();
    //    //line.positionCount = 2;//设置两点
    //    //line.startColor = Color.white; //设置直线颜色
    //    //line.endColor = Color.white;
    //    line.startWidth = 0.01f;//设置直线宽度
    //    line.endWidth = 0.05f;

    //    //设置指示线的起点和终点
    //    line.SetPosition(0, Camera.main.ScreenToWorldPoint(v1) + Camera.main.transform.forward);
    //    line.SetPosition(1, Camera.main.ScreenToWorldPoint(v2) + Camera.main.transform.forward);
    //    return line;
    //}



    //public void FocusCameraCenter()
    //{
    //    Vector3 vec = this.CurrentStructure().GetCellTransfer(1, 1, 1)/2;
    //    cameraCenter.position = vec;
    //    cameraTransform.position = vec;
    //    cameraTransform.Translate(Vector3.back.normalized * vec.magnitude * 4);
    //    Camera.main.orthographicSize = vec.magnitude;
    //}

}