using System;
using UnityEngine;

namespace YouYouTest.VRMove2
{
    #region 状态定义

    // 移动状态枚举
    public enum MovementState
    {
        Grounded,
        Airborne,
        WallSliding,
        Dashing
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

    #endregion

    #region 具体状态实现

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

            // 检查并恢复bodyPosition的Y轴scale到1
            controller.RestoreBodyScale();

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

            // 当进入跳跃状态时，将bodyPosition的Y轴scale设置为0.4
            if (controller.bodyPosition != null)
            {
                Vector3 currentScale = controller.bodyPosition.localScale;
                controller.bodyPosition.localScale = new Vector3(currentScale.x, 0.4f, currentScale.z);
                Debug.Log("跳跃状态：将bodyPosition的Y轴scale设置为0.4");
            }
        }

        public override void Update()
        {
            // 在空中状态下的移动逻辑
            controller.AirborneMovement();

            // 从headPosition向下发射射线检测Building层
            controller.CheckBuildingBelow();

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

    // 贴墙滑行状态
    public class WallSlidingState : MovementStateBase
    {
        public WallSlidingState(NewVRMove2 controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("进入贴墙滑行状态");

            // 停止body跟随玩家相机
            if (controller.vrBody != null)
            {
                controller.vrBody.StopFollow();
            }

            // 禁用刚体重力
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = false;
            }

            // 触发进入贴墙滑行事件
            controller.InvokeOnEnterWallSliding(controller.wallNormal);
        }

        public override void Update()
        {
            // 更新贴墙计时器
            controller.wallSlideTimer += Time.deltaTime;

            // 持续检测是否还贴在墙上
            controller.CheckWallAttachment();

            // 贴墙滑行时的移动逻辑
            controller.WallSlidingMovement();
        }

        public override void Exit()
        {
            Debug.Log("离开贴墙滑行状态");

            // 恢复body跟随玩家相机
            if (controller.vrBody != null)
            {
                controller.vrBody.StartFollow();
            }

            // 重新启用刚体重力
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = true;
            }

            // 触发退出贴墙滑行事件
            controller.InvokeOnExitWallSliding(controller.wallNormal);
        }
    }

    #endregion

    // 冲刺状态
    public class DashingState : MovementStateBase
    {
        public DashingState(NewVRMove2 controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("进入冲刺状态");
            
            // 禁用刚体重力
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = false;
            }
        }

        public override void Update()
        {
            // 更新冲刺计时器
            controller.dashTimer -= Time.deltaTime;
            
            // 处理冲刺移动
            controller.DashMovement();
            
            // 检查冲刺是否结束
            if (controller.dashTimer <= 0f)
            {
                controller.EndDash();
            }
        }

        public override void Exit()
        {
            Debug.Log("离开冲刺状态");
            
            // 重新启用刚体重力
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = true;
            }
        }
    }

    public class NewVRMove2 : MonoBehaviour
    {
        public Transform bodyPosition;
        public Transform headPosition;
        public VRBody2 vrBody; // VRBody2引用，用于控制body跟随

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
        public float multiJumpForceY;
        public float addGravityForceY;

        [Header("速度限制")]
        [Tooltip("平移移动的最大速度")]
        public float maxTranslationSpeed = 10f;

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

        [Tooltip("是否使用动态地面检测距离（基于bodyPosition的Y轴scale）")]
        public bool useDynamicGroundCheckDistance = false;

        [Tooltip("指定要检测的地面层")]
        public LayerMask groundLayerMask = -1; // -1表示检测所有层

        [Tooltip("指定要检测的建筑层")]
        public LayerMask buildingLayerMask;

        [Header("贴墙滑行设置")]
        [Tooltip("贴墙滑行速度倍率")]
        public float wallSlideSpeedMultiplier = 1.2f;
        [Tooltip("墙体检测射线距离")]
        public float wallCheckRayDistance = 6f;
        [Tooltip("是否启用贴墙滑行日志")]
        public bool needWallSlideLog = false;

        [Header("冲刺设置")]
        [Tooltip("冲刺速度")]
        public float dashSpeed = 15f;
        [Tooltip("冲刺持续时间")]
        public float dashDuration = 0.3f;
        [Tooltip("冲刺冷却时间")]
        public float dashCooldown = 1f;

        [Header("手部移动冲刺设置")]
        [Tooltip("手部移动速度阈值")]
        public float handMoveSpeedThreshold = 1f;

        // 贴墙滑行相关私有变量
        [HideInInspector] public Vector3 wallNormal; // 存储墙面法线
        [HideInInspector] public float wallSlideTimer = 0f; // 贴墙计时器
        [HideInInspector] public Vector3 wallSlideDirection; // 存储贴墙滑行方向（投影向量）

        // 冲刺相关私有变量
        [HideInInspector] public float dashTimer = 0f; // 冲刺计时器
        [HideInInspector] public float dashCooldownTimer = 0f; // 冲刺冷却计时器
        [HideInInspector] public Vector3 dashDirection; // 冲刺方向

        // 手部移动检测相关私有变量
        private Vector3 previousRightHandLocalPosition;
        private Vector3 previousLeftHandLocalPosition;

        // 贴墙滑行事件
        public event Action<Vector3> OnEnterWallSliding;
        public event Action<Vector3> OnExitWallSliding;

        // 事件触发辅助方法（用于在状态类中调用）
        public void InvokeOnEnterWallSliding(Vector3 normal)
        {
            OnEnterWallSliding?.Invoke(normal);
        }

        public void InvokeOnExitWallSliding(Vector3 normal)
        {
            OnExitWallSliding?.Invoke(normal);
        }

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


        #region 私有变量

        // 用于检测grip键状态变化
        private bool lastLeftGripPressed;
        private bool lastRightGripPressed;
        private bool lastLeftTriggerPressed;
        private bool lastRightTriggerPressed;

        // 记录Trigger松开的时间，用于处理"同时松开"的容差
        private float lastLeftTriggerReleaseTime = -100f;
        private float lastRightTriggerReleaseTime = -100f;

        #endregion

        #region Unity生命周期方法

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // 初始化grip状态
            lastLeftGripPressed = false;
            lastRightGripPressed = false;

            // 初始化手部位置
            if (rightSphereTarget != null)
            {
                previousRightHandLocalPosition = rightSphereTarget.localPosition;
            }
            if (leftSphereTarget != null)
            {
                previousLeftHandLocalPosition = leftSphereTarget.localPosition;
            }

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
            
            // 处理冲刺逻辑
            HandleDash();
            
            // 检测手部移动并触发冲刺
            HandleHandMoveDash();
        }

        // LateUpdate is called after all Update functions have been called
        void LateUpdate()
        {
            GroundCheck();
        }

        /// <summary>
        /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
        /// </summary>
        void FixedUpdate()
        {
            // 应用额外重力（冲刺状态和贴墙滑行状态下不应用重力）
            if (thisRb != null && CurrentStateType != MovementState.Dashing && CurrentStateType != MovementState.WallSliding)
            {
                thisRb.AddForce(Vector3.down * addGravityForceY, ForceMode.Acceleration);
            }
            DebugGraph.Log("final speeeeed", thisRb.linearVelocity.magnitude);
        }

        #endregion

        #region 状态机管理

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
            else if (newState is WallSlidingState)
                CurrentStateType = MovementState.WallSliding;
            else if (newState is DashingState)
                CurrentStateType = MovementState.Dashing;
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

        #endregion

        #region 地面检测

        private void GroundCheck()
        {
            // 从当前物体位置开始检测
            Vector3 spherePosition = bodyPosition.position;
            Vector3 direction = Vector3.down; // 向下检测

            // 创建球体投射的参数
            RaycastHit hitInfo;

            // 根据设置决定使用固定距离还是动态距离
            float actualGroundCheckDistance;
            if (useDynamicGroundCheckDistance)
            {
                // 动态计算检测距离：使用bodyPosition的Y轴scale的一半
                actualGroundCheckDistance = bodyPosition.localScale.y * 0.5f;
            }
            else
            {
                // 使用固定的检测距离
                actualGroundCheckDistance = groundCheckDistance;
            }
            actualGroundCheckDistance +=2f; // 增加一个偏移，避免贴地时检测不到

            // 执行球体投射，检测指定层的地面
            bool hitGround = Physics.SphereCast(
                spherePosition+Vector3.up*2f, // 抬高起点，避免穿透地面
                groundCheckRadius,
                direction,
                out hitInfo,
                actualGroundCheckDistance,
                groundLayerMask
            );

            // 更新地面状态
            isOnGround = hitGround;

            // 可选：在Scene视图中可视化检测范围（仅在编辑器中可见）
#if UNITY_EDITOR
            // 使用CapsuleWireframeDrawer绘制球体投射的可视化
            // 将球体投射转换为胶囊体表示（避免 point1==point2 导致方向向量为零的问题）
            Vector3 sphereCenter = spherePosition + Vector3.up * 2f; // 与球体投射起始点保持一致
            Vector3 castDirection = direction * actualGroundCheckDistance;
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

        #endregion

        #region 地面移动逻辑

        private void MoveLoop()
        {
            // 检查是否在空中，如果在空中则取消拖拽球的影响
            if (!isOnGround)
            {
                // 如果在空中，立即同步球体位置到目标位置，取消拖拽效果
                if (leftSphere != null && leftSphereTarget != null)
                {
                    leftSphere.position = leftSphereTarget.position;
                    leftSphere.rotation = leftSphereTarget.rotation;
                }
                if (rightSphere != null && rightSphereTarget != null)
                {
                    rightSphere.position = rightSphereTarget.position;
                    rightSphere.rotation = rightSphereTarget.rotation;
                }
                
                // 清除方向向量和残差速度
                leftDirection = Vector3.zero;
                rightDirection = Vector3.zero;
                residualVelocity = Vector3.zero;
                
                Debug.Log("检测到空中状态，取消拖拽球的影响");
                return; // 直接返回，不执行后续的拖拽逻辑
            }
            
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

                // 限制累加后的residualVelocity的水平分量
                Vector3 horizontalResidual = new Vector3(residualVelocity.x, 0, residualVelocity.z);
                if (horizontalResidual.magnitude > maxTranslationSpeed)
                {
                    horizontalResidual = horizontalResidual.normalized * maxTranslationSpeed;
                    residualVelocity = new Vector3(horizontalResidual.x, residualVelocity.y, horizontalResidual.z);
                }

                Debug.Log("左手松开（捕获前一帧位置），累加leftDirection到residualVelocity: " + leftDirection);
            }

            if (rightJustReleased && !leftGripPressed && !rightRecently3D)
            {
                residualVelocity += rightDirection;

                // 限制累加后的residualVelocity的水平分量
                Vector3 horizontalResidual = new Vector3(residualVelocity.x, 0, residualVelocity.z);
                if (horizontalResidual.magnitude > maxTranslationSpeed)
                {
                    horizontalResidual = horizontalResidual.normalized * maxTranslationSpeed;
                    residualVelocity = new Vector3(horizontalResidual.x, residualVelocity.y, horizontalResidual.z);
                }

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

            // 限制residualVelocity的水平分量，确保回落速度也不超过最大值
            Vector3 horizontalResidualVelocity = new Vector3(residualVelocity.x, 0, residualVelocity.z);
            if (horizontalResidualVelocity.magnitude > maxTranslationSpeed)
            {
                horizontalResidualVelocity = horizontalResidualVelocity.normalized * maxTranslationSpeed;
                residualVelocity = new Vector3(horizontalResidualVelocity.x, residualVelocity.y, horizontalResidualVelocity.z);
            }

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

            // 限制finalVelocity的水平分量，确保累加的速度不超过最大值
            Vector3 horizontalFinalVelocity = new Vector3(finalVelocity.x, 0, finalVelocity.z);
            if (horizontalFinalVelocity.magnitude > maxTranslationSpeed)
            {
                horizontalFinalVelocity = horizontalFinalVelocity.normalized * maxTranslationSpeed;
                finalVelocity = new Vector3(horizontalFinalVelocity.x, finalVelocity.y, horizontalFinalVelocity.z);
            }

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
                speed = new Vector3(speed.x, speed.y * multiJumpForceY, speed.z);
                // 在3D移动模式下，完全应用计算出的速度（包括Y轴）
                speed = new Vector3(speed.x, Mathf.Clamp(speed.y, -maxJumpForceY, maxJumpForceY), speed.z);
                thisRb.linearVelocity = speed;
                // DebugGraph.MultiLog("Related Variables", DebugGraph.DefaultRed, speed.x, "speed.x");
                // DebugGraph.MultiLog("Related Variables", DebugGraph.DefaultGreen, speed.y, "speed.y");
                // DebugGraph.MultiLog("Related Variables", DebugGraph.DefaultBlue, speed.z, "speed.z");
                DebugGraph.Log("Y Speed Magnitude", speed.magnitude);

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

        #endregion

        #region 空中移动逻辑

        // 空中移动循环 - 纯物理模式
        private void AirborneMoveLoop()
        {
            // 在空中状态下，立即同步球体位置到目标位置，取消拖拽效果
            if (leftSphere != null && leftSphereTarget != null)
            {
                leftSphere.position = leftSphereTarget.position;
                leftSphere.rotation = leftSphereTarget.rotation;
            }
            if (rightSphere != null && rightSphereTarget != null)
            {
                rightSphere.position = rightSphereTarget.position;
                rightSphere.rotation = rightSphereTarget.rotation;
            }
            
            // 清除方向向量，确保不会继续应用拖拽效果
            leftDirection = Vector3.zero;
            rightDirection = Vector3.zero;
            
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

        #endregion

        #region 转向控制

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

        #endregion

        #region 建筑物检测

        // 检测下方建筑物的射线检测
        public void CheckBuildingBelow()
        {
            if (headPosition == null)
            {
                Debug.LogWarning("headPosition未设置！");
                return;
            }

            // 从headPosition位置向下发射射线
            Vector3 rayOrigin = headPosition.position;
            Vector3 rayDirection = Vector3.down;
            float rayDistance = 2f;

            RaycastHit hitInfo;
            bool hitBuilding = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, rayDistance, buildingLayerMask);

            // 根据检测结果选择颜色
            Color lineColor = hitBuilding ? Color.green : Color.red;

            // 绘制调试线
            Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * rayDistance, lineColor);

            // 如果检测到建筑物，获取距离并应用到bodyPosition的Y轴scale
            if (hitBuilding)
            {
                float distance = hitInfo.distance;

                if (bodyPosition != null)
                {
                    Vector3 currentScale = bodyPosition.localScale;
                    bodyPosition.localScale = new Vector3(currentScale.x, distance / 2, currentScale.z);
                    Debug.Log($"检测到建筑物: {hitInfo.collider.gameObject.name}, 距离: {distance}, 设置bodyPosition Y轴scale为: {distance}");
                }
            }
        }

        // 恢复bodyPosition的Y轴scale到1
        public void RestoreBodyScale()
        {
            if (bodyPosition == null)
            {
                Debug.LogWarning("bodyPosition未设置！");
                return;
            }

            Vector3 currentScale = bodyPosition.localScale;

            // 如果Y轴scale不是1，就渐渐变回1
            if (Mathf.Abs(currentScale.y - 1f) > 0.001f)
            {
                float newY = Mathf.Lerp(currentScale.y, 1f, Time.deltaTime * 2f); // 使用Lerp平滑过渡
                bodyPosition.localScale = new Vector3(currentScale.x, newY, currentScale.z);

                // 可选：输出调试信息
                if (Time.frameCount % 30 == 0) // 每30帧打印一次，避免日志过多
                {
                    Debug.Log($"恢复bodyPosition Y轴scale: {currentScale.y} -> {newY}");
                }
            }
        }

        #endregion

        #region 贴墙滑行相关方法

        /// <summary>
        /// 贴墙滑行时的移动逻辑
        /// </summary>
        public void WallSlidingMovement()
        {
            if (thisRb == null)
                return;

            // 贴墙滑行状态：按照投影向量方向自动滑行，Y轴速度为0
            float actualMoveSpeed = finalVelocityMultiplier * wallSlideSpeedMultiplier;

            Vector3 wallSlideVelocity = new Vector3(
                wallSlideDirection.x * actualMoveSpeed,
                0,
                wallSlideDirection.z * actualMoveSpeed
            );
            thisRb.linearVelocity = wallSlideVelocity;
        }

        /// <summary>
        /// 检测是否还贴在墙上
        /// </summary>
        public void CheckWallAttachment()
        {
            if (bodyPosition == null)
                return;

            // 从body位置向墙面法线的反方向发射射线
            Vector3 rayDirection = -wallNormal;
            Ray ray = new Ray(bodyPosition.position, rayDirection);

            // 绘制黄色射线
            Color rayColor = Color.yellow;

            // 使用RaycastAll检测所有碰撞，然后过滤出Wall tag的物体
            RaycastHit[] hits = Physics.RaycastAll(ray, wallCheckRayDistance);
            bool stillAttached = false;

            foreach (RaycastHit hitInfo in hits)
            {
                if (hitInfo.collider.CompareTag("Wall"))
                {
                    stillAttached = true;
                    break;
                }
            }

            if (!stillAttached)
            {
                if (needWallSlideLog)
                    Debug.Log("贴墙滑行时未检测到墙体，退出贴墙状态");

                // 如果射线没有检测到墙体，退出贴墙状态
                ExitWallSliding();
            }

            // 在Scene视图中绘制射线
            Debug.DrawRay(bodyPosition.position, rayDirection * wallCheckRayDistance, rayColor, 0.1f);
        }

        /// <summary>
        /// 进入贴墙滑行状态
        /// </summary>
        public void EnterWallSliding(Vector3 normal)
        {
            wallNormal = normal;
            wallSlideTimer = 0f;

            if (needWallSlideLog)
                Debug.Log("进入贴墙滑行状态");

            ChangeState(new WallSlidingState(this));
        }

        /// <summary>
        /// 退出贴墙滑行状态
        /// </summary>
        public void ExitWallSliding()
        {
            if (CurrentStateType == MovementState.WallSliding)
            {
                if (needWallSlideLog)
                    Debug.Log("退出贴墙滑行状态");

                // 清除residualVelocity，让速度就是当前离开墙面时的实际速度
                residualVelocity = Vector3.zero;

                // 根据当前是否在地面来决定下一个状态
                if (isOnGround)
                    ChangeState(new GroundedState(this));
                else
                    ChangeState(new AirborneState(this));
            }
        }

        #endregion

        #region 碰撞检测

        void OnCollisionEnter(Collision collision)
        {
            // 检测碰撞物体是否为Wall
            if (collision.gameObject.CompareTag("Wall"))
            {
                if (needWallSlideLog)
                    Debug.Log("检测到墙体碰撞");

                // 获取第一个接触点的法线
                if (collision.contactCount > 0)
                {
                    ContactPoint contact = collision.GetContact(0);
                    Vector3 normal = contact.normal;

                    if (needWallSlideLog)
                        Debug.Log($"碰撞点法线: {normal}");

                    // 从碰撞点绘制法线（红色）
                    Debug.DrawRay(contact.point, normal * 2f, Color.red, 2f);

                    // 计算速度在法线平面上的投影向量
                    if (thisRb != null)
                    {
                        Vector3 velocity = thisRb.linearVelocity;
                        Vector3 projection = Vector3.ProjectOnPlane(velocity, normal);

                        // Y轴归零，变成水平向量
                        Vector3 horizontalProjection = new Vector3(projection.x, 0, projection.z);

                        // 长度重置为1
                        if (horizontalProjection != Vector3.zero)
                        {
                            horizontalProjection = horizontalProjection.normalized;
                        }

                        if (needWallSlideLog)
                            Debug.Log($"速度在法线平面上的投影向量 (Y轴归零, 长度1): {horizontalProjection}");

                        // 从碰撞点绘制投影向量（绿色）
                        Debug.DrawRay(contact.point, horizontalProjection, Color.green, 2f);

                        // 检查投影向量是否为垂直方向（没有水平分量）且不在地面上
                        if (horizontalProjection != Vector3.zero && !isOnGround)
                        {
                            // 保存投影向量用于贴墙滑行
                            wallSlideDirection = horizontalProjection;

                            // 进入贴墙滑行状态
                            EnterWallSliding(normal);
                        }
                        else
                        {
                            if (needWallSlideLog)
                                Debug.Log("投影向量为垂直方向或在地面上，不进入滑行状态");
                        }
                    }
                }
            }
        }

        void OnCollisionExit(Collision collision)
        {
            // 离开墙体时退出贴墙滑行状态
            if (collision.gameObject.CompareTag("Wall") && CurrentStateType == MovementState.WallSliding)
            {
                if (needWallSlideLog)
                    Debug.Log("离开墙体，退出贴墙滑行状态");

                ExitWallSliding();
            }
        }

        #endregion

        #region 冲刺相关方法

        // 处理冲刺逻辑
        private void HandleDash()
        {
            // 更新冷却计时器
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
            }

            // 检测冲刺输入（使用左手柄的PrimaryButton，通常是A键）
            if (InputActionsManager.Actions.XRILeftInteraction.Activate.WasPressedThisFrame() && CanDash())
            {
                StartDash();
            }
        }

        // 检查是否可以冲刺
        private bool CanDash()
        {
            return dashCooldownTimer <= 0f &&
                   CurrentStateType != MovementState.Dashing &&
                   CurrentStateType != MovementState.WallSliding;
        }

        // 开始冲刺
        private void StartDash()
        {
            StartDash(Vector3.zero);
        }

        // 开始冲刺（带自定义方向）
        private void StartDash(Vector3 customDirection)
        {
            ChangeState(new DashingState(this));
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // 如果提供了自定义方向，使用自定义方向
            if (customDirection != Vector3.zero)
            {
                dashDirection = customDirection.normalized;
            }
            else
            {
                // 计算冲刺方向
                Vector3 moveDirection = Vector3.zero;
                
                // 获取当前移动方向（基于手柄输入）
                Vector2 leftStickInput = InputActionsManager.Actions.XRILeftLocomotion.Move.ReadValue<Vector2>();
                if (leftStickInput.magnitude > 0.1f)
                {
                    // 将摇杆输入转换为世界空间方向
                    Vector3 forward = playerHead.forward;
                    Vector3 right = playerHead.right;
                    
                    // 只使用水平方向
                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();
                    
                    moveDirection = forward * leftStickInput.y + right * leftStickInput.x;
                }
                
                // 如果当前没有移动方向，则使用玩家朝向作为冲刺方向
                if (moveDirection != Vector3.zero)
                {
                    dashDirection = moveDirection.normalized;
                }
                else
                {
                    // 使用playerHead的前方作为默认冲刺方向（只使用水平方向）
                    Vector3 forward = playerHead.forward;
                    forward.y = 0;
                    dashDirection = forward.normalized;
                }
            }
            
            Debug.Log("开始冲刺，方向: " + dashDirection);
        }

        // 结束冲刺
        public void EndDash()
        {
            // 冲刺结束时恢复速度
            if (thisRb != null)
            {
                // 保持当前的水平速度，但降低到正常移动速度
                Vector3 currentVelocity = thisRb.linearVelocity;
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z).normalized * finalVelocityMultiplier;
                thisRb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
            }

            // 根据当前是否在地面来决定下一个状态
            if (isOnGround)
                ChangeState(new GroundedState(this));
            else
                ChangeState(new AirborneState(this));
                
            Debug.Log("结束冲刺");
        }

        // 处理冲刺移动
        public void DashMovement()
        {
            if (thisRb != null)
            {
                // 应用冲刺速度，只控制x和z轴，保持y轴速度不变
                Vector3 dashVelocity = new Vector3(dashDirection.x * dashSpeed, thisRb.linearVelocity.y, dashDirection.z * dashSpeed);
                thisRb.linearVelocity = dashVelocity;
            }
        }

        /// <summary>
        /// 外部调用触发冲刺
        /// </summary>
        public void TriggerDash()
        {
            if (CanDash())
            {
                StartDash();
            }
        }

        /// <summary>
        /// 外部调用触发冲刺，可指定方向
        /// </summary>
        /// <param name="direction">冲刺方向</param>
        public void TriggerDash(Vector3 direction)
        {
            if (CanDash())
            {
                StartDash(direction);
            }
        }

        #endregion

        #region 手部移动冲刺方法
        /// <summary>
        /// 检测手部移动并触发冲刺
        /// </summary>
        void HandleHandMoveDash()
        {
            // 检测右手柄移动
            if (rightSphereTarget != null)
            {
                // 检测右手柄A键是否被按下
                bool rightButtonPressed = InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.IsPressed();

                // 如果A键没有被按下，则不进行冲刺检测
                if (!rightButtonPressed)
                {
                    // 更新上一帧的位置（重置位置跟踪）
                    previousRightHandLocalPosition = rightSphereTarget.localPosition;
                }
                else
                {
                    // 计算当前帧的本地位置
                    Vector3 currentLocalPosition = rightSphereTarget.localPosition;

                    // 计算移动速度（每帧移动的距离）
                    float moveDistance = Vector3.Distance(currentLocalPosition, previousRightHandLocalPosition);
                    float speed = moveDistance / Time.deltaTime;

                    // 检查速度是否超过阈值
                    if (speed > handMoveSpeedThreshold)
                    {
                        Debug.Log("右手移动速度超过阈值且A键被按下，触发冲刺");
                        TriggerDash();
                    }

                    // 更新上一帧的位置
                    previousRightHandLocalPosition = currentLocalPosition;
                }
            }

            // 检测左手柄移动
            if (leftSphereTarget != null)
            {
                // 检测左手柄A键是否被按下
                bool leftButtonPressed = InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.IsPressed();

                // 如果A键没有被按下，则不进行冲刺检测
                if (!leftButtonPressed)
                {
                    // 更新上一帧的位置（重置位置跟踪）
                    previousLeftHandLocalPosition = leftSphereTarget.localPosition;
                }
                else
                {
                    // 计算当前帧的本地位置
                    Vector3 currentLocalPosition = leftSphereTarget.localPosition;

                    // 计算移动速度（每帧移动的距离）
                    float moveDistance = Vector3.Distance(currentLocalPosition, previousLeftHandLocalPosition);
                    float speed = moveDistance / Time.deltaTime;

                    // 检查速度是否超过阈值
                    if (speed > handMoveSpeedThreshold)
                    {
                        Debug.Log("左手移动速度超过阈值且A键被按下，触发冲刺");
                        TriggerDash();
                    }

                    // 更新上一帧的位置
                    previousLeftHandLocalPosition = currentLocalPosition;
                }
            }
        }
        #endregion
    }
}
