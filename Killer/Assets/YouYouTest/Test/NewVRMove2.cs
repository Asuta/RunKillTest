using System;
using UnityEngine;

namespace YouYouTest.VRMove2
{
    // 移动状态枚举
    public enum MovementState
    {
        Grounded
    }

// 状态基类
public abstract class MovementStateBase
{
    protected NewVRMove2 controller;

    public MovementStateBase(NewVRMove2 controller)
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
    public GroundedState(NewVRMove2 controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("进入地面状态");
    }

    public override void Update()
    {
        // 在地面状态下的移动逻辑
        controller.GroundMovement();
    }

    public override void Exit()
    {
        Debug.Log("离开地面状态");
    }
}


public class NewVRMove2 : MonoBehaviour
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
    }

    // 地面移动逻辑
    public void GroundMovement()
    {
        MoveLoop();
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
        
        // 读取当前trigger状态（布尔值）
        bool leftTriggerPressed = InputActionsManager.Actions.XRILeftInteraction.Activate.IsPressed();
        bool rightTriggerPressed = InputActionsManager.Actions.XRIRightInteraction.Activate.IsPressed();

        // 1) 先基于当前sphere与target的"真实位置"计算方向向量（**必须在修改球体位置之前计算**）
        if (leftSphere != null && leftSphereTarget != null)
        {
            Vector3 rawLeftDirection = leftSphere.position - leftSphereTarget.position;
            float leftMagnitude = rawLeftDirection.magnitude; // 记录原始长度
            
            // 如果同时按住grip和trigger，保留完整的3D方向（包括Y轴）
            if (leftGripPressed && leftTriggerPressed)
            {
                leftDirection = rawLeftDirection.normalized * leftMagnitude; // 保留完整的3D方向
            }
            else
            {
                // 否则只使用水平方向的移动
                Vector3 leftHorizontalDirection = new Vector3(rawLeftDirection.x, 0, rawLeftDirection.z).normalized; // 拍平并归一化得到方向
                leftDirection = leftHorizontalDirection * leftMagnitude; // 用方向加上原始长度
            }
        }
        else
        {
            leftDirection = Vector3.zero;
        }

        if (rightSphere != null && rightSphereTarget != null)
        {
            Vector3 rawRightDirection = rightSphere.position - rightSphereTarget.position;
            float rightMagnitude = rawRightDirection.magnitude; // 记录原始长度
            
            // 如果同时按住grip和trigger，保留完整的3D方向（包括Y轴）
            if (rightGripPressed && rightTriggerPressed)
            {
                rightDirection = rawRightDirection.normalized * rightMagnitude; // 保留完整的3D方向
            }
            else
            {
                // 否则只使用水平方向的移动
                Vector3 rightHorizontalDirection = new Vector3(rawRightDirection.x, 0, rawRightDirection.z).normalized; // 拍平并归一化得到方向
                rightDirection = rightHorizontalDirection * rightMagnitude; // 用方向加上原始长度
            }
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

        // 检查是否有任何手同时按住了grip和trigger（3D移动模式）
        bool is3DMovementMode = (leftGripPressed && leftTriggerPressed) || (rightGripPressed && rightTriggerPressed);
        
        // 根据移动模式选择不同的颜色进行可视化
        Color velocityColor = is3DMovementMode ? Color.cyan : Color.green; // 3D模式用青色，普通模式用绿色
        
        if (linePosition != null)
        {
            Debug.DrawLine(linePosition.position, linePosition.position + finalVelocity * 11f, velocityColor, 0.1f);
        }

        // 计算最终速度
        Vector3 speed = finalVelocity * finalVelocityMultiplier;
        
        if (is3DMovementMode)
        {
            // 在3D移动模式下，完全应用计算出的速度（包括Y轴）
            thisRb.linearVelocity = speed;
            
            // 调试信息
            if (Time.frameCount % 30 == 0) // 每30帧打印一次，避免日志过多
            {
                Debug.Log("3D移动模式激活 - 应用完整速度: " + speed);
            }
        }
        else
        {
            // 在普通模式下，只应用XZ轴的速度，保持原有的Y轴速度
            thisRb.linearVelocity = new Vector3(speed.x, thisRb.linearVelocity.y, speed.z);
        }

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
}
