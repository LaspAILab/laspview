#if !UNITY_WEBGL
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
//Messege XML Format
//<laspview user="%username">
//    <file type = "%type" name="%name">
//        # %type = arc  
//        # %name = directory name of file
//        %fileContent
//    </file>

//    or
//    <sig>%sig</sig> #signal including stop server, grab file

//    or
//    <path>%path</paht> #pwd of file
//</laspview>
#endif

namespace ViewerNetwork
{
#if !UNITY_WEBGL
    public class NetworkBasic : MonoBehaviour
    {
        public string RmTag(string msg, out string tag, out Dictionary<string, string> attr)
        {
            attr = new Dictionary<string, string>();
            string fullTag = Regex.Match(msg, @"\A<([^/<>]*)>").Groups[1].ToString();
            if (fullTag == "")
            {
                throw new Exception("SocketError: Wrong XML format");
                //tag = "";
                //return "";
            }
            string[] st = fullTag.Split(' ');
            tag = st[0];
            for (int i = 1; i < st.Length; i++)
            {
                string[] at = st[i].Split('=');
                attr.Add(at[0], at[1]);
            }
            string pattern = $"^(<{tag}([^>/])*>)|(</{tag}>)";
            msg = Regex.Replace(msg, pattern, "");
            return msg;
        }

        public StringBuilder AddTag(StringBuilder msgList, string tag)
        {
            msgList.Insert(0, $"<{tag}>").Append($"</{tag}>");
            return msgList;
        }

        public StringBuilder AddTag(StringBuilder msgList, string tag, string attr)
        {
            msgList.Insert(0, $"<{tag} {attr}>").Append($"</{tag}>");
            return msgList;
        }

        public List<string> GetIPAddress()
        {
            //read by dns;
            List<string> ipv4Addresses = new List<string>();
            string hostName = Dns.GetHostName();   //获取本机名
            IPAddress[] address = Dns.GetHostAddresses(hostName);
            foreach (IPAddress addr in address)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork) // judge if it is ipv4
                {
                    ipv4Addresses.Add(addr.ToString());
                }
            }

            //read by ipconfig
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string lines = RunCmd("chcp 437 &  ipconfig -all");
#elif UNITY_STANDALONE_OSX
            string lines = RunCms("ipconfig");
#endif
            //print(lines);
            foreach(string li in Regex.Split(lines, "\n", RegexOptions.Multiline))
            {
                //print(li);
                Match m1 = Regex.Match(li, @"addr", RegexOptions.IgnoreCase);
                if (m1.Success)
                {
                    //print(m1);
                    MatchCollection m2 = Regex.Matches(li, @"((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)");
                    if(m2.Count != 0)
                    {
                        foreach(Match m3 in m2)
                        {
                            if (Regex.IsMatch(m3.Value, "127.0.0.1")) continue;
                            if (Regex.IsMatch(m3.Value, "255.255")) continue;
                            if (Regex.IsMatch(m3.Value, "0.0.0")) continue;
                            if (Regex.IsMatch(m3.Value, @".255$")) continue;
                            if (!ipv4Addresses.Contains(m3.Value)) ipv4Addresses.Add(m3.Value);
                            print(m3.Value);
                        }
                    }
                }

            }

