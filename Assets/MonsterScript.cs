using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NetAdapter;

public class MonsterScript : MonoBehaviour
{
    public static MonsterScript Instance;
    public GameObject ostrich;
    public GameObject bear;
    public GameObject buffalo;
    public GameObject zebra;
    public GameObject player;
    public GameObject mainCamera;
    private PlayerControl playerControl;
    private bool initPlayer;
    private Dictionary<long, GameObject> scenObjMap = new Dictionary<long, GameObject>();

    private void Awake()
    {
        Instance = this;


    }
    public  void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RemoveSceneObj(ResSceneObjDisMsg msg) {

        foreach (long objId in msg.disAppearList)
        {
            if (scenObjMap.TryGetValue(objId, out GameObject gameObj))
            {
                scenObjMap.Remove(objId);
                Destroy(gameObj);
                
            }
        }

    }


    public void CreateMonster(ResSceneObjShowMsg msg) 
    {

        List<SceneObjInfo> objInfoList = msg.objInfoList;
        foreach(SceneObjInfo objInfo in objInfoList)
        {
            Vector3 position = new Vector3(objInfo.currentPosition.x,0 , objInfo.currentPosition.y);
            if (objInfo.modelId == 1)
            {
                scenObjMap.Add(objInfo.objId, Instantiate(ostrich, position, Quaternion.identity));
            } else if (objInfo.modelId == 2)
            {
                scenObjMap.Add(objInfo.objId, Instantiate(bear, position, Quaternion.identity));
            } else if (objInfo.modelId == 3)
            {
                scenObjMap.Add(objInfo.objId, Instantiate(buffalo, position, Quaternion.identity));
            } else if (objInfo.modelId == 4) 
            {
                scenObjMap.Add(objInfo.objId, Instantiate(zebra, position, Quaternion.identity));
            }
            else if (objInfo.modelId == 100)
            {
                if (initPlayer)
                {
                    scenObjMap.Add(objInfo.objId, Instantiate(player, position, Quaternion.identity));
                    if (objInfo.roads!=null&& objInfo.roads.Count>0) 
                    {
                        List<PointInfo> roads = objInfo.roads;
                        PointInfo pInfo = roads[roads.Count-1];
                        Vector3 vec = new();
                        vec.x = pInfo.x;
                        vec.z = pInfo.y;
                        StartCoroutine(playerControl.InertialRun(vec));
                    }
                    return;
                }
                initPlayer = true;

                Vector3 positionCamera = new Vector3(50, 2, 40);
                GameObject mainCameraObj = Instantiate(mainCamera, positionCamera, Quaternion.identity);
                SurroundCamera scriptCamera = mainCameraObj.GetComponent<SurroundCamera>();

                GameObject playerObj = Instantiate(player, position, Quaternion.identity);
                GameObject foucs = GameObject.Find("foucs");

                scriptCamera.focus = foucs.transform;
                playerControl = playerObj.GetComponent<PlayerControl>();
                playerControl.SetPlayer(playerObj, objInfo.objId);
                scenObjMap.Add(objInfo.objId, playerObj);
            }
        }
    }
}
