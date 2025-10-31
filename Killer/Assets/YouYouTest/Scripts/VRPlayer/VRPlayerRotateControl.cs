using UnityEngine;

public class VRPlayerRotateControl : MonoBehaviour
{
    private IWallSlidingProvider playerMove;
    public Transform vrCameraOffset;
    private Quaternion originalLocalRotation;
    private bool rotated = false;

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

            // 保存原始局部旋转，用于退出时恢复
            if (vrCameraOffset != null)
            {
                originalLocalRotation = vrCameraOffset.localRotation;
            }
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
    private void HandleEnterWallSliding(Vector3 wallNormal)
    {
        Debug.Log("VRPlayerRotateControl: 玩家进入贴墙滑行状态, normal=" + wallNormal);
        if (vrCameraOffset == null)
        {
            Debug.LogWarning("VRPlayerRotateControl: vrCameraOffset 未设置，无法旋转");
            return;
        }

        if (rotated) return; // 已经旋转过就不重复旋转

        // 计算相对于玩家朝向，法线方向在水平面上的左右侧，决定旋转方向的符号
        Vector3 flatNormal = wallNormal;
        flatNormal.y = 0f;
        if (flatNormal.sqrMagnitude < 0.0001f)
        {
            // 无效法线，跳过
            return;
        }

        float sign = Mathf.Sign(Vector3.SignedAngle(transform.forward, -flatNormal.normalized, Vector3.up));
        float angle = 20f * sign;

        vrCameraOffset.localRotation = originalLocalRotation * Quaternion.Euler(0f, 0f, angle);
        rotated = true;
    }

    /// <summary>
    /// 处理退出贴墙滑行事件
    /// </summary>
    private void HandleExitWallSliding(Vector3 wallNormal)
    {
        Debug.Log("VRPlayerRotateControl: 玩家退出贴墙滑行状态, restoring rotation");
        if (vrCameraOffset == null)
        {
            return;
        }

        if (!rotated) return;

        // 恢复原始局部旋转
        vrCameraOffset.localRotation = originalLocalRotation;
        rotated = false;
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
