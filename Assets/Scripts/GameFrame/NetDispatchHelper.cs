using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf;




[RequireComponent(typeof(NetWorkBehavior))]
public class NetDispatchHelper : MonoBehaviour {

    void Update () {
        NetWorkBehavior.Instance.Dispatch();
	}
}
