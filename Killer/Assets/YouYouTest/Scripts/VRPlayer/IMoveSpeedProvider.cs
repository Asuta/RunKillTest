using UnityEngine;

/// <summary>
/// 移动速度提供者接口
/// </summary>
public interface IMoveSpeedProvider
{
    /// <summary>
    /// 设置额外的移动速度加成
    /// </summary>
    /// <param name="additionalSpeed">要添加的额外速度</param>
    void SetAdditionalMoveSpeed(float additionalSpeed);

    /// <summary>
    /// 获取当前的实际移动速度（基础速度 + 额外速度）
    /// </summary>
    /// <returns>实际移动速度</returns>
    float GetActualMoveSpeed();
}