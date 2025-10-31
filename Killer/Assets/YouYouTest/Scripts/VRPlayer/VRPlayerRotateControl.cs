using UnityEngine;

public class VRPlayerRotateControl : MonoBehaviour
{
    private IWallSlidingProvider playerMove;
    public Transform vrCameraOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取VRPlayerMove组件的引用
        playerMove = GetComponent<IWallSlidingProvider>();
        
        if (playerMove != null)
        {
            // 订阅进入贴墙滑行事件
            playerMove.OnEnterWallSliding += HandleEnterWallSliding;
            
            // 订阅退出贴墙滑行事件
            playerMove.OnExitWallSliding += HandleExitWallSliding;
        }
        else
        {
            Debug.LogError("VRPlayerRotateControl: 未找到VRPlayerMove组件！");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 处理进入贴墙滑行事件
    /// </summary>
    private void HandleEnterWallSliding()
    {
        Debug.Log("VRPlayerRotateControl: 玩家进入贴墙滑行状态");
        // 在这里可以添加进入贴墙滑行时的逻辑，比如禁用旋转控制
    }

    /// <summary>
    /// 处理退出贴墙滑行事件
    /// </summary>
    private void HandleExitWallSliding()
    {
        Debug.Log("VRPlayerRotateControl: 玩家退出贴墙滑行状态");
        // 在这里可以添加退出贴墙滑行时的逻辑，比如重新启用旋转控制
    }

    /// <summary>
    /// 在组件销毁时取消事件订阅，避免内存泄漏
    /// </summary>
    private void OnDestroy()
    {
        if (playerMove != null)
        {
            // 取消订阅事件
            playerMove.OnEnterWallSliding -= HandleEnterWallSliding;
            playerMove.OnExitWallSliding -= HandleExitWallSliding;
        }
    }
}
