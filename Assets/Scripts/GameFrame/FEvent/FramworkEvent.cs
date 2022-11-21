using System;
using UnityEngine;

public static class FramworkEvent
{
    // 对象池加载完成
    public static FEvent InitResPoolOver = new FEvent();
    // json初始化完成
    public static FEvent InitJsonOver = new FEvent();
    // 手机sdk初始化完成
    public static FEvent OnSDKInitOver = new FEvent();
    // 资源数据准备完成，正式进入游戏流程
    public static FEvent OnGameStart = new FEvent();
    // 弹出提示
    public static FEvent<string> ToastWarning = new FEvent<string>();
    // 语言加载完成
    public static FEvent LanguageInit = new FEvent();
    // 放大缩小
    public static FEvent<float> OnPinch = new FEvent<float>();
    // CameraTarget碰撞触发
    public static FEvent<bool, Transform> OnCameraTargetTrigger = new FEvent<bool, Transform>();
    //滑动开始监听
    public static FEvent<float> PinchBegin = new FEvent<float>();
}
