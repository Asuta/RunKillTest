using UnityEngine;

public class HandMoveCheckTest : MonoBehaviour
{
    public Transform rightHandTarget;
    public float moveSpeed = 1f;

    private Vector3 previousLocalPosition;

    [Header("玩家引用")]
    public VRPlayerMove playerMove; // 引用VRPlayerMove组件

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rightHandTarget != null)
        {
            previousLocalPosition = rightHandTarget.localPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandTarget == null)
            return;

        // 计算当前帧的本地位置
        Vector3 currentLocalPosition = rightHandTarget.localPosition;

        // 计算移动速度（每帧移动的距离）
        float moveDistance = Vector3.Distance(currentLocalPosition, previousLocalPosition);
        float speed = moveDistance / Time.deltaTime;

        // 检查速度是否超过阈值
        if (speed > moveSpeed)
        {
            Debug.Log("hahaha");
            playerMove.TriggerDash();
        }

        // 更新上一帧的位置
        previousLocalPosition = currentLocalPosition;
    }
}
