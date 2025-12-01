using System;
using UnityEngine;

// 移动状态枚举
public enum MovementState
{
    Grounded,
    Jumping
}

// 状态基类
public abstract class MovementStateBase
{
    protected NewVRMove controller;

    public MovementStateBase(NewVRMove controller)
    {
        this.controller = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

// 地面状态
public class GroundedState : MovementStateBase
{
    public GroundedState(NewVRMove controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("进入地面状态");
    }

    public override void Update()
    {
        // 在地面状态下的移动逻辑
        controller.GroundMovement();

        // 地面状态下不再通过地面检测切换到跳跃状态
        // 只有通过Jump()方法才会切换到跳跃状态
    }

    public override void Exit()
    {
        Debug.Log("离开地面状态");
    }
}

// 跳跃状态
public class JumpingState : MovementStateBase
{
    public JumpingState(NewVRMove controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("进入跳跃状态");
        // 重置跳跃状态计时器
        controller.jumpStateTimer = 0f;
    }

    public override void Update()
    {
        // 在跳跃状态下的移动逻辑
        controller.AirMovement();

        // 更新跳跃状态计时器
        controller.jumpStateTimer += Time.deltaTime;

        // 只有在跳跃状态持续时间超过最小值且检测到地面时才切换回地面状态
        if (controller.isOnGround && controller.jumpStateTimer >= NewVRMove.JUMP_STATE_MIN_DURATION)
        {
            controller.ChangeState(new GroundedState(controller));
        }
    }

    public override void Exit()
    {
        Debug.Log("离开跳跃状态");
    }
}

public class NewVRMove : MonoBehaviour
{
    public Transform bodyPosition;

    public Transform leftSphere;
    public Transform leftSphereTarget;

    public Vector3 leftDirection;

    public Transform rightSphere;
    public Transform rightSphereTarget;

    public Vector3 rightDirection;


    public Vector3 residualVelocity;
    public float speedDecay;
    public float speedDecayAdd;

    public Vector3 finalVelocity;
    public float finalVelocityMultiplier;

    public Rigidbody thisRb;

    public bool isOnGround;

    // 状态机相关
    private MovementStateBase currentState;
    public MovementState CurrentStateType { get; private set; }

    // 跳跃状态计时器
    public float jumpStateTimer = 0f;
    public const float JUMP_STATE_MIN_DURATION = 0.2f; // 跳跃状态最小持续时间

    [Header("地面检测参数")]
    [Tooltip("球体检测的半径")]
    public float groundCheckRadius = 0.2f;

    [Tooltip("球体检测的距离")]
    public float groundCheckDistance = 0.3f;

    [Tooltip("指定要检测的地面层")]
    public LayerMask groundLayerMask = -1; // -1表示检测所有层
    [Header("转向相关")]
    public Transform vrOrigin;
    public Transform cameraOffset;
    public Transform playerHead;

    [Header("转向参数")]
    [Tooltip("单次转向角度（度）")]
    public float rotationAngle = 30f;

    [Tooltip("转向检测阈值")]
    public float rotationThreshold = 0.8f;

    [Header("跳跃参数")]
    // public float maxJumpForce = 5f;
    public float maxJumpForceY = 10f;

    // 转向状态跟踪
    private bool wasRotatingLeft = false;
    private bool wasRotatingRight = false;





    //test
    public Transform linePosition;




    [Tooltip("Lerp跟随速度，值越大跟随越快")]
    public float lerpSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初始化grip状态
        lastLeftGripPressed = false;
        lastRightGripPressed = false;

        // 初始化状态机，默认进入地面状态
        ChangeState(new GroundedState(this));
    }

    // Update is called once per frame
    void Update()
    {
        // 更新当前状态
        currentState?.Update();

        // 处理转向逻辑
        HandleRotation();
    }

    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        GroundCheck();
    }

    // 状态切换方法
    public void ChangeState(MovementStateBase newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        // 更新状态类型
        if (newState is GroundedState)
            CurrentStateType = MovementState.Grounded;
        else if (newState is JumpingState)
            CurrentStateType = MovementState.Jumping;
    }

    // 地面移动逻辑
    public void GroundMovement()
    {
        MoveLoop();
    }

    // 空中移动逻辑
    public void AirMovement()
    {
        // 在空中的移动逻辑，可能有不同的控制方式
        AirMoveLoop();
    }