            return ipv4Addresses;
        }



        private string RunCmd(string  cmd)
        {
            Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c  " + cmd;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            return p.StandardOutput.ReadToEnd();
        }
    }


    public class LocalServer : NetworkBasic
    {
        static public LocalServer S;
        static public Thread listenThred;
        TcpListener tlistener;
        TcpClient remoteClient;
        private int bufferSize = 4096;
        private string localIP;  //read from ip.txt later
        private int localPort;   //read from ip.txt later

        //private string fileType = "";
        public string line;
        public string fileType;             //when receive file, fileWaiting, fileType, fileNam will not be Null
        //public string fileWainting = null;  //fileWaiting will be check if it is Null each Update() in MainScr
        public string fileName;             //After reading the file, fileWaiting will be set to Null again in MainScr
        static string tempPath = "./Temp/";

        public void Start()
        {
            //creak work directory
            if (false == System.IO.Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }


            localPort = int.Parse(ConFun.S.globalPara["local_port"]);
            if (!(1000 < localPort && localPort < 99999))
            {
                ConFun.S.printStatusInfo("wrong local Port format");
                return;
            }
        }

        public void StartServer(string ip)
        {
            localIP = ip;
            //use Loom to start a thread
            Loom.RunAsync(() =>
            {
                listenThred = new Thread(()=>RunServer(ip));
                listenThred.Start();
            });
        }

        private void RunServer(string ip)
        {
            IPAddress ipAds = IPAddress.Parse(ip);
            tlistener = new TcpListener(ipAds, localPort);
            StringBuilder msgList;
            byte[] buffer;
            int byteRead;
            string msgRtn;

            tlistener.Start();
            printOnMainTrhead("Socket: start listening " + tlistener.LocalEndpoint);

            

            while (true)
            {
                remoteClient = tlistener.AcceptTcpClient();//接收已连接的客户端,阻塞方法
                printOnMainTrhead("Connected！local: " + remoteClient.Client.LocalEndPoint + " < ---Client:" + remoteClient.Client.RemoteEndPoint);

                NetworkStream streamToClient = remoteClient.GetStream();//获得来自客户端的流
                msgList = new StringBuilder();
                try  //直接关掉客户端，服务器端会抛出异常
                {
                    buffer = new byte[bufferSize];//定义一个缓存buffer数组

                    //1.检查第一个bufferSize，有问题直接关掉链接
                    byteRead = streamToClient.Read(buffer, 0, bufferSize);
                    string msgHead = Encoding.UTF8.GetString(buffer, 0, byteRead);
                    if (this.JudgeMsgHead(msgHead))
                    {
                        msgList.Append(msgHead);
                    }
                    else
                    {
                        throw new Exception("Server Error: Unkown Message. Wrong head format or user name different from allkey.log");
                    }

                    //2.接收完整信息，//文件长度超过1M强制停止接收
                    int i = 0;
                    do
                    {
                        byteRead = streamToClient.Read(buffer, 0, bufferSize);//将数据搞入缓存中（有朋友说read()是阻塞方法，测试中未发现程序阻塞）
                        if (byteRead == 0)//连接断开，或者在TCPClient上调用了Close()方法，或者在流上调用了Dispose()方法。
                        {
                            printOnMainTrhead("Disconnected......");
                            break;
                        }

                        msgList.Append(Encoding.UTF8.GetString(buffer, 0, byteRead));//从二进制转换为字符串对应的客户端会有从字符串转换为二进制的方法
                        print("Receive: [" + byteRead + "byte]");
                        i++;
                        if (i > 768)
                        {
                            msgList.Append("</file></laspview>");
                            //文件长度超过1M强制停止接收
                            break;
                        }
                    }
                    while (streamToClient.CanRead );


                    //信息处理
                    msgRtn = this.MsgProcess(msgList.ToString());
                    if (msgRtn == "Stop")
                    {
                        break;
                    }
                    else 
                    {

                    }
                }
                catch (Exception ex)
                {
                    printOnMainTrhead("Error:" + ex.GetType() + ex.StackTrace + ex.Message);
                    //StreamWriter file = new StreamWriter(@"error.txt", true);
                    //file.WriteLine(ex.Message);
                    //break;
                }
                finally
                {
                    streamToClient.Close();
                }
            }
            printOnMainTrhead("Stop Listening");
            tlistener.Stop();
            

        }

        private bool JudgeMsgHead(string head)
        {
            string user = Regex.Match(head, @"\A<laspview user=([^/<>]*)>").Groups[1].ToString();
            //print(user);
            if (user == ConFun.S.globalPara["user"] || user == "local")
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private string MsgProcess(string msg)
        {
            string tag;
            Dictionary<string, string> attr = new Dictionary<string, string>();
            msg = RmTag(msg, out tag, out attr);

            if (tag != "laspview")
            {
                printOnMainTrhead($"unknown message: {msg}");
                return "";
            }
            else
            {
                printOnMainTrhead($"message of user {attr["user"]}");

                //second remove
                msg = RmTag(msg, out tag, out attr);
                if (tag == "file")
                {
                    if (attr["type"] == "arc" | attr["type"] == "car")
                    {
                        printOnMainTrhead($"filename：{attr["name"]}");

                        fileName = attr["name"];
                        File.WriteAllText(Path.Combine(tempPath, fileName), msg);
                        AddArc2MainThread(fileName, msg);
                    }
                }
                else if (tag == "sig")
                {
                    if (msg == "StopLocalServer")
                    {
                        return "Stop"; //means closer server
                    }
                }
                return ConFun.S.globalPara["user"] + "local reveive" + tag + "succeed";
            }

        }

        private void AddArc2MainThread(string fileName, string msg)
        {
            Loom.QueueOnMainThread((param) =>
            {
                //MainScr.S.AddArc(fileName, msg);
                MainScr.S.AddArcByCorouWaiting(fileName, msg);
                File.Delete(fileName);
            }, null);
        }


        public void Quit1()
        {
            //used for main thread to kill listener
            //forced close
            Loom.RunAsync(() =>
            {
                if (listenThred.IsAlive)
                {
                    tlistener.Stop();
                    listenThred.Abort();
                }
                Loom.QueueOnMainThread((param) =>
                {
                    R_ServerPanelSrc.S.NetLocalBtnChange();
                    ConFun.S.printStatusInfo("Stop Listening");
                }, null);
            });
        }

        public void Quit2()
        {
            if (R_ServerPanelSrc.S.lclSrvrListening == false)
            {
                return;
            }
            LocalClient client = gameObject.AddComponent<LocalClient>();
            Loom.RunAsync(() =>
            {
                client.remoteIP = localIP;
                client.remotePort = localPort;
                if (true)
                {
                    StringBuilder msgList = new StringBuilder("StopLocalServer");
                    msgList = client.AddTag(msgList, "sig");
                    msgList = client.AddTag(msgList, "laspview", "user=local");
                    client.SynSendMsg(msgList.ToString());
                }
            });
            UnityEngine.Object.Destroy(client);

        }

        public void printOnMainTrhead(string str)
        {
            Loom.QueueOnMainThread((param) =>
            {
                ConFun.S.printStatusInfo(str);
            }, null);
        }
    }

    public class LocalClient : NetworkBasic
    {

        private TcpClient tcpClient;
        NetworkStream streamToServer;
        //private int bufferSize = 4096;

        /// 远程服务IP地址
        public string remoteIP;
        public int remotePort;

        public bool StartClient(string ip)
        {
            //check if IPAdress format in ip.txt is correct
            remoteIP = ip;
            remotePort = int.Parse(ConFun.S.globalPara["remote_port"]);
            if (!Regex.IsMatch(remoteIP, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$"))
            {
                ConFun.S.printStatusInfo("wrong remote IP adress format");
                return false;
            }
            if (!(1000 < remotePort && remotePort < 99999))
            {
                ConFun.S.printStatusInfo("wrong remote Port format");
                return false;
            }
            //ConFun.S.printStatusInfo((remoteIP, remotePort));
            return true;

        }

        //public void AsynSendMsg(string msg)
        //{
        //    //异步不能直接传参，变量似乎只能在函数外面(类的属性里面)传
        //    asynWaitingMsg = msg;
        //    //初始化TCP客户端对象
        //    tcpClient = new TcpClient();
        //    //开始异步连接
        //    try
        //    {
        //        tcpClient.BeginConnect(remoteIP, remotePort, new AsyncCallback(AsynSendWaitingMsg), tcpClient);
        //    }
        //    catch (Exception ex)
        //    {
        //        print("Send Msg Error:" + ex.Message.ToString());
        //    }
        //}
        //private void AsynSendWaitingMsg(IAsyncResult ar)
        //{
        //    try
        //    {
        //        TcpClient t = (TcpClient)ar.AsyncState;

        //        t.EndConnect(ar);
        //        streamToServer = tcpClient.GetStream();
        //        byte[] bytMsg = Encoding.ASCII.GetBytes(asynWaitingMsg);
        //        streamToServer.Write(bytMsg, 0, bytMsg.Length);
        //        if (streamToServer.CanWrite && asynWaitingMsg != null && asynWaitingMsg.Length > 0)
        //        {
        //            //发送数据
        //            streamToServer.Write(Encoding.UTF8.GetBytes(asynWaitingMsg), 0, bufferSize);
        //            streamToServer.Flush();
        //        }
        //        asynWaitingMsg = null;

        //    }
        //    catch (Exception ex)
        //    {
        //        print("Send Msg Error:" + ex.Message.ToString());
        //    }
        //    finally
        //    {
        //        if (streamToServer != null) streamToServer.Close();
        //        if (tcpClient != null) tcpClient.Close();
        //    }
        //}

        public string  SynSendMsg(string msg)
        {
            string rtn = null;
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(remoteIP, remotePort);
                streamToServer = tcpClient.GetStream();
                byte[] bytMsg = Encoding.ASCII.GetBytes(msg);
                streamToServer.Write(bytMsg, 0, bytMsg.Length);
                streamToServer.Flush();
            }

            catch (Exception ex)
            {
                rtn = ("Send Msg Error:" + ex.Message.ToString());
            }
            finally
            {
                if (streamToServer != null) streamToServer.Close();
                if (tcpClient != null) tcpClient.Close();
                
            }
            return rtn;

        }

        public void printOnMainTrhead(string str)
        {
            Loom.QueueOnMainThread((param) =>
            {
                ConFun.S.printStatusInfo(str);
            }, null);
        }

        ///// <summary>
        ///// 异步接受数据
        ///// </summary>
        ///// <param name="iar"></param>
        //public static void AsynReceiveData(IAsyncResult iar)
        //{
        //    byte[] CurrentBytes = (byte[])iar.AsyncState;
        //    try
        //    {
        //        //结束了本次数据接收
        //        int num = networkStream.EndRead(iar);
        //        //这里展示结果为InfoModel的CurrBytes属性，将返回的数据添加至返回数据容器中
        //        ResponseBytes.Add(CurrentBytes);
        //        //处理结果后马上启动数据异步读取【目前我每条接收的字节数据长度不会超过1024】
        //        byte[] NewBytes = new byte[1024];
        //        networkStream.BeginRead(NewBytes, 0, NewBytes.Length, new AsyncCallback(AsynReceiveData), NewBytes);
        //    }
        //    catch (Exception ex)
        //    {
        //        VerficationOperate.WriteTextLogs("TcpClientBusiness", "AsynReceiveData|异常消息：" + ex.Message.ToString());
        //    }
        //}
    }


    //Unity do not allowed sub thread to affect main thread
    // use Loom to transfer main thread function
    public class Loom : MonoBehaviour
    {
        public static int maxThreads = 8;
        static int numThreads;

        private static Loom _current;
        //private int _count;
        public static Loom Current
        {
            get
            {
                Initialize();
                return _current;
            }
        }

        void Awake()
        {
            _current = this;
            initialized = true;
        }

        static bool initialized;

        public static void Initialize()
        {
            if (!initialized)
            {
                if (!Application.isPlaying)
                    return;
                initialized = true;
                var g = new GameObject("Loom");
                _current = g.AddComponent<Loom>();
#if !ARTIST_BUILD
                UnityEngine.Object.DontDestroyOnLoad(g);
#endif
            }

        }
        public struct NoDelayedQueueItem
        {
            public Action<object> action;
            public object param;
        }

        private List<NoDelayedQueueItem> _actions = new List<NoDelayedQueueItem>();
        public struct DelayedQueueItem
        {
            public float time;
            public Action<object> action;
            public object param;
        }
        private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

        List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

        public static void QueueOnMainThread(Action<object> taction, object tparam)
        {
            QueueOnMainThread(taction, tparam, 0f);
        }
        public static void QueueOnMainThread(Action<object> taction, object tparam, float time)
        {
            if (time != 0)
            {
                lock (Current._delayed)
                {
                    Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = taction, param = tparam });
                }
            }
            else
            {
                lock (Current._actions)
                {
                    Current._actions.Add(new NoDelayedQueueItem { action = taction, param = tparam });
                }
            }
        }

        public static Thread RunAsync(Action a)
        {
            Initialize();
            while (numThreads >= maxThreads)
            {
                Thread.Sleep(100);
            }
            Interlocked.Increment(ref numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, a);
            return null;
        }

        private static void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Decrement(ref numThreads);
            }

        }


        void OnDisable()
        {
            if (_current == this)
            {

                _current = null;
            }
        }



        // Use this for initialization
        void Start()
        {

        }

        List<NoDelayedQueueItem> _currentActions = new List<NoDelayedQueueItem>();

        // Update is called once per frame
        void Update()
        {
            if (_actions.Count > 0)
            {
                lock (_actions)
                {
                    _currentActions.Clear();
                    _currentActions.AddRange(_actions);
                    _actions.Clear();
                }
                for (int i = 0; i < _currentActions.Count; i++)
                {
                    _currentActions[i].action(_currentActions[i].param);
                }
            }

            if (_delayed.Count > 0)
            {
                lock (_delayed)
                {
                    _currentDelayed.Clear();
                    _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
                    for (int i = 0; i < _currentDelayed.Count; i++)
                    {
                        _delayed.Remove(_currentDelayed[i]);
                    }
                }

                for (int i = 0; i < _currentDelayed.Count; i++)
                {
                    _currentDelayed[i].action(_currentDelayed[i].param);
                }
            }
        }
    }
#endif
        }

//**************** example to use Loom to communiccate between two thread **************************
//https://blog.csdn.net/luoyikun/article/details/78650904
//public Text mText;
//void Start()
//{
//    Thread thread = new Thread(RefreshText);
//    thread.Start();
//}

//void Update()
//{

//}
//private void RefreshText()
//{
//    mText.text = "Hello Loom!";
//}
//using UnityEngine;
//using System.Collections;
//using UnityEngine.UI;
//using System.Threading;
//public class testLoom : MonoBehaviour
//{

//    public Text mText;
//    void Start()
//    {

//        // 用Loom的方法调用一个线程
//        Loom.RunAsync(
//            () =>
//            {
//                Thread thread = new Thread(RefreshText);
//                thread.Start();
//            }
//            );
//    }

//    void Update()
//    {

//    }
//    private void RefreshText()
//    {
//        // 用Loom的方法在Unity主线程中调用Text组件
//        Loom.QueueOnMainThread((param) =>
//        {
//            mText.text = "Hello Loom!";
//        }, null);
//    }

//}
//****************************** example end ************************************



