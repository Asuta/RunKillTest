using UnityEngine;

/// <summary>
/// Hook冲刺功能提供者接口
/// 定义了处理Hook冲刺逻辑的基本功能，用于解耦外部脚本与具体的玩家移动实现
/// </summary>
public interface IHookDashProvider
{
    /// <summary>
    /// 处理Hook冲刺逻辑
    /// 检测hook冲刺输入并更新hook冲刺状态
    /// </summary>
    void OutHandleHookDash();
}