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
    /// 统一抓取方法（包含设置状态和抓取）
    /// </summary>
    /// <param name="handTransform">抓住它的手部transform</param>
    void UnifiedGrab(Transform handTransform);
    
    /// <summary>
    /// 松开时调用（传入释放的手部Transform，用于在实现内判断是否仍被另一只手抓取）
    /// </summary>
    /// <param name="releasedHandTransform">释放的手部transform</param>
    void OnReleased(Transform releasedHandTransform);
}