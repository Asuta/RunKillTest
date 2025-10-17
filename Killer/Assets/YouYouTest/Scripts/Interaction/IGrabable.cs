using UnityEngine;

/// <summary>
/// 可被抓取对象的接口
/// </summary>
public interface IGrabable
{
    /// <summary>
    /// 获取对象的Transform
    /// </summary>
    Transform ObjectTransform { get; }
    
    /// <summary>
    /// 获取对象的GameObject
    /// </summary>
    GameObject ObjectGameObject { get; }
    
    /// <summary>
    /// 被抓住时调用
    /// </summary>
    /// <param name="handTransform">抓住它的手部transform</param>
    void OnGrabbed(Transform handTransform);
    
    /// <summary>
    /// 松开时调用
    /// </summary>
    void OnReleased();
}