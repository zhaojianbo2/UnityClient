using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NetWorkBehavior : WeakSingleTon<NetWorkBehavior>
{
    public const float ReConnectTime = 2f;

    #region Public Members
    public string IP;
    public int Port;
    #endregion

    TcpClient m_Client;
    Thread m_SendThread;
    ConcurrentQueue<Msg> m_SendQueue = new ConcurrentQueue<Msg>();
    object m_NetworkExceptionLock = new object();
    Exception m_NetworkException;

    Thread m_ReceiveThread;
    ConcurrentQueue<Msg> m_ReceiveQueue = new ConcurrentQueue<Msg>();
    StringBuilder m_WaningMsg = new StringBuilder();

    static Dictionary<Type, int> m_MessageIDDic = new Dictionary<Type, int>();
    //用于控制一帧的最后发送消息
    AutoResetEvent m_SendSignal = new AutoResetEvent(false);
    //保存消息id对应的parser缓存,用于解析bytes data
    Dictionary<int, Delegate> m_MsgParserCache = new Dictionary<int, Delegate>();
    //用于注册返回消息委托方法
    Dictionary<int, List<Func<object, bool>>> m_RegistedMsgHandler = new Dictionary<int, List<Func<object, bool>>>();
    //用于委托方法清理
    HashSet<MsgHandlerInfo> m_RegisteredFuncs = new HashSet<MsgHandlerInfo>();
    //用于检测当前是否拥堵，暂时还没返现用处
    List<SendInfo> m_SendingList = new List<SendInfo>();

    #region CallBack
    /// <summary>
    /// 开始建立连接时执行
    /// </summary>
    public event Action OnBeginConnect;
    /// <summary>
    /// 建立连接得到返回结果时执行
    /// </summary>
    public event Action<bool> OnConnectResult;
    /// <summary>
    /// 建立连接完成时执行
    /// </summary>
    public event Action OnConnected;
    /// <summary>
    /// 断开连接时执行
    /// </summary>
    public event Action OnDisconnect;

    #endregion
    struct Msg
    {
        public Msg(int id, object protoMsg = null, byte[] jsonData = null)
        {
            Id = id;
            ProtoMsg = protoMsg;
            JsonData = jsonData;
        }
        public int Id;
        public object ProtoMsg;
        public byte[] JsonData;
    }
    class SendInfo
    {
        public float SendTime;
        public int MsgID;
    }

    class MsgHandlerInfo
    {
        public MonoBehaviour Target;
        public Func<object, bool> Func;
        public List<Func<object, bool>> Handle;
    }
    private void Awake()
    {
        
        gameObject.AddComponent<NetDispatchHelper>();
        NetAdapter netAdapter = gameObject.AddComponent<NetAdapter>();
       // gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        Transform DontDestroy = GameObject.FindGameObjectWithTag("DontDestroy").transform;
        transform.SetParent(DontDestroy);
    }

    void Start()
    {
        StartCoroutine(ConnectServer());
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LateUpdate()
    {
        FlushSend();
    }
    void OnApplicationQuit()
    {
        if (m_Client != null)
        {
            FlushSend();
            m_Client.Close();//windows platform need
        }
    }
    void FlushSend()
    {
        if (m_SendThread != null && m_SendThread.IsAlive)
            m_SendSignal.Set();
    }

    IEnumerator ConnectServer(float delay = 0)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);
        OnBeginConnect?.Invoke();
        m_Client = new TcpClient();
        m_Client.NoDelay = true;
        m_Client.Client.Blocking = true;
        m_Client.BeginConnect(IP, Port, ConnectCallback, m_Client);

    }
    void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            m_Client.EndConnect(ar);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
        }
        finally
        {
            OnConnectResult?.Invoke(m_Client.Connected);
            if (m_Client.Connected)
            {
                CreateSendLoop();
                CreateReceiveLoop();
                OnConnected?.Invoke();
            }
            else
            {
                StartCoroutine(ConnectServer(ReConnectTime));
            }
        }
    }


    void CreateSendLoop()
    {

        m_SendThread = new Thread(() =>
        {
            try
            {
                Msg msg;
                while (m_SendQueue.TryDequeue(out msg)) ;
                var sendStream = new MemoryStream();
                while (true)
                {
                    m_SendSignal.WaitOne();
                    sendStream.Seek(0, SeekOrigin.Begin);
                    sendStream.SetLength(0);

                    while (m_SendQueue.TryDequeue(out msg))
                    {
                        var len = 8;
                        byte[] data = null;
                        if (msg.ProtoMsg != null)
                        {
                            data = ((IMessage)msg.ProtoMsg).ToByteArray();
                            len += data.Length;
                        } else if (msg.JsonData!=null)
                        {
                            data = msg.JsonData;
                            len += data.Length;
                        }
                        sendStream.Write(BitConverter.GetBytes(len), 0, 4);
                        sendStream.Write(BitConverter.GetBytes(msg.Id), 0, 4);
                        if (data != null)
                            sendStream.Write(data, 0, data.Length);
                    }

                    if (sendStream.Length > 0)
                    {
                        var buffer = sendStream.GetBuffer();
                        var length = (int)sendStream.Length;
                        int sended = 0;
                        while (sended < length)
                            sended += m_Client.Client.Send(buffer, sended, length - sended, SocketFlags.None);
                    }
                }
            }
            catch (Exception exception)
            {
                lock (m_NetworkExceptionLock)
                    m_NetworkException = exception;
            }

        });
        m_SendThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        m_SendThread.Start();

    }

    void CreateReceiveLoop()
    {
        m_ReceiveThread = new Thread(() =>
        {
            try
            {

                Msg msg;
                while (m_ReceiveQueue.TryDequeue(out msg)) ;
                byte[] recvbuff = new byte[4];
                int recvsize, id, len;
                while (true)
                {
                    recvsize = 0;
                    while (recvsize < 4)
                        recvsize += m_Client.Client.Receive(recvbuff, recvsize, 4 - recvsize, SocketFlags.None);

                    len = BitConverter.ToInt32(recvbuff, 0);
                    if (len < 8)
                        throw new Exception("recv msg len < 8");

                    recvsize = 0;

                    while (recvsize < 4)
                        recvsize += m_Client.Client.Receive(recvbuff, recvsize, 4 - recvsize, SocketFlags.None);

                    id = BitConverter.ToInt32(recvbuff, 0);
                    byte[] data = null;
                    object parseredMsg = null;
                    len -= 8;
                    data = new byte[len];
                    recvsize = 0;
                    while (recvsize < len)
                        recvsize += m_Client.Client.Receive(data, recvsize, len - recvsize, SocketFlags.None);

                    Delegate parser;
                    if (m_MsgParserCache.TryGetValue(id, out parser))
                    {
                        try
                        {
                            parseredMsg = parser.DynamicInvoke(data);
                        }
                        catch (Exception e)
                        {
                            lock (m_WaningMsg)
                                m_WaningMsg.Append("parser protomsg failed id: " + id + "  exception :" + e + "\n");
                        }
                    }
                    else
                    {
                        if (len > 0)
                            lock (m_WaningMsg)
                                m_WaningMsg.Append("no regist parser for msg id : " + id + "\n");
                    }
                    m_ReceiveQueue.Enqueue(new Msg(id, parseredMsg));
                }
            }
            catch (Exception exception)
            {
                lock (m_NetworkExceptionLock)
                    m_NetworkException = exception;
            }
        });
        m_ReceiveThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        m_ReceiveThread.Start();
    }
    public void Dispatch()
    {
        Msg msg;
        while (m_ReceiveQueue.TryDequeue(out msg))
        {
            if (m_SendingList.Count > 0)
            {
                var index = m_SendingList.FindLastIndex((SendInfo info) => info.MsgID == msg.Id);
                if (index >= 0)
                    m_SendingList.RemoveAt(index);
            }
            List<Func<object, bool>> handles = null;
            m_RegistedMsgHandler.TryGetValue(msg.Id, out handles);

            if (handles == null || handles.Count == 0)
            {
                Debug.LogWarning("no registed handle for msg id: " + msg.Id + " object: " + msg.ProtoMsg);
                continue;
            }

            for (int i = handles.Count - 1; i >= 0; --i)
                if (!handles[i](msg.ProtoMsg))
                    handles.RemoveAt(i);
        }
    }
    public void Send<T>(T message, int id = 0,bool waitForReply = false)
    {
        if (id == 0)
        {
            id = GetID(typeof(T));
            if (waitForReply)
                m_SendingList.Add(new SendInfo() { MsgID = id, SendTime = Time.time });
            m_SendQueue.Enqueue(new Msg(id, message));
        }
        else {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            m_SendQueue.Enqueue(new Msg(id, null, bytes));
        }

    }


    public void RegistMsg<T>(Action<T> receiver,int id=0 ) where T : class, new()
    {
        Delegate parser;
        if (!m_MsgParserCache.TryGetValue(id, out parser))
        {
            if (id==0)
            {
                id = GetID(typeof(T));
                var paserProperty = typeof(T).GetProperty("Parser");
                var parserobj = paserProperty.GetValue(typeof(T));
                var parsermethod = parserobj.GetType().GetMethod("ParseFrom", new Type[] { typeof(byte[]) });
                parser = Delegate.CreateDelegate(typeof(Func<byte[], T>), parserobj, parsermethod, true);
            }
            else
            {
                Func<byte[], T> func = (byte[] data) => 
                {
                    return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
                
                };
                parser = func;

            }

            m_MsgParserCache[id] = parser;
        }
        else
        {
            if (parser.GetType() != typeof(Func<byte[], T>))
            {
                throw new Exception("msg id " + id + " parser not same " + parser.GetType() + "      " + typeof(T));
            }
        }

        RegistInteral(id, receiver, false);
    }
    void RegistInteral(int id, Delegate receiver, bool emptyMsg)
    {
        List<Func<object, bool>> handler;
        if (!m_RegistedMsgHandler.TryGetValue(id, out handler))
        {
            handler = new List<Func<object, bool>>();
            m_RegistedMsgHandler[id] = handler;
        }

        Func<object, bool> func = (object msg) =>
        {
            if (emptyMsg)
            {
                if (msg != null)
                {
                    Debug.LogWarning("no empty msg: " + id);
                    return false;
                }
            }
            else
            {
                if (msg == null)
                {
                    Debug.LogWarning(" id: " + id + "empty msg ");
                }
            }

            object[] para;
            if (emptyMsg)
                para = new object[] { };
            else
                para = new object[] { msg };
            try
            {
                receiver.Method.Invoke(receiver.Target, para);
            }
            catch (Exception e)
            {
                Debug.LogError("netMsg id: " + id + "   " + e);
                return false;
            }


            return true;
        };
        handler.Add(func);
        m_RegisteredFuncs.Add(new MsgHandlerInfo() {Func = func, Handle = handler });
    }
    static int GetID(Type type)
    {
        if (!m_MessageIDDic.TryGetValue(type, out int id))
        {
            var types = type.GetNestedTypes();
            foreach (var item in types)
            {
                if (item.Name == "Types")
                {
                    var msgID = item.GetNestedTypes();
                    foreach (var item1 in msgID)
                    {
                        if (item1.Name == "MsgID")
                        {
                            foreach (var item2 in Enum.GetValues(item1))
                            {
                                if (item2.ToString() == "EMsgId")
                                {
                                    id = (int)Enum.GetValues(item1).GetValue(1);
                                    m_MessageIDDic.Add(type, id);
                                }
                            }
                        }
                    }
                }
            }
        }
        return id;
    }
}
