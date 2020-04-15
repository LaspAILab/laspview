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

    void Awake()
    {
        this.transform.position = Vector3.zero;


        cameraCenter = Instantiate(StructurPrefabVar).GetComponent<Transform>();
        cameraCenter.SetParent(this.transform);//use a void GameObject as cameraCenter



        this.modelPara = new Dictionary<string, float> { { "ball_radius", 0.5f }, { "bound_radius", 1.0f }, { "bound_boundry", 0.63f } };
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
        structureList[currentStr].gameObject.SetActiveRecursively(false);

        structureList[i].DrawModel(modelPara);
        currentStr = i;
        ConFun.S.printStructureInfo("Structure: " + (currentStr + 1) + " of " + structureList.Count + "\t\t Energy  =  " + structureList[currentStr].energy);
        GuiMain.S.FocusCameraCenter();

        bool upper = true;
        bool lower = true;
        //used for PlayerBtnChecked()
        if (currentStr <= 0)
            lower = false;  //when at lower bound, false means deactivate lower player button
        if (currentStr >= lenStructures - 1)
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
        Structure newStru = Instantiate(StructurPrefabVar).GetComponent<Structure>();
        structureList.Add(newStru);
        newStru.transform.SetParent(this.transform, false);
        lenStructures += 1;
        return newStru;

    }



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

    public string ArcWrite(int startStruc, int count = 0)
    {
        StringBuilder lines = new StringBuilder();
        lines.Append("!BIOSYM archive 2\nPBC=ON\n");
        int id = 0;
        for (int i = startStruc; i <= startStruc + count; i++, id++)
        {
            Structure struc = structureList[i];
            lines.Append(string.Format("                      Energy{0,10:d}{1,16:f4}{2,18:f6}\n", id, 0.0f, struc.energy));
            lines.Append("!DATE\n");
            lines.Append(string.Format("PBC{0,14:f8}{1,14:f8}{2,14:f8}{3,14:f8}{4,14:f8}{5,14:f8}\n",
                                        struc.abc[0], struc.abc[1], struc.abc[2], struc.abc[3], struc.abc[4], struc.abc[5]));
            for (int h = 0; h < struc.atomList.Count; h++)
            {
                Atom at = struc.atomList[h];
                lines.Append(string.Format("{0,-2}{1,15:f9}{2,15:f9}{3,15:f9}", at.ele, at.cdnt[0], at.cdnt[1], at.cdnt[2]));
                lines.Append(string.Format(" CORE{0,5:d} {1,-3}{2,-3}{3,8:f4}{4,5:d}\n", h + 1, at.ele, at.ele, 0.0f, h + 1));
            }

            lines.Append("end\nend\n");
        }
        lines.Append("end\n");
        return lines.ToString();
    }
    public string ArcWrite()
    {
        return ArcWrite(currentStr);
    }

    public string ArcWriteAll()
    {
        return ArcWrite(0, structureList.Count - 1);
    }

}