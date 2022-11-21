using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NetAdapter;

public class GameInit : MonoBehaviour
{
    public static GameInit Instance;
    [Tooltip("������")]
    public string MainSceneName;
   // [Tooltip("���ⳡ��")]
   // public string ShipHouseSceneName;
    //[Tooltip("ս������")]
    //public string BattleSceneName;
    //����ر�ǩ
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

    //�ȸ�����������
    void OnILInitOver()
    {
        //2.����json
        FramworkEvent.InitJsonOver.AddEventHandler(JsonLoadOver);
        //StartCoroutine(SampleFactory.InitJson());

    }
    //json�������
    void JsonLoadOver()
    {
        FramworkEvent.InitJsonOver.RemoveEventHandler(JsonLoadOver);
        //3.��������
        var t1 = System.DateTime.Now;
    }
    // �����������
    void SceneLoadOver()
    {
        FramworkEvent.InitResPoolOver.AddEventHandler(GameObjPoolLoadOver);
        //4.����ؼ���
        //StartCoroutine(ResLoader.InitGameObjPool(ResPoolLabels));
    }
    // ����ؼ������
    void GameObjPoolLoadOver()
    {
        // 5.��ʼ���ֻ�SDK
        FramworkEvent.InitResPoolOver.RemoveEventHandler(GameObjPoolLoadOver);
        StopAllCoroutines();

        FramworkEvent.OnSDKInitOver.AddEventHandler(() => Destroy(gameObject));
       // GameComponent.Instance.InitAndroidSDK();
    }
}
