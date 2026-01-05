using UnityEngine;

public class GameEndUIMove : MonoBehaviour
{
    public Transform targetPosition;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotateSpeed = 5.0f;
    [SerializeField] private float distance = 2.0f;

    void Update()
    {
        if (targetPosition == null) return;

        // 1. 位置跟随 (Position Following)
        // (a) 获取目标在水平面上的平铺朝向 (仅取 Yaw)
        Vector3 targetForward = targetPosition.forward;
        targetForward.y = 0;
        if (targetForward.sqrMagnitude < 0.001f)
        {
            targetForward = Vector3.ProjectOnPlane(targetPosition.up, Vector3.up).normalized;
        }
        else
        {
            targetForward.Normalize();
        }

        // (b) 计算在水平面内的目标位置
        // 目标位置 = 目标点位置 + 目标水平朝向 * 距离
        Vector3 desiredPosition = targetPosition.position + targetForward * distance;
        
        // 限制在水平面：保持 Y 轴与目标一致，或者保持当前 Y 轴
        // 根据“移动仅限制在水平面内”以及“绕着它转动”，通常指在目标所在的高度平面
        desiredPosition.y = targetPosition.position.y;

        // (c) 使用 Lerp 进行平滑移动
        transform.position = Vector3.Lerp(transform.position, desiredPosition, moveSpeed * Time.deltaTime);

        // 2. 旋转跟随 (Rotation Following)
        // 物体需要始终朝向 TargetPosition
        Vector3 lookDir = targetPosition.position - transform.position;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            // 同样使用插值保持平滑一致性
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }
}
