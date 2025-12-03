using System;
using UnityEngine;

namespace YouYouTest.VRMove2
{
    // 移动状态枚举
    public enum MovementState
    {
        Grounded,
        Airborne
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

            // 检查是否离开地面，如果是则切换到空中状态
            if (!controller.isOnGround)
            {
                controller.ChangeState(new AirborneState(controller));
            }
        }

        public override void Exit()
        {
            Debug.Log("离开地面状态");
        }
    }

    // 空中状态
    public class AirborneState : MovementStateBase
    {
        public AirborneState(NewVRMove2 controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("进入空中状态");
        }

        public override void Update()
        {
            // 在空中状态下的移动逻辑
            controller.AirborneMovement();

            // 检查是否回到地面，如果是则切换到地面状态
            if (controller.isOnGround)
            {
                controller.ChangeState(new GroundedState(controller));
            }
        }

        public override void Exit()
        {
            Debug.Log("离开空中状态");
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
        public float maxJumpForceY;

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
            DebugGraph.Log("final speeeeed", thisRb.linearVelocity.magnitude);
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
            else if (newState is AirborneState)
                CurrentStateType = MovementState.Airborne;
        }

        // 地面移动逻辑
        public void GroundMovement()
        {
            MoveLoop();
        }

        // 空中移动逻辑
        public void AirborneMovement()
        {
            AirborneMoveLoop();
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

            // 更新Trigger松开时间
            if (lastLeftTriggerPressed && !leftTriggerPressed) lastLeftTriggerReleaseTime = Time.time;
            if (lastRightTriggerPressed && !rightTriggerPressed) lastRightTriggerReleaseTime = Time.time;

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

            // 2) 更新grip历史状态（用于下一帧检测）
            bool leftJustReleased = lastLeftGripPressed && !leftGripPressed;
            bool rightJustReleased = lastRightGripPressed && !rightGripPressed;

            // 3) 检查是否处于3D移动模式（扳机键和侧卧键同时按下）
            bool leftIs3DMode = leftGripPressed && leftTriggerPressed;
            bool rightIs3DMode = rightGripPressed && rightTriggerPressed;
            bool isIn3DMode = leftIs3DMode || rightIs3DMode;

            // 检查上一帧是否处于3D移动模式
            bool leftWas3DMode = lastLeftGripPressed && lastLeftTriggerPressed;
            bool rightWas3DMode = lastRightGripPressed && lastRightTriggerPressed;

            // 检查是否刚刚退出3D模式（容差判断）
            bool leftRecently3D = leftWas3DMode || (Time.time - lastLeftTriggerReleaseTime < 0.25f);
            bool rightRecently3D = rightWas3DMode || (Time.time - lastRightTriggerReleaseTime < 0.25f);

            // 在把球体位置写回target之前，检测"松手瞬间"
            // 但在3D移动模式下，松手时不累加速度到residualVelocity
            if (leftJustReleased && !rightGripPressed && !leftRecently3D)
            {
                residualVelocity += leftDirection;
                Debug.Log("左手松开（捕获前一帧位置），累加leftDirection到residualVelocity: " + leftDirection);
            }

            if (rightJustReleased && !leftGripPressed && !rightRecently3D)
            {
                residualVelocity += rightDirection;
                Debug.Log("右手松开（捕获前一帧位置），累加rightDirection到residualVelocity: " + rightDirection);
            }

            // 在3D模式下松手时，保持当前速度不变，不进行累加
            if ((leftJustReleased && leftRecently3D) || (rightJustReleased && rightRecently3D))
            {
                Debug.Log("3D模式下松手（含容差），保持当前速度不变，不累加到residualVelocity");
            }

            // 4) 更新grip和trigger历史状态（用于下一帧检测）
            lastLeftGripPressed = leftGripPressed;
            lastRightGripPressed = rightGripPressed;
            lastLeftTriggerPressed = leftTriggerPressed;
            lastRightTriggerPressed = rightTriggerPressed;

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

            // 5) 在地面状态下，每帧都让residualVelocity以speedDecay衰减（使用MoveTowards避免反向）
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
            // 在3D移动模式下，松手瞬间不应用当前方向，避免速度激增
            Vector3 activeDirection = Vector3.zero;
            if (leftGripPressed && !(leftJustReleased && leftWas3DMode))
            {
                activeDirection += leftDirection;
            }
            if (rightGripPressed && !(rightJustReleased && rightWas3DMode))
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
                speed = new Vector3(speed.x, Mathf.Clamp(speed.y, -maxJumpForceY, maxJumpForceY), speed.z);
                thisRb.linearVelocity = speed;
                DebugGraph.MultiLog("Related Variables", DebugGraph.DefaultRed, speed.x, "speed.x");
                DebugGraph.MultiLog("Related Variables", DebugGraph.DefaultGreen, speed.y, "speed.y");
                DebugGraph.MultiLog("Related Variables", DebugGraph.DefaultBlue, speed.z, "speed.z");
                DebugGraph.Log("Speed Magnitude", speed.magnitude);

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

        // 空中移动循环 - 纯物理模式
        private void AirborneMoveLoop()
        {
            // 读取当前grip状态（布尔值）
            bool leftGripPressed = InputActionsManager.Actions.XRILeftInteraction.Select.IsPressed();
            bool rightGripPressed = InputActionsManager.Actions.XRIRightInteraction.Select.IsPressed();

            // 读取当前trigger状态（布尔值）
            bool leftTriggerPressed = InputActionsManager.Actions.XRILeftInteraction.Activate.IsPressed();
            bool rightTriggerPressed = InputActionsManager.Actions.XRIRightInteraction.Activate.IsPressed();

            // 更新Trigger松开时间
            if (lastLeftTriggerPressed && !leftTriggerPressed) lastLeftTriggerReleaseTime = Time.time;
            if (lastRightTriggerPressed && !rightTriggerPressed) lastRightTriggerReleaseTime = Time.time;

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

            // 2) 在把球体位置写回target之前，检测"松手瞬间"，把当时捕获到的方向转换为力

            // 检查上一帧是否处于3D移动模式
            bool leftWas3DMode = lastLeftGripPressed && lastLeftTriggerPressed;
            bool rightWas3DMode = lastRightGripPressed && lastRightTriggerPressed;

            // 检查是否刚刚退出3D模式（容差判断）
            bool leftRecently3D = leftWas3DMode || (Time.time - lastLeftTriggerReleaseTime < 0.25f);
            bool rightRecently3D = rightWas3DMode || (Time.time - lastRightTriggerReleaseTime < 0.25f);

            if (lastLeftGripPressed && !leftGripPressed)
            {
                // 如果不是3D模式，才施加脉冲力
                if (!leftRecently3D)
                {
                    Vector3 force = leftDirection * finalVelocityMultiplier;
                    thisRb.AddForce(force, ForceMode.Impulse);
                    Debug.Log("左手松开（空中），施加脉冲力: " + force);
                }
                else
                {
                    Debug.Log("左手松开（空中 3D模式/刚刚退出），不施加脉冲力，保持当前速度");
                }
            }

            if (lastRightGripPressed && !rightGripPressed)
            {
                // 如果不是3D模式，才施加脉冲力
                if (!rightRecently3D)
                {
                    Vector3 force = rightDirection * finalVelocityMultiplier;
                    thisRb.AddForce(force, ForceMode.Impulse);
                    Debug.Log("右手松开（空中），施加脉冲力: " + force);
                }
                else
                {
                    Debug.Log("右手松开（空中 3D模式/刚刚退出），不施加脉冲力，保持当前速度");
                }
            }

            // 3) 更新grip和trigger历史状态（用于下一帧检测）
            lastLeftGripPressed = leftGripPressed;
            lastRightGripPressed = rightGripPressed;
            lastLeftTriggerPressed = leftTriggerPressed;
            lastRightTriggerPressed = rightTriggerPressed;

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

            // 5) 在空中状态下，不对速度进行任何直接控制，让物理系统完全接管
            // 不设置 linearVelocity，不累加速度，不进行衰减

            // 6) 可选：在按住grip时持续施加较小的力进行微调（如果需要的话）
            Vector3 continuousForce = Vector3.zero;
            bool is3DMovementMode = (leftGripPressed && leftTriggerPressed) || (rightGripPressed && rightTriggerPressed);

            if (leftGripPressed)
            {
                continuousForce += leftDirection * 0.1f; // 较小的持续力
            }
            if (rightGripPressed)
            {
                continuousForce += rightDirection * 0.1f; // 较小的持续力
            }

            if (continuousForce != Vector3.zero)
            {
                if (is3DMovementMode)
                {
                    // 3D模式下施加完整的力
                    thisRb.AddForce(continuousForce * finalVelocityMultiplier, ForceMode.Force);
                }
                else
                {
                    // 普通模式下只施加水平力
                    Vector3 horizontalForce = new Vector3(continuousForce.x, 0, continuousForce.z) * finalVelocityMultiplier;
                    thisRb.AddForce(horizontalForce, ForceMode.Force);
                }
            }

            // 可视化当前速度（物理系统控制的真实速度）
            if (linePosition != null)
            {
                Color velocityColor = is3DMovementMode ? Color.cyan : Color.blue; // 空中3D模式用青色，普通模式用蓝色
                Debug.DrawLine(linePosition.position, linePosition.position + thisRb.linearVelocity * 11f, velocityColor, 0.1f);
            }

            // 调试信息
            if (Time.frameCount % 60 == 0) // 每60帧打印一次，避免日志过多
            {
                Debug.Log("空中状态 - 当前物理速度: " + thisRb.linearVelocity + " | 3D模式: " + is3DMovementMode);
            }
        }

        // 用于检测grip键状态变化

        private bool lastLeftGripPressed;
        private bool lastRightGripPressed;
        private bool lastLeftTriggerPressed;
        private bool lastRightTriggerPressed;

        // 记录Trigger松开的时间，用于处理"同时松开"的容差
        private float lastLeftTriggerReleaseTime = -100f;
        private float lastRightTriggerReleaseTime = -100f;

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
