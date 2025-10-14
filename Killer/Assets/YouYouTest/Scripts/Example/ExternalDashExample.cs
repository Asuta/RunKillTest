using UnityEngine;

/// <summary>
/// 外部调用冲刺的示例脚本
/// 将此脚本附加到任何GameObject上，可以通过代码触发玩家冲刺
/// </summary>
public class ExternalDashExample : MonoBehaviour
{
    [Header("玩家引用")]
    public VRPlayerMove playerMove; // 引用VRPlayerMove组件
    
    [Header("测试设置")]
    public KeyCode testDashKey = KeyCode.Space; // 测试按键
    public Vector3 testDashDirection = Vector3.forward; // 测试冲刺方向
    
    void Update()
    {
        // 示例：按下空格键触发冲刺
        if (Input.GetKeyDown(testDashKey))
        {
            TriggerPlayerDash();
        }
        
        // 示例：按下左Shift键触发指定方向的冲刺
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            TriggerPlayerDashWithDirection();
        }
    }
    
    /// <summary>
    /// 触发玩家冲刺（使用默认方向）
    /// </summary>
    public void TriggerPlayerDash()
    {
        if (playerMove != null)
        {
            playerMove.TriggerDash();
            Debug.Log("外部调用触发冲刺（默认方向）");
        }
        else
        {
            Debug.LogWarning("未设置玩家引用！");
        }
    }
    
    /// <summary>
    /// 触发玩家冲刺（指定方向）
    /// </summary>
    public void TriggerPlayerDashWithDirection()
    {
        if (playerMove != null)
        {
            playerMove.TriggerDash(testDashDirection);
            Debug.Log($"外部调用触发冲刺（方向: {testDashDirection}）");
        }
        else
        {
            Debug.LogWarning("未设置玩家引用！");
        }
    }
    
    /// <summary>
    /// 触发玩家冲刺（自定义方向）
    /// </summary>
    /// <param name="direction">冲刺方向</param>
    public void TriggerPlayerDashWithDirection(Vector3 direction)
    {
        if (playerMove != null)
        {
            playerMove.TriggerDash(direction);
            Debug.Log($"外部调用触发冲刺（自定义方向: {direction}）");
        }
        else
        {
            Debug.LogWarning("未设置玩家引用！");
        }
    }
}