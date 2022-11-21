using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NetAdapter;

public class GameInit : MonoBehaviour
{
    public static GameInit Instance;
    [Tooltip("主场景")]
    public string MainSceneName;
   // [Tooltip("机库场景")]
   // public string ShipHouseSceneName;
    //[Tooltip("战斗场景")]
    //public string BattleSceneName;
    //对象池标签
    public string[] ResPoolLabels;


    private void Awake()
    {
        Instance = this;
        GameObject dontDestroy = new GameObject("DontDestroy");
        dontDestroy.tag = "DontDestroy";
        DontDestroyOnLoad(dontDestroy);
        //DontDestroyOnLoad(this);
        transform.SetParent(dontDestroy.transform);
    }

    public void EnterGame()
    {
        //var iLLoader = GetComponent<ILLoader>();
        //iLLoader.OnInitOver += OnILInitOver;
        //iLLoader.InitIL();


        SceneManager.LoadScene("Battle");
        ReqEnterSceneMsg msg = new ReqEnterSceneMsg();
        msg.playerId = NetAdapter.Instance.playerId;
        msg.x = 10;
        msg.y = 0;
        NetWorkBehavior.Instance.Send<ReqEnterSceneMsg>(msg,1001);
    }

    //热更代码加载完成
    void OnILInitOver()
    {
        //2.加载json
        FramworkEvent.InitJsonOver.AddEventHandler(JsonLoadOver);
        //StartCoroutine(SampleFactory.InitJson());

    }
    //json加载完成
    void JsonLoadOver()
    {
        FramworkEvent.InitJsonOver.RemoveEventHandler(JsonLoadOver);
        //3.场景加载
        var t1 = System.DateTime.Now;
    }
    // 场景加载完成
    void SceneLoadOver()
    {
        FramworkEvent.InitResPoolOver.AddEventHandler(GameObjPoolLoadOver);
        //4.对象池加载
        //StartCoroutine(ResLoader.InitGameObjPool(ResPoolLabels));
    }
    // 对象池加载完成
    void GameObjPoolLoadOver()
    {
        // 5.初始化手机SDK
        FramworkEvent.InitResPoolOver.RemoveEventHandler(GameObjPoolLoadOver);
        StopAllCoroutines();

        FramworkEvent.OnSDKInitOver.AddEventHandler(() => Destroy(gameObject));
       // GameComponent.Instance.InitAndroidSDK();
    }
}
