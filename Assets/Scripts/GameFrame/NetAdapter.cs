using Org.Game.Protobuf.C2S;
using Org.Game.Protobuf.S2C;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 
using UnityEngine.SceneManagement;

public class NetAdapter : MonoBehaviour
{
    public static NetAdapter Instance;

    [HideInInspector]
    public float Ping = -1;//ping <0 not connect
    public bool IsConnected => Ping >= 0;

    float m_PingSendTime = 0;

    Action m_LoginCallBack;

    public string UserName;

    public float ServerTime;

    public long playerId;

    private void Awake()
    {
        Instance = this;
        NetWorkBehavior.Instance.OnDisconnect += () => {
            Ping = -1;
            StopAllCoroutines();
            //NetManager.Instance.Reconnect();      
            FramworkEvent.ToastWarning.BroadCastEvent("网络连接已断开");
        };
        NetWorkBehavior.Instance.RegistMsg((Pong pong) =>
        {
            ServerTime = pong.ServerTime;
            Ping = ServerTime - m_PingSendTime;
        });

        ResObjShowHandler handler = new ResObjShowHandler(Instance);
        NetWorkBehavior.Instance.RegistMsg<ResLoginMsg>(handler.HandActionLogin, 2000);
        NetWorkBehavior.Instance.RegistMsg<ResSceneObjShowMsg>(handler.HandActionObjShow, 2001);
        NetWorkBehavior.Instance.RegistMsg<ResSceneObjDisMsg>(handler.HandActionObjDestroy, 2002);
        NetWorkBehavior.Instance.RegistMsg<ResSceneObjRunMsg>(handler.HandActionObjRun,2003);
    }


    public class ResObjShowHandler
    {
        NetAdapter netAdapter;
        public ResObjShowHandler(NetAdapter netAdapter) {
            this.netAdapter = netAdapter;
        }
        public void HandActionLogin(ResLoginMsg msg)
        {
            netAdapter.playerId = msg.playerId;
            netAdapter.m_LoginCallBack?.Invoke();
            netAdapter.StartCoroutine(netAdapter.SendPing(5));

        }

        public void HandActionObjShow(ResSceneObjShowMsg msg) 
        {

            MonsterScript.Instance.CreateMonster(msg);

        }
        public void HandActionObjDestroy(ResSceneObjDisMsg msg)
        {
            MonsterScript.Instance.RemoveSceneObj(msg);
        }
        public void HandActionObjRun(ResSceneObjRunMsg msg)
        {

        }
    }

    public class ReqLoginMsg
    {
        public string userName;
    }
    public class ResLoginMsg
    {
        public long playerId;
    }

    public class ReqSceneObjRunMsg
    {
        public long playerId;
        public List<PointInfo> roads = new();

    }

public class ReqEnterSceneMsg {

        public long playerId;
        public int x;
        public int y;
    }

    public class ResSceneObjShowMsg {
        public List<SceneObjInfo> objInfoList = new List<SceneObjInfo>();
    }
    public class ResSceneObjDisMsg {
        public List<long> disAppearList = new List<long>();
    }
    public class ResSceneObjRunMsg {
        public long objId;//对象id
        public int speed;//速度
        public List<PointInfo> roads = new List<PointInfo>();//跑步路径集合
    }

    public class SceneObjInfo {
        public long objId;// 唯一id
        public int modelId;// 模型sid
        public PointInfo currentPosition;//当前位置
        public List<PointInfo> roads;//跑步路径集合
    }
    public class PointInfo {
        public int x;
        public int y;
    }

    public void Login(string userName, Action action)
    {
        UserName = userName;
        m_LoginCallBack = action;
        ReqLoginMsg loginMsg = new ReqLoginMsg();
        loginMsg.userName = "tt";
        NetWorkBehavior.Instance.Send<ReqLoginMsg>(loginMsg, 1000);
    }
    IEnumerator SendPing(float interval)
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            PingMessage ping = new PingMessage();
            NetWorkBehavior.Instance.Send<PingMessage>(ping);
            m_PingSendTime = Time.time;
            yield return new WaitForSeconds(interval);
        }
    }

}

