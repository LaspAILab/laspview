using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ViewerNetwork;
using System.Threading.Tasks;
public class R_ServerPanelSrc : MonoBehaviour
{
#if !UNITY_WEBGL
    static public R_ServerPanelSrc S;

    // Start is called before the first frame update
    public bool lclSrvrListening = false;
    void Awake()
    {
        S = this;

        //Set Server panel
        this.transform.SetParent(GuiMain.S.rightCanvas.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localScale = Vector3.one;
        Button svrButton = GameObject.Find("ServerBtn").GetComponent<Button>();
        svrButton.onClick.AddListener(() =>
        {
            GuiMain.S.BtnHideObject(svrButton, this.gameObject);
            //when Btn1.text = "Check Local IP"
            Dropdown localAddrDp = this.GetComponentInChildren<Dropdown>();
            List<string> addrString = MainScr.S.localServer.GetIPAddress();
            List<Dropdown.OptionData> addrDrop = new List<Dropdown.OptionData>();
            foreach (string str in addrString)
            {
                addrDrop.Add(new Dropdown.OptionData(str));
            }
            localAddrDp.options = addrDrop;
        });
        this.gameObject.SetActive(false);
    }



    private void Start()
    {
        //print default remote ip on ServerIP inputfield;
        this.transform.GetChild(1).GetChild(1).gameObject.GetComponent<InputField>().text = ConFun.S.globalPara["remote_ip"];
        //check local IP   
        GuiMain.S.SetUIInteract(this.transform.GetChild(2).GetChild(0).GetComponent<Button>(), false);
    }

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
            ////when Btn1.text = "Check Local IP"
            //Dropdown localAddrDp = this.GetComponentInChildren<Dropdown>();
            //List<string> addrString = MainScr.S.localServer.GetIPAddress();
            //List<Dropdown.OptionData> addrDrop = new List<Dropdown.OptionData>();
            //foreach (string str in addrString)
            //{
            //    addrDrop.Add(new Dropdown.OptionData(str));
            //}
            //localAddrDp.options = addrDrop;
            //GuiMain.S.SetUIInteract(this.transform.GetChild(2).GetChild(0).GetComponent<Button>(), false) ;
        }
        else
        {
            //when Btn text = "Stop Listening"

            MainScr.S.localServer.Quit2();
            lclSrvrListening = false;
            NetLocalBtnChange();
            //NetLocalBtnChange is called in LocalServer.Quit(), like a callback function
        }

    }
    public void NetLocalBtn2()
    {
        //Btn of StartListening
        if (lclSrvrListening == false)
        {
            Dropdown localAddrDp = this.GetComponentInChildren<Dropdown>();
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
        Button Btn1 = this.transform.GetChild(2).GetChild(0).GetComponent<Button>();
        Button Btn2 = this.transform.GetChild(2).GetChild(1).GetComponent<Button>();
        Dropdown localAddrDp = this.GetComponentInChildren<Dropdown>();
        if (lclSrvrListening == true)
        {
            //Btn1.GetComponentInChildren<Text>().text = "Stop Listening";
            Btn2.GetComponentInChildren<Text>().text = "Listening...";
            GuiMain.S.SetUIInteract(Btn1, true);
            GuiMain.S.SetUIInteract(Btn2, false);
            GuiMain.S.SetUIInteract(localAddrDp, false);
        }
        else
        {
            //Btn1.GetComponentInChildren<Text>().text = "Check Local IP";
            Btn2.GetComponentInChildren<Text>().text = "Start Listening";
            GuiMain.S.SetUIInteract(Btn1, false);
            GuiMain.S.SetUIInteract(Btn2, true);
            GuiMain.S.SetUIInteract(localAddrDp, true);
        }
    }

    async public void NetCheckIP2Remote()
    {
        //Btn of "Send Local IP"
        Button serverBtn = this.transform.GetChild(3).GetChild(0).gameObject.GetComponent<Button>();
        GuiMain.S.SetUIInteract(serverBtn, false);
        LocalClient client = gameObject.AddComponent<LocalClient>();
        string remoteip = this.transform.GetChild(1).GetComponentInChildren<InputField>().text;
        string str = null;
        string localip = this.transform.GetChild(0).GetComponentInChildren<Dropdown>().captionText.text;

        await Task.Run(() =>
        {
            if (client.StartClient(remoteip) == true)
            {
                StringBuilder msgList = new StringBuilder("CheckIP=" + localip);
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
        GuiMain.S.SetUIInteract(serverBtn, true);

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
        Button serverBtn = this.transform.GetChild(3).GetChild(1).gameObject.GetComponent<Button>();
        GuiMain.S.SetUIInteract(serverBtn, false);
        LocalClient client = gameObject.AddComponent<LocalClient>();
        string ip = this.transform.GetChild(1).GetComponentInChildren<InputField>().text;
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
        GuiMain.S.SetUIInteract(serverBtn, true);

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
}