    public void Jump(Vector3 jumpForce)
    {
        // 允许在任何状态下跳跃（多段跳）
        if (thisRb != null)
        {
            // 获取当前速度，保持XZ轴速度不变
            Vector3 currentVelocity = thisRb.linearVelocity;
            
            // 只取跳跃力的Y轴分量，并限制其最大值
            float yForce = Mathf.Min(jumpForce.y, maxJumpForceY);
            Vector3 yOnlyJumpForce = new Vector3(0, yForce, 0);
            
            // 施加只包含Y轴的跳跃力
            thisRb.AddForce(yOnlyJumpForce, ForceMode.Impulse);
            
            Debug.Log("执行跳跃，施加Y轴力: " + yOnlyJumpForce + "，限制Y轴最大施力为: " + maxJumpForceY);

            // 如果当前不是跳跃状态，切换到跳跃状态
            if (CurrentStateType != MovementState.Jumping)
            {
                ChangeState(new JumpingState(this));
            }
            else
            {
                // 如果已经在跳跃状态，重置计时器以延长最小持续时间
                jumpStateTimer = 0f;
                Debug.Log("多段跳！重置跳跃状态计时器");
            }
        }
    }

    private void GroundCheck()
    {
        // 从当前物体位置开始检测
        Vector3 spherePosition = bodyPosition.position;
        Vector3 direction = Vector3.down; // 向下检测

        // 创建球体投射的参数
        RaycastHit hitInfo;

        // 执行球体投射，检测指定层的地面
        bool hitGround = Physics.SphereCast(
            spherePosition,
            groundCheckRadius,
            direction,
            out hitInfo,
            groundCheckDistance,
            groundLayerMask
        );

        // 更新地面状态
        isOnGround = hitGround;

        // 可选：在Scene视图中可视化检测范围（仅在编辑器中可见）
#if UNITY_EDITOR
        // 使用CapsuleWireframeDrawer绘制球体投射的可视化
        // 将球体投射转换为胶囊体表示（避免 point1==point2 导致方向向量为零的问题）
        Vector3 sphereCenter = spherePosition;
        Vector3 castDirection = direction * groundCheckDistance;
        Color gizmoColor = isOnGround ? Color.green : Color.red;

        // 构造非常短的胶囊（顶部/底部点），确保非零高度并与对象的 up 方向一致
        float smallHalfHeight = Mathf.Max(0.001f, groundCheckRadius * 0.01f);
        Vector3 point1 = sphereCenter - transform.up * smallHalfHeight;
        Vector3 point2 = sphereCenter + transform.up * smallHalfHeight;

        CapsuleWireframeDrawer.DrawCapsuleCastGizmo(
            point1,
            point2,
            groundCheckRadius,
            castDirection,
            gizmoColor
        );

        // 显示检测到的层信息
        if (hitGround)
        {
            // Debug.Log("检测到地面: " + LayerMask.LayerToName(hitInfo.collider.gameObject.layer) + " 层");
        }
#endif
    }


