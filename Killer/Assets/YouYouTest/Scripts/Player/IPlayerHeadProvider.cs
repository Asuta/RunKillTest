using UnityEngine;

/// <summary>
/// 提供玩家头部Transform的接口
/// </summary>
public interface IPlayerHeadProvider
{
    /// <summary>
    /// 获取玩家头部的Transform
    /// </summary>
    /// <returns>玩家头部的Transform</returns>
    Transform GetPlayerHead();
}