using UnityEngine;

/// <summary>
/// 冲刺功能提供者接口
/// 定义了触发冲刺的基本功能，用于解耦外部脚本与具体的玩家移动实现
/// </summary>
public interface IDashProvider
{
    /// <summary>
    /// 触发冲刺（使用默认方向）
    /// </summary>
    void TriggerDash();
    
    /// <summary>
    /// 触发冲刺（指定方向）
    /// </summary>
    /// <param name="direction">冲刺方向</param>
    void TriggerDash(Vector3 direction);
}