    private void MoveLoop()
    {
        // 读取当前grip状态（布尔值）
        bool leftGripPressed = InputActionsManager.Actions.XRILeftInteraction.Select.IsPressed();
        bool rightGripPressed = InputActionsManager.Actions.XRIRightInteraction.Select.IsPressed();

        // 1) 先基于当前sphere与target的"真实位置"计算方向向量（**必须在修改球体位置之前计算**）
        if (leftSphere != null && leftSphereTarget != null)
        {
            Vector3 rawLeftDirection = leftSphere.position - leftSphereTarget.position;
            float leftMagnitude = rawLeftDirection.magnitude; // 记录原始长度
            Vector3 leftHorizontalDirection = new Vector3(rawLeftDirection.x, 0, rawLeftDirection.z).normalized; // 拍平并归一化得到方向
            leftDirection = leftHorizontalDirection * leftMagnitude; // 用方向加上原始长度
        }
        else
        {
            leftDirection = Vector3.zero;
        }

        if (rightSphere != null && rightSphereTarget != null)
        {
            Vector3 rawRightDirection = rightSphere.position - rightSphereTarget.position;
            float rightMagnitude = rawRightDirection.magnitude; // 记录原始长度
            Vector3 rightHorizontalDirection = new Vector3(rawRightDirection.x, 0, rawRightDirection.z).normalized; // 拍平并归一化得到方向
            rightDirection = rightHorizontalDirection * rightMagnitude; // 用方向加上原始长度
        }
        else
        {
            rightDirection = Vector3.zero;
        }

        // 2) 在把球体位置写回target之前，检测"松手瞬间"，把当时捕获到的方向累加到residualVelocity
        if (lastLeftGripPressed && !leftGripPressed)
        {
            residualVelocity += leftDirection;
            Debug.Log("左手松开（捕获前一帧位置），累加leftDirection到residualVelocity: " + leftDirection);
        }

        if (lastRightGripPressed && !rightGripPressed)
        {
            residualVelocity += rightDirection;
            Debug.Log("右手松开（捕获前一帧位置），累加rightDirection到residualVelocity: " + rightDirection);
        }

        // 3) 更新grip历史状态（用于下一帧检测）
        lastLeftGripPressed = leftGripPressed;
        lastRightGripPressed = rightGripPressed;

        // 4) 现在执行跟随逻辑（Lerp 或 直接设置）
        if (leftGripPressed && leftSphere != null && leftSphereTarget != null)
        {
            leftSphere.position = Vector3.Lerp(leftSphere.position, leftSphereTarget.position, lerpSpeed * Time.deltaTime);
            leftSphere.rotation = Quaternion.Slerp(leftSphere.rotation, leftSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
        else if (leftSphere != null && leftSphereTarget != null)
        {
            // 松开时直接同步位置（这一步在计算并累加方向之后执行）
            leftSphere.position = leftSphereTarget.position;
            leftSphere.rotation = leftSphereTarget.rotation;
        }

        if (rightGripPressed && rightSphere != null && rightSphereTarget != null)
        {
            rightSphere.position = Vector3.Lerp(rightSphere.position, rightSphereTarget.position, lerpSpeed * Time.deltaTime);
            rightSphere.rotation = Quaternion.Slerp(rightSphere.rotation, rightSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
        else if (rightSphere != null && rightSphereTarget != null)
        {
            rightSphere.position = rightSphereTarget.position;
            rightSphere.rotation = rightSphereTarget.rotation;
        }

        // 5) 每帧都让residualVelocity以speedDecay衰减（使用MoveTowards避免反向）
        // 当任意grip键按住时，使用增加的衰减速度
        float currentSpeedDecay = (leftGripPressed || rightGripPressed) ? speedDecay + speedDecayAdd : speedDecay;
        residualVelocity = Vector3.MoveTowards(residualVelocity, Vector3.zero, currentSpeedDecay * Time.deltaTime);

        // 日志与可视化（只显示residualVelocity）
        // Debug.Log("当前residualVelocity: " + residualVelocity);
        if (linePosition != null)
        {
            Debug.DrawLine(linePosition.position, linePosition.position + residualVelocity * 11f, Color.red, 0.1f);
        }


        // 只有在对应grip键按住时才将方向向量加到最终速度中
        Vector3 activeDirection = Vector3.zero;
        if (leftGripPressed)
        {
            activeDirection += leftDirection;
        }
        if (rightGripPressed)
        {
            activeDirection += rightDirection;
        }

        finalVelocity = residualVelocity + activeDirection;

        if (linePosition != null)
        {
            Debug.DrawLine(linePosition.position, linePosition.position + finalVelocity * 11f, Color.green, 0.1f);
        }

        // thisRb.linearVelocity = finalVelocity * finalVelocityMultiplier;
        Vector3 speed = finalVelocity * finalVelocityMultiplier;
        thisRb.linearVelocity = new Vector3(speed.x, thisRb.linearVelocity.y, speed.z);

    }

    // 空中移动循环
    private void AirMoveLoop()
    {
        // 读取当前grip状态（布尔值）
        bool leftGripPressed = InputActionsManager.Actions.XRILeftInteraction.Select.IsPressed();
        bool rightGripPressed = InputActionsManager.Actions.XRIRightInteraction.Select.IsPressed();

        // 1) 先基于当前sphere与target的"真实位置"计算方向向量（**必须在修改球体位置之前计算**）
        if (leftSphere != null && leftSphereTarget != null)
        {
            Vector3 rawLeftDirection = leftSphere.position - leftSphereTarget.position;
            float leftMagnitude = rawLeftDirection.magnitude; // 记录原始长度
            Vector3 leftHorizontalDirection = new Vector3(rawLeftDirection.x, 0, rawLeftDirection.z).normalized; // 拍平并归一化得到方向
            leftDirection = leftHorizontalDirection * leftMagnitude; // 用方向加上原始长度
        }
        else
        {
            leftDirection = Vector3.zero;
        }

        if (rightSphere != null && rightSphereTarget != null)
        {
            Vector3 rawRightDirection = rightSphere.position - rightSphereTarget.position;
            float rightMagnitude = rawRightDirection.magnitude; // 记录原始长度
            Vector3 rightHorizontalDirection = new Vector3(rawRightDirection.x, 0, rawRightDirection.z).normalized; // 拍平并归一化得到方向
            rightDirection = rightHorizontalDirection * rightMagnitude; // 用方向加上原始长度
        }
        else
        {
            rightDirection = Vector3.zero;
        }

        // 2) 在空中状态下，不处理松手瞬间的方向累加

        // 3) 更新grip历史状态（用于下一帧检测）
        lastLeftGripPressed = leftGripPressed;
        lastRightGripPressed = rightGripPressed;

        // 4) 现在执行跟随逻辑（Lerp 或 直接设置）
        if (leftGripPressed && leftSphere != null && leftSphereTarget != null)
        {
            leftSphere.position = Vector3.Lerp(leftSphere.position, leftSphereTarget.position, lerpSpeed * Time.deltaTime);
            leftSphere.rotation = Quaternion.Slerp(leftSphere.rotation, leftSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
        else if (leftSphere != null && leftSphereTarget != null)
        {
            // 松开时直接同步位置（这一步在计算并累加方向之后执行）
            leftSphere.position = leftSphereTarget.position;
            leftSphere.rotation = leftSphereTarget.rotation;
        }

        if (rightGripPressed && rightSphere != null && rightSphereTarget != null)
        {
            rightSphere.position = Vector3.Lerp(rightSphere.position, rightSphereTarget.position, lerpSpeed * Time.deltaTime);
            rightSphere.rotation = Quaternion.Slerp(rightSphere.rotation, rightSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
        else if (rightSphere != null && rightSphereTarget != null)
        {
            rightSphere.position = rightSphereTarget.position;
            rightSphere.rotation = rightSphereTarget.rotation;
        }

        // 5) 在空中状态下，不对速度做任何处理，让跳跃力完全生效
        // 不修改residualVelocity，不衰减速度

        // 日志与可视化（只显示当前速度）
        if (linePosition != null)
        {
            Debug.DrawLine(linePosition.position, linePosition.position + thisRb.linearVelocity * 11f, Color.blue, 0.1f); // 空中用蓝色
        }

        // 6) 在空中状态下，不对速度做任何处理
        // 不修改finalVelocity，不应用任何速度变化
    }

    // 用于检测grip键状态变化

    private bool lastLeftGripPressed;
    private bool lastRightGripPressed;

    // 处理转向逻辑
    private void HandleRotation()
    {
        // 读取右手柄摇杆输入
        Vector2 rightStickInput = InputActionsManager.Actions.XRIRightLocomotion.Move.ReadValue<Vector2>();

        // 右旋转 - 摇杆推到最右边时才旋转一次
        if (rightStickInput.x > rotationThreshold && !wasRotatingRight)
        {
            RotateCameraOffset(rotationAngle);
            wasRotatingRight = true;
        }
        else if (rightStickInput.x <= rotationThreshold)
        {
            wasRotatingRight = false;
        }

        // 左旋转 - 摇杆推到最左边时才旋转一次
        if (rightStickInput.x < -rotationThreshold && !wasRotatingLeft)
        {
            RotateCameraOffset(-rotationAngle);
            wasRotatingLeft = true;
        }
        else if (rightStickInput.x >= -rotationThreshold)
        {
            wasRotatingLeft = false;
        }
    }

    // 旋转cameraOffset，以playerHead为中心
    private void RotateCameraOffset(float angle)
    {
        if (cameraOffset == null || playerHead == null)
        {
            Debug.LogWarning("cameraOffset或playerHead未设置！");
            return;
        }

        // 计算旋转中心（playerHead的位置）
        Vector3 rotationCenter = playerHead.position;

        // 计算cameraOffset相对于旋转中心的位置
        Vector3 relativePosition = cameraOffset.position - rotationCenter;

        // 创建旋转（绕Y轴）
        Quaternion rotation = Quaternion.Euler(0, angle, 0);

        // 旋转相对位置
        Vector3 rotatedPosition = rotation * relativePosition;

        // 应用新位置
        cameraOffset.position = rotationCenter + rotatedPosition;

        // 旋转cameraOffset的朝向
        cameraOffset.rotation = rotation * cameraOffset.rotation;

        Debug.Log($"转向: {angle}度，旋转中心: {rotationCenter}");
    }
}
