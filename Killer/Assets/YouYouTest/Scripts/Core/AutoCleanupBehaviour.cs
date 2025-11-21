using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 自动清理事件监听器的Behaviour基类
/// 继承此类的脚本会自动在OnDestroy时清理注册的UnityEvent监听器
/// </summary>
public class AutoCleanupBehaviour : MonoBehaviour
{
    private readonly Dictionary<object, System.Delegate> registeredEvents = new Dictionary<object, System.Delegate>();

    /// <summary>
    /// 注册UnityEvent监听器，会自动在OnDestroy时清理
    /// </summary>
    /// <param name="unityEvent">要监听的UnityEvent</param>
    /// <param name="listener">监听器方法</param>
    protected void RegisterEvent(UnityEvent unityEvent, UnityAction listener)
    {
        if (unityEvent != null && listener != null)
        {
            unityEvent.AddListener(listener);
            registeredEvents[(object)unityEvent] = listener;
        }
    }

    /// <summary>
    /// 注册带参数的UnityEvent监听器，会自动在OnDestroy时清理
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="unityEvent">要监听的UnityEvent</param>
    /// <param name="listener">监听器方法</param>
    protected void RegisterEvent<T>(UnityEvent<T> unityEvent, UnityAction<T> listener)
    {
        if (unityEvent != null && listener != null)
        {
            unityEvent.AddListener(listener);
            registeredEvents[(object)unityEvent] = listener;
        }
    }

    /// <summary>
    /// 注册带两个参数的UnityEvent监听器，会自动在OnDestroy时清理
    /// </summary>
    /// <typeparam name="T0">第一个参数类型</typeparam>
    /// <typeparam name="T1">第二个参数类型</typeparam>
    /// <param name="unityEvent">要监听的UnityEvent</param>
    /// <param name="listener">监听器方法</param>
    protected void RegisterEvent<T0, T1>(UnityEvent<T0, T1> unityEvent, UnityAction<T0, T1> listener)
    {
        if (unityEvent != null && listener != null)
        {
            unityEvent.AddListener(listener);
            registeredEvents[(object)unityEvent] = listener;
        }
    }

    /// <summary>
    /// 在对象销毁时自动清理所有注册的事件监听器
    /// </summary>
    protected virtual void OnDestroy()
    {
        foreach (var kvp in registeredEvents)
        {
            var unityEvent = kvp.Key;
            var listener = kvp.Value;

            if (unityEvent != null && listener != null)
            {
                // 根据事件类型移除监听器
                if (unityEvent is UnityEvent)
                {
                    (unityEvent as UnityEvent)?.RemoveListener(listener as UnityAction);
                }
                else if (unityEvent is UnityEvent<string>)
                {
                    (unityEvent as UnityEvent<string>)?.RemoveListener(listener as UnityAction<string>);
                }
                else if (unityEvent is UnityEvent<bool>)
                {
                    (unityEvent as UnityEvent<bool>)?.RemoveListener(listener as UnityAction<bool>);
                }
                else if (unityEvent is UnityEvent<GameObject>)
                {
                    (unityEvent as UnityEvent<GameObject>)?.RemoveListener(listener as UnityAction<GameObject>);
                }
                else if (unityEvent is UnityEvent<GameObject, Transform>)
                {
                    (unityEvent as UnityEvent<GameObject, Transform>)?.RemoveListener(listener as UnityAction<GameObject, Transform>);
                }
                else if (unityEvent is UnityEvent<List<GameObject>>)
                {
                    (unityEvent as UnityEvent<List<GameObject>>)?.RemoveListener(listener as UnityAction<List<GameObject>>);
                }
            }
        }

        registeredEvents.Clear();
    }
}