using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 全局事件系统
/// 使用方法：
/// 1. 继承 AutoCleanupBehaviour 而不是 MonoBehaviour
/// 2. 使用 RegisterEvent 方法注册事件，会自动在 OnDestroy 时清理
///
/// 示例：
/// public class MyScript : AutoCleanupBehaviour
/// {
///     void Start()
///     {
///         // 自动清理，无需手动 RemoveListener
///         RegisterEvent(GlobalEvent.CheckPointReset, OnCheckPointReset);
///         RegisterEvent(GlobalEvent.IsPlayChange, OnIsPlayChange);
///     }
///
///     void OnCheckPointReset()
///     {
///         Debug.Log("检查点重置");
///     }
///
///     void OnIsPlayChange(bool isPlay)
///     {
///         Debug.Log($"播放状态改变: {isPlay}");
///     }
/// }
///
/// 触发事件：
/// GlobalEvent.CheckPointReset.Invoke();
/// GlobalEvent.IsPlayChange.Invoke(true);
/// </summary>
public static class GlobalEvent
{
    /// <summary>
    /// 检查点重置事件
    /// </summary>
    public static readonly UnityEvent CheckPointReset = new UnityEvent();

    /// <summary>
    /// 检查点激活事件
    /// </summary>
    public static readonly UnityEvent<CheckPoint> CheckPointActivate = new UnityEvent<CheckPoint>();


    // /// <summary>
    // /// 强制死亡事件
    // /// </summary>
    // public static readonly UnityEvent ForceDeathActivate = new UnityEvent();



    /// <summary>
    /// 生成按钮点击事件
    /// </summary>
    public static readonly UnityEvent<GameObject, Transform> CreateButtonPoke = new UnityEvent<GameObject, Transform>();


    /// <summary>
    /// 模式按钮点击事件
    /// </summary>
    public static readonly UnityEvent ModeButtonPoke = new UnityEvent();

    /// <summary>
    /// 播放状态改变事件
    /// </summary>
    public static readonly UnityEvent<bool> IsPlayChange = new UnityEvent<bool>();


    /// <summary>
    /// 存档读取事件
    /// </summary>
    public static readonly UnityEvent<string> OnLoadSaveChange = new UnityEvent<string>();



    /// <summary>
    /// 选择成功事件
    /// </summary>
    public static readonly UnityEvent OnSelect = new UnityEvent();


    /// <summary>
    /// 指定select被删除事件
    /// </summary>
    public static readonly UnityEvent OnSelectDelete = new UnityEvent();

    /// <summary>
    /// 加载对象后设置选中状态事件
    /// </summary>
    public static readonly UnityEvent<List<GameObject>> OnLoadObjectsSetSelected = new UnityEvent<List<GameObject>>();

    /// <summary>
    /// 保存选中对象事件
    /// </summary>
    public static readonly UnityEvent<string> OnSaveSelectedObjects = new UnityEvent<string>();

    /// <summary>
    /// 射线命中UI状态变化事件
    /// </summary>
    public static readonly UnityEvent<bool> RaycastHittingUIChange = new UnityEvent<bool>();



}

