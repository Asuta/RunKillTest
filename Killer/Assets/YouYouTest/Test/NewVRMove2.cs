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
        Dashing,
        HookDashing
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
                
                // ★★★ 关键修改：立即重置速度为贴墙滑行速度，防止原来的高速度导致被挤出墙面 ★★★
                float actualMoveSpeed = controller.finalVelocityMultiplier * controller.wallSlideSpeedMultiplier;
                Vector3 wallSlideVelocity = new Vector3(
                    controller.wallSlideDirection.x * actualMoveSpeed,
                    0, // Y轴速度强制为0
                    controller.wallSlideDirection.z * actualMoveSpeed
                );
                controller.thisRb.linearVelocity = wallSlideVelocity;
                Debug.Log($"<color=green>[WallSlidingState.Enter] 重置速度为贴墙滑行速度: {wallSlideVelocity}</color>");
            }
            
            // 启动贴墙保护计时器
            controller.StartWallSlideProtection();

            // 触发进入贴墙滑行事件
            controller.InvokeOnEnterWallSliding(controller.wallNormal);
        }

        public override void Update()
        {
            Debug.Log($"<color=lime>[WallSlidingState.Update] 帧更新 - 计时器: {controller.wallSlideTimer:F2}秒</color>");
            
            // 更新贴墙计时器
            controller.wallSlideTimer += Time.deltaTime;
            
            // 更新贴墙保护计时器
            controller.UpdateWallSlideProtection();

            // 持续检测是否还贴在墙上
            Debug.Log("<color=lime>[WallSlidingState.Update] 准备执行CheckWallAttachment</color>");
            controller.CheckWallAttachment();
            
            // 检查状态是否仍然是贴墙状态（CheckWallAttachment可能已经切换了状态）
            if (controller.CurrentStateType != MovementState.WallSliding)
            {
                Debug.LogWarning("<color=lime>[WallSlidingState.Update] CheckWallAttachment后状态变了，中止Update</color>");
                return;
            }

            // 检测贴墙状态下的拖拽跳跃
            Debug.Log("<color=lime>[WallSlidingState.Update] 执行WallSlidingJumpCheck</color>");
            controller.WallSlidingJumpCheck();
            
            // 再次检查状态
            if (controller.CurrentStateType != MovementState.WallSliding)
            {
                Debug.LogWarning("<color=lime>[WallSlidingState.Update] WallSlidingJumpCheck后状态变了，中止Update</color>");
                return;
            }

            // 贴墙滑行时的移动逻辑
            Debug.Log("<color=lime>[WallSlidingState.Update] 执行WallSlidingMovement</color>");
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

    // Hook冲刺状态
    public class HookDashingState : MovementStateBase
    {
        public HookDashingState(NewVRMove2 controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("进入Hook冲刺状态");

            // 禁用刚体重力
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = false;
            }
        }

        public override void Update()
        {
            // 更新Hook冲刺计时器
            controller.hookDashTimer -= Time.deltaTime;

            // 处理Hook冲刺移动
            controller.HookDashMovement();

            // 检查Hook冲刺是否结束（计时器耗尽）
            if (controller.hookDashTimer <= 0f)
            {
                controller.EndHookDash();
            }
        }

        public override void Exit()
        {
            Debug.Log("离开Hook冲刺状态");

            // 重新启用刚体重力
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = true;
            }
        }
    }

    public class NewVRMove2 : MonoBehaviour, IPlayerHeadProvider, IDashProvider, IHookDashProvider, IMoveSpeedProvider, IWallSlidingProvider, IBounceCubeProvider
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
        [Tooltip("贴墙跳跃固定速度")]
        public float wallJumpSpeed = 15f;
        [Tooltip("贴墙跳跃手部速度阈值")]
        public float wallJumpHandSpeedThreshold = 2.0f;

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

        [Header("Hook冲刺设置")]
        [Tooltip("Hook冲刺速度")]
        public float hookDashSpeed = 15f;
        [Tooltip("Hook冲刺持续时间")]
        public float hookDashDuration = 0.3f;

        [Header("移动速度设置")]
        [Tooltip("额外移动速度")]
        public float AddMoveSpeed = 0f;
        [Tooltip("额外移动速度倍率")]
        public float AddMoveSpeedMultiplier = 1f;

        // 贴墙滑行相关私有变量
        [HideInInspector] public Vector3 wallNormal; // 存储墙面法线
        [HideInInspector] public float wallSlideTimer = 0f; // 贴墙计时器
        [HideInInspector] public Vector3 wallSlideDirection; // 存储贴墙滑行方向（投影向量）
        private bool wallJumpProtection = false; // 贴墙跳跃保护标志，防止跳跃速度被覆盖
        private float wallSlideProtectionTimer = 0f; // 贴墙保护计时器，防止刚进入就被挤出
        private const float WALL_SLIDE_PROTECTION_DURATION = 0.15f; // 贴墙保护时间（秒）

        // 冲刺相关私有变量
        [HideInInspector] public float dashTimer = 0f; // 冲刺计时器
        [HideInInspector] public float dashCooldownTimer = 0f; // 冲刺冷却计时器
        [HideInInspector] public Vector3 dashDirection; // 冲刺方向

        // 手部移动检测相关私有变量
        private Vector3 previousRightHandLocalPosition;
        private Vector3 previousLeftHandLocalPosition;

        // Hook冲刺相关私有变量
        [HideInInspector] public Transform hookTarget; // 目标hook的Transform
        [HideInInspector] public float hookDashTimer = 0f; // hook冲刺计时器
        [HideInInspector] public Vector3 hookDashDirection; // hook冲刺方向

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

            // 处理Hook冲刺逻辑
            HandleHookDash();

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
            // 应用额外重力（冲刺状态、贴墙滑行状态和Hook冲刺状态下不应用重力）
            if (thisRb != null && CurrentStateType != MovementState.Dashing && CurrentStateType != MovementState.WallSliding && CurrentStateType != MovementState.HookDashing)
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
            // 获取调用堆栈信息
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(1, true);
            string callerMethod = stackTrace.GetFrame(0)?.GetMethod()?.Name ?? "Unknown";
            
            // 记录旧状态
            MovementState oldState = CurrentStateType;
            
            // 确定新状态类型
            MovementState newStateType = MovementState.Grounded;
            if (newState is GroundedState)
                newStateType = MovementState.Grounded;
            else if (newState is AirborneState)
                newStateType = MovementState.Airborne;
            else if (newState is WallSlidingState)
                newStateType = MovementState.WallSliding;
            else if (newState is DashingState)
                newStateType = MovementState.Dashing;
            else if (newState is HookDashingState)
                newStateType = MovementState.HookDashing;
            
            Debug.LogWarning($"<color=yellow>[ChangeState] ★★★ 状态切换 ★★★</color>");
            Debug.LogWarning($"<color=yellow>[ChangeState] {oldState} → {newStateType}</color>");
            Debug.LogWarning($"<color=yellow>[ChangeState] 调用来源: {callerMethod}</color>");
            Debug.LogWarning($"<color=yellow>[ChangeState] 是否在地面: {isOnGround}</color>");
            Debug.LogWarning($"<color=yellow>[ChangeState] 当前速度: {thisRb?.linearVelocity}</color>");
            
            currentState?.Exit();
            currentState = newState;
            CurrentStateType = newStateType;
            currentState.Enter();
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
            actualGroundCheckDistance += 2f; // 增加一个偏移，避免贴地时检测不到

            // 执行球体投射，检测指定层的地面
            bool hitGround = Physics.SphereCast(
                spherePosition + Vector3.up * 2f, // 抬高起点，避免穿透地面
                groundCheckRadius,
                direction,
                out hitInfo,
                actualGroundCheckDistance,
                groundLayerMask
            );
            
            // 如果当前在贴墙状态，记录地面检测结果
            if (CurrentStateType == MovementState.WallSliding)
            {
                if (hitGround != isOnGround)
                {
                    Debug.LogWarning($"<color=yellow>[GroundCheck] 贴墙状态下地面状态变化: {isOnGround} → {hitGround}</color>");
                    if (hitGround)
                    {
                        Debug.LogWarning($"<color=yellow>[GroundCheck] 检测到地面: {hitInfo.collider.gameObject.name}, 层: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}</color>");
                    }
                }
            }

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
                DebugGraph.Log("跳跃", speed.magnitude);
                DebugGraph.Write("普通跳跃");
                // Debug.LogError("跳跃速度" + speed);

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
            // 检查贴墙跳跃保护标志，如果刚刚进行了贴墙跳跃，跳过本帧的处理以保护速度
            if (wallJumpProtection)
            {
                wallJumpProtection = false;
                Debug.Log("贴墙跳跃保护：跳过本帧空中移动逻辑");
                return;
            }

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

            // 2) 在空中状态下，不检测"松手瞬间"，不施加任何脉冲力
            // 完全取消在空中时的拖拽影响，包括松手时的速度累积
            if (lastLeftGripPressed && !leftGripPressed)
            {
                Debug.Log("左手松开（空中），不施加脉冲力，拖拽球影响已取消");
            }

            if (lastRightGripPressed && !rightGripPressed)
            {
                Debug.Log("右手松开（空中），不施加脉冲力，拖拽球影响已取消");
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
            // 完全取消在空中时的拖拽影响，包括持续施加的微调力

            // 可视化当前速度（物理系统控制的真实速度）
            if (linePosition != null)
            {
                // 在空中状态下，始终使用蓝色表示纯物理模式
                Color velocityColor = Color.blue;
                Debug.DrawLine(linePosition.position, linePosition.position + thisRb.linearVelocity * 11f, velocityColor, 0.1f);
            }

            // 调试信息
            if (Time.frameCount % 60 == 0) // 每60帧打印一次，避免日志过多
            {
                Debug.Log("空中状态 - 当前物理速度: " + thisRb.linearVelocity + " | 拖拽球影响已取消");
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

                // // 可选：输出调试信息
                // if (Time.frameCount % 30 == 0) // 每30帧打印一次，避免日志过多
                // {
                //     Debug.Log($"恢复bodyPosition Y轴scale: {currentScale.y} -> {newY}");
                // }
            }
        }

        #endregion

        #region 贴墙滑行相关方法

        /// <summary>
        /// 贴墙滑行时的跳跃检测（固定方向和速度）
        /// 跳跃方向为墙面法线方向和滑行方向之间的45度方向
        /// </summary>
        public void WallSlidingJumpCheck()
        {
            // 读取当前grip状态（布尔值）
            bool leftGripPressed = InputActionsManager.Actions.XRILeftInteraction.Select.IsPressed();
            bool rightGripPressed = InputActionsManager.Actions.XRIRightInteraction.Select.IsPressed();

            // 读取当前trigger状态（布尔值）
            bool leftTriggerPressed = InputActionsManager.Actions.XRILeftInteraction.Activate.IsPressed();
            bool rightTriggerPressed = InputActionsManager.Actions.XRIRightInteraction.Activate.IsPressed();

            // 检查是否处于3D移动模式（扳机键和侧卧键同时按下）- 用于贴墙跳跃
            bool leftIs3DMode = leftGripPressed && leftTriggerPressed;
            bool rightIs3DMode = rightGripPressed && rightTriggerPressed;
            bool isIn3DMode = leftIs3DMode || rightIs3DMode;

            // 如果处于3D模式，执行贴墙跳跃
            if (isIn3DMode)
            {
                // 检测手部移动速度是否达到阈值
                bool speedConditionMet = false;

                // 检查左手
                if (leftIs3DMode && leftSphereTarget != null)
                {
                    float moveDistance = Vector3.Distance(leftSphereTarget.localPosition, previousLeftHandLocalPosition);
                    float speed = moveDistance / Time.deltaTime;
                    if (speed > wallJumpHandSpeedThreshold)
                    {
                        speedConditionMet = true;
                        Debug.Log($"左手触发贴墙跳跃，速度: {speed}");
                    }
                }

                // 检查右手
                if (rightIs3DMode && rightSphereTarget != null)
                {
                    float moveDistance = Vector3.Distance(rightSphereTarget.localPosition, previousRightHandLocalPosition);
                    float speed = moveDistance / Time.deltaTime;
                    if (speed > wallJumpHandSpeedThreshold)
                    {
                        speedConditionMet = true;
                        Debug.Log($"右手触发贴墙跳跃，速度: {speed}");
                    }
                }

                // 如果速度未达标，不执行跳跃
                if (!speedConditionMet)
                {
                    return;
                }

                // 计算跳跃方向：墙面法线方向和滑行方向之间的45度方向
                // 将墙面法线和滑行方向都归一化后取平均，得到45度方向
                Vector3 normalizedWallNormal = wallNormal.normalized;
                Vector3 normalizedSlideDirection = wallSlideDirection.normalized;
                
                // 取墙面法线和滑行方向的中间方向（45度）
                Vector3 jumpDirection = (normalizedWallNormal + normalizedSlideDirection).normalized;

                // 如果两向量几乎抵消，退化为沿墙法线方向跳，并加一点上抬
                if (jumpDirection.sqrMagnitude < 0.0001f)
                {
                    jumpDirection = (normalizedWallNormal + Vector3.up * 0.5f).normalized;
                }
                else
                {
                    // 添加一个向上的分量，使跳跃有一定的向上力度
                    jumpDirection = new Vector3(jumpDirection.x, 0.5f, jumpDirection.z).normalized;
                }

                // 使用固定速度计算跳跃速度
                Vector3 jumpVelocity = jumpDirection * wallJumpSpeed;

                // 设置贴墙跳跃保护标志，防止速度被后续逻辑覆盖
                wallJumpProtection = true;

                // 应用跳跃速度
                if (thisRb != null)
                {
                    // 直接设置线速度，并用VelocityChange再推一遍，确保立即生效
                    thisRb.linearVelocity = jumpVelocity;
                    DebugGraph.Write("贴墙跳跃");
                    Debug.LogError("贴墙跳跃速度" + jumpVelocity);
                }

                Debug.Log("贴墙跳跃触发，方向: " + jumpDirection + "，速度: " + jumpVelocity);

                // 退出贴墙滑行状态
                ExitWallSliding();
                
                // 跳跃后直接返回，不再执行后续逻辑
                return;
            }
        }

        /// <summary>
        /// 贴墙滑行时的移动逻辑
        /// </summary>
        public void WallSlidingMovement()
        {
            // 如果当前不再是贴墙滑行状态（例如刚刚跳跃了），则不执行滑行移动逻辑，防止覆盖跳跃速度
            if (CurrentStateType != MovementState.WallSliding)
                return;

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
            {
                Debug.LogError("<color=red>[CheckWallAttachment] bodyPosition为空！</color>");
                return;
            }

            Debug.Log($"<color=cyan>[CheckWallAttachment] 开始检测墙体附着，法线: {wallNormal}</color>");

            // 从body位置向墙面法线的反方向发射射线
            Vector3 rayDirection = -wallNormal;
            Ray ray = new Ray(bodyPosition.position, rayDirection);

            Debug.Log($"<color=cyan>[CheckWallAttachment] 射线起点: {bodyPosition.position}, 方向: {rayDirection}, 距离: {wallCheckRayDistance}</color>");

            // 绘制黄色射线
            Color rayColor = Color.yellow;

            // 使用RaycastAll检测所有碰撞，然后过滤出Wall tag的物体
            RaycastHit[] hits = Physics.RaycastAll(ray, wallCheckRayDistance);
            bool stillAttached = false;

            Debug.Log($"<color=cyan>[CheckWallAttachment] 检测到 {hits.Length} 个碰撞体</color>");

            foreach (RaycastHit hitInfo in hits)
            {
                Debug.Log($"<color=cyan>[CheckWallAttachment] 碰撞体: {hitInfo.collider.gameObject.name}, Tag: {hitInfo.collider.tag}, 距离: {hitInfo.distance}</color>");
                
                if (hitInfo.collider.CompareTag("Wall"))
                {
                    stillAttached = true;
                    Debug.Log($"<color=green>[CheckWallAttachment] ✓ 找到墙体: {hitInfo.collider.gameObject.name}</color>");
                    break;
                }
            }

            if (!stillAttached)
            {
                // ★★★ 检查是否在保护期内 ★★★
                if (IsInWallSlideProtection())
                {
                    Debug.LogWarning($"<color=yellow>[CheckWallAttachment] 保护期内（剩余: {wallSlideProtectionTimer:F2}秒），忽略射线检测失败</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=red>[CheckWallAttachment] ✗ 未检测到墙体，准备退出贴墙状态</color>");
                    Debug.LogWarning($"<color=yellow>[CheckWallAttachment] 射线参数 - 起点: {bodyPosition.position}, 方向: {rayDirection}, 距离: {wallCheckRayDistance}</color>");
                    Debug.LogWarning($"<color=yellow>[CheckWallAttachment] 检测到的碰撞体数量: {hits.Length}</color>");

                    // 如果射线没有检测到墙体，退出贴墙状态
                    ExitWallSliding();
                }
            }
            else
            {
                Debug.Log("<color=green>[CheckWallAttachment] ✓ 墙体附着检测通过</color>");
            }

            // 在Scene视图中绘制射线
            Debug.DrawRay(bodyPosition.position, rayDirection * wallCheckRayDistance, rayColor, 0.1f);
        }

        /// <summary>
        /// 进入贴墙滑行状态
        /// </summary>
        public void EnterWallSliding(Vector3 normal)
        {
            // 如果已经在贴墙滑行状态，只更新墙面法线，不重新进入状态
            if (CurrentStateType == MovementState.WallSliding)
            {
                Debug.LogWarning("<color=yellow>[EnterWallSliding] 已经在贴墙状态，只更新墙面法线（避免重复进入）</color>");
                Debug.LogWarning($"<color=yellow>[EnterWallSliding] 旧法线: {wallNormal} → 新法线: {normal}</color>");
                wallNormal = normal;
                return; // 不重新进入状态
            }

            wallNormal = normal;
            wallSlideTimer = 0f;

            Debug.Log("<color=green>[EnterWallSliding] ★★★ 首次进入贴墙滑行状态 ★★★</color>");

            ChangeState(new WallSlidingState(this));
        }

        /// <summary>
        /// 退出贴墙滑行状态
        /// </summary>
        public void ExitWallSliding()
        {
            if (CurrentStateType == MovementState.WallSliding)
            {
                // 获取调用堆栈信息
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(1, true);
                string callerMethod = stackTrace.GetFrame(0)?.GetMethod()?.Name ?? "Unknown";
                
                Debug.LogWarning($"<color=magenta>[ExitWallSliding] ★★★ 退出贴墙滑行状态 ★★★</color>");
                Debug.LogWarning($"<color=magenta>[ExitWallSliding] 调用来源: {callerMethod}</color>");
                Debug.LogWarning($"<color=magenta>[ExitWallSliding] 当前状态: {CurrentStateType}</color>");
                Debug.LogWarning($"<color=magenta>[ExitWallSliding] 是否在地面: {isOnGround}</color>");
                Debug.LogWarning($"<color=magenta>[ExitWallSliding] 当前速度: {thisRb?.linearVelocity}</color>");
                Debug.LogWarning($"<color=magenta>[ExitWallSliding] 贴墙时长: {wallSlideTimer}秒</color>");

                // 清除residualVelocity，让速度就是当前离开墙面时的实际速度
                residualVelocity = Vector3.zero;

                // 根据当前是否在地面来决定下一个状态
                if (isOnGround)
                {
                    Debug.LogWarning("<color=magenta>[ExitWallSliding] → 切换到地面状态</color>");
                    ChangeState(new GroundedState(this));
                }
                else
                {
                    Debug.LogWarning("<color=magenta>[ExitWallSliding] → 切换到空中状态</color>");
                    ChangeState(new AirborneState(this));
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[ExitWallSliding] 当前不在贴墙状态（当前状态: {CurrentStateType}），忽略退出请求</color>");
            }
        }

        /// <summary>
        /// 启动贴墙保护计时器
        /// </summary>
        public void StartWallSlideProtection()
        {
            wallSlideProtectionTimer = WALL_SLIDE_PROTECTION_DURATION;
            Debug.Log($"<color=green>[WallSlideProtection] 启动贴墙保护，持续 {WALL_SLIDE_PROTECTION_DURATION} 秒</color>");
        }

        /// <summary>
        /// 更新贴墙保护计时器（需要在 Update 中调用）
        /// </summary>
        public void UpdateWallSlideProtection()
        {
            if (wallSlideProtectionTimer > 0f)
            {
                wallSlideProtectionTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// 检查是否在贴墙保护期内
        /// </summary>
        public bool IsInWallSlideProtection()
        {
            return wallSlideProtectionTimer > 0f;
        }

        #endregion

        #region 碰撞检测

        void OnCollisionEnter(Collision collision)
        {
            // ========== 第一步：检查碰撞对象是否为墙体 ==========
            Debug.Log($"<color=cyan>[碰撞检测] 碰到物体: {collision.gameObject.name}, Tag: {collision.gameObject.tag}</color>");
            
            if (!collision.gameObject.CompareTag("Wall"))
            {
                Debug.LogWarning($"<color=yellow>[碰撞检测] 物体不是Wall标签，跳过处理</color>");
                return;
            }

            Debug.Log("<color=green>[碰撞检测] ✓ 确认为墙体碰撞</color>");

            // ========== 新增：检查是否已经在贴墙状态 ==========
            if (CurrentStateType == MovementState.WallSliding)
            {
                Debug.LogWarning("<color=yellow>[碰撞检测] ✗ 已经在贴墙滑行状态，跳过重复进入（避免先退出再进入导致状态混乱）</color>");
                Debug.LogWarning($"<color=yellow>[碰撞检测] 当前墙面法线: {wallNormal}, 新墙面法线: {(collision.contactCount > 0 ? collision.GetContact(0).normal.ToString() : "无")}</color>");
                return;
            }

            // ========== 第二步：检查接触点 ==========
            if (collision.contactCount <= 0)
            {
                Debug.LogError("<color=red>[碰撞检测] ✗ 没有接触点信息！</color>");
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            Vector3 normal = contact.normal;
            Debug.Log($"<color=green>[碰撞检测] ✓ 接触点数量: {collision.contactCount}, 法线: {normal}</color>");

            // 从碰撞点绘制法线（红色）
            Debug.DrawRay(contact.point, normal * 2f, Color.red, 2f);

            // ========== 第三步：检查刚体 ==========
            if (thisRb == null)
            {
                Debug.LogError("<color=red>[碰撞检测] ✗ Rigidbody为空！</color>");
                return;
            }

            Vector3 velocity = thisRb.linearVelocity;
            Debug.Log($"<color=cyan>[碰撞检测] 当前速度: {velocity}, 速度大小: {velocity.magnitude}</color>");

            // ========== 第四步：计算投影向量 ==========
            Vector3 projection = Vector3.ProjectOnPlane(velocity, normal);
            Debug.Log($"<color=cyan>[碰撞检测] 速度在墙面上的投影: {projection}</color>");

            // Y轴归零，变成水平向量
            Vector3 horizontalProjection = new Vector3(projection.x, 0, projection.z);
            Debug.Log($"<color=cyan>[碰撞检测] 水平投影（Y归零前）: {horizontalProjection}, 长度: {horizontalProjection.magnitude}</color>");

            // 长度重置为1
            if (horizontalProjection != Vector3.zero)
            {
                horizontalProjection = horizontalProjection.normalized;
                Debug.Log($"<color=green>[碰撞检测] ✓ 归一化后的水平投影: {horizontalProjection}</color>");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[碰撞检测] ! 水平投影为零向量</color>");
            }

            // 从碰撞点绘制投影向量（绿色）
            Debug.DrawRay(contact.point, horizontalProjection * 2f, Color.green, 2f);

            // ========== 第五步：检查当前状态 ==========
            Debug.Log($"<color=cyan>[碰撞检测] 当前状态: {CurrentStateType}</color>");
            Debug.Log($"<color=cyan>[碰撞检测] 是否在地面: {isOnGround}</color>");

            bool isDashing = CurrentStateType == MovementState.Dashing;
            bool isAirborne = !isOnGround;
            bool hasHorizontalProjection = horizontalProjection != Vector3.zero;

            Debug.Log($"<color=cyan>[碰撞检测] 状态检查 - 是否冲刺: {isDashing}, 是否空中: {isAirborne}, 有水平投影: {hasHorizontalProjection}</color>");

            // ========== 第六步：判断是否可以进入贴墙滑行 ==========
            bool canEnterWallSlide = hasHorizontalProjection && isAirborne;
            bool dashingIntoWall = isDashing && isAirborne;

            Debug.Log($"<color=cyan>[碰撞检测] 条件判断 - 普通条件满足: {canEnterWallSlide}, 冲刺条件满足: {dashingIntoWall}</color>");

            if (canEnterWallSlide || dashingIntoWall)
            {
                Debug.Log("<color=green>[碰撞检测] ✓ 满足进入贴墙滑行的基本条件</color>");

                // 如果是冲刺状态且投影向量为零，使用冲刺方向计算滑行方向
                if (isDashing && horizontalProjection == Vector3.zero)
                {
                    Debug.Log($"<color=yellow>[碰撞检测] 冲刺状态下投影为零，使用冲刺方向: {dashDirection}</color>");
                    
                    // 使用冲刺方向在墙面上的投影作为滑行方向
                    Vector3 dashProjection = Vector3.ProjectOnPlane(dashDirection, normal);
                    Debug.Log($"<color=cyan>[碰撞检测] 冲刺方向在墙面上的投影: {dashProjection}</color>");
                    
                    horizontalProjection = new Vector3(dashProjection.x, 0, dashProjection.z);
                    Debug.Log($"<color=cyan>[碰撞检测] 水平化后的投影: {horizontalProjection}, 长度: {horizontalProjection.magnitude}</color>");
                    
                    if (horizontalProjection != Vector3.zero)
                    {
                        horizontalProjection = horizontalProjection.normalized;
                        Debug.Log($"<color=green>[碰撞检测] ✓ 归一化后的滑行方向: {horizontalProjection}</color>");
                    }
                    else
                    {
                        Debug.LogWarning("<color=yellow>[碰撞检测] ! 归一化后仍为零向量</color>");
                    }
                }

                // 如果仍然没有有效的滑行方向，则不进入滑行状态
                if (horizontalProjection == Vector3.zero)
                {
                    Debug.LogError("<color=red>[碰撞检测] ✗ 无法计算有效的滑行方向，不进入滑行状态</color>");
                    Debug.LogError($"<color=red>[原因分析] 速度: {velocity}, 法线: {normal}, 冲刺方向: {dashDirection}</color>");
                    return;
                }

                // 保存投影向量用于贴墙滑行
                wallSlideDirection = horizontalProjection;
                Debug.Log($"<color=green>[碰撞检测] ✓ 设置滑行方向: {wallSlideDirection}</color>");

                if (isDashing)
                {
                    Debug.Log("<color=green>[碰撞检测] ★★★ 冲刺状态下碰到墙体，进入贴墙滑行状态！★★★</color>");
                }
                else
                {
                    Debug.Log("<color=green>[碰撞检测] ★★★ 普通状态下碰到墙体，进入贴墙滑行状态！★★★</color>");
                }

                // 进入贴墙滑行状态
                EnterWallSliding(normal);
            }
            else
            {
                // ========== 详细输出不满足条件的原因 ==========
                Debug.LogWarning("<color=red>[碰撞检测] ✗ 不满足进入贴墙滑行的条件</color>");
                
                if (isOnGround)
                {
                    Debug.LogWarning("<color=yellow>[原因] 角色在地面上（isOnGround = true）</color>");
                }
                
                if (!hasHorizontalProjection && !isDashing)
                {
                    Debug.LogWarning("<color=yellow>[原因] 没有水平投影向量 且 不在冲刺状态</color>");
                }
                
                if (CurrentStateType == MovementState.WallSliding)
                {
                    Debug.LogWarning("<color=yellow>[原因] 已经在贴墙滑行状态中</color>");
                }

                Debug.LogWarning($"<color=yellow>[详细信息] 水平投影: {horizontalProjection}, 冲刺: {isDashing}, 地面: {isOnGround}, 状态: {CurrentStateType}</color>");
            }
        }

        void OnCollisionExit(Collision collision)
        {
            Debug.Log($"<color=orange>[OnCollisionExit] 离开碰撞: {collision.gameObject.name}, Tag: {collision.gameObject.tag}</color>");
            Debug.Log($"<color=orange>[OnCollisionExit] 当前状态: {CurrentStateType}</color>");
            
            // ★★★ 关键修改：贴墙滑行状态下，完全不依赖 OnCollisionExit 来退出 ★★★
            // ★★★ 只依靠 CheckWallAttachment 的射线检测来判断是否脱离墙面 ★★★
            if (collision.gameObject.CompareTag("Wall"))
            {
                Debug.LogWarning($"<color=orange>[OnCollisionExit] ✓ 确认离开墙体: {collision.gameObject.name}</color>");
                
                if (CurrentStateType == MovementState.WallSliding)
                {
                    // 不在这里退出贴墙状态，而是完全依赖射线检测
                    Debug.LogWarning("<color=yellow>[OnCollisionExit] 贴墙状态下忽略碰撞退出事件，将由射线检测决定是否退出</color>");
                    // 不调用 ExitWallSliding()，让 CheckWallAttachment 来处理
                }
                else
                {
                    Debug.Log($"<color=orange>[OnCollisionExit] 当前不在贴墙状态（状态: {CurrentStateType}），不需要退出</color>");
                }
            }
            else
            {
                Debug.Log($"<color=orange>[OnCollisionExit] 不是墙体，忽略</color>");
            }
        }

        void OnCollisionStay(Collision collision)
        {
            // 记录持续碰撞信息（仅在贴墙状态且每60帧记录一次）
            if (collision.gameObject.CompareTag("Wall") && CurrentStateType == MovementState.WallSliding)
            {
                if (Time.frameCount % 60 == 0) // 每60帧记录一次，避免日志过多
                {
                    Debug.Log($"<color=cyan>[OnCollisionStay] 持续接触墙体: {collision.gameObject.name}, 接触点数: {collision.contactCount}</color>");
                }
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

            // // 检测冲刺输入（使用左手柄的PrimaryButton，通常是A键）
            // if (InputActionsManager.Actions.XRILeftInteraction.Activate.WasPressedThisFrame() && CanDash())
            // {
            //     StartDash();
            // }
        }

        // 检查是否可以冲刺
        private bool CanDash()
        {
            return dashCooldownTimer <= 0f &&
                   CurrentStateType != MovementState.Dashing;
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

        #region Hook冲刺相关方法

        /// <summary>
        /// 处理Hook冲刺输入
        /// </summary>
        private void HandleHookDash()
        {
            // 检测hook冲刺输入（使用左摇杆的按下事件）
            bool hookDashInput = InputActionsManager.Actions.XRILeftInteraction.ScaleToggle.IsPressed();

            if (hookDashInput && CanHookDash())
            {
                StartHookDash();
            }
        }

        /// <summary>
        /// 检查是否可以进行Hook冲刺
        /// </summary>
        private bool CanHookDash()
        {
            // 检查GameManager中是否有ClosestAngleHook，并且当前不在hook冲刺状态
            // 允许在贴墙滑行状态下进行hook冲刺
            return GameManager.Instance != null &&
                   GameManager.Instance.ClosestAngleHook != null &&
                   CurrentStateType != MovementState.HookDashing;
        }

        /// <summary>
        /// 开始Hook冲刺
        /// </summary>
        private void StartHookDash()
        {
            hookDashTimer = hookDashDuration;
            hookTarget = GameManager.Instance.ClosestAngleHook;

            // 计算冲向hook的方向
            if (hookTarget != null)
            {
                hookDashDirection = (hookTarget.position - transform.position).normalized;
            }

            ChangeState(new HookDashingState(this));
            Debug.Log("开始Hook冲刺，目标: " + hookTarget.name);
        }

        /// <summary>
        /// 结束Hook冲刺
        /// </summary>
        public void EndHookDash()
        {
            // 到达hook位置后进入浮空状态，并保留一定速度
            if (thisRb != null)
            {
                thisRb.linearVelocity = thisRb.linearVelocity.normalized * 10f;
            }

            // 根据当前是否在地面来决定下一个状态
            if (isOnGround)
                ChangeState(new GroundedState(this));
            else
                ChangeState(new AirborneState(this));

            Debug.Log("结束Hook冲刺");
        }

        /// <summary>
        /// 处理Hook冲刺移动
        /// </summary>
        public void HookDashMovement()
        {
            if (thisRb != null && hookTarget != null)
            {
                Debug.Log("Hook冲刺中");

                // 计算到目标的距离
                float distanceToHook = Vector3.Distance(transform.position, hookTarget.position);
                float step = hookDashSpeed * Time.deltaTime;

                // 如果本次步进会到达或超过目标位置，则直接移动到目标并结束hook冲刺，避免穿透或跳过
                if (distanceToHook <= step)
                {
                    // 精确移动到目标位置
                    thisRb.MovePosition(hookTarget.position);
                    EndHookDash();
                }
                else
                {
                    // 以冲刺速度冲向hook目标位置（使用立体的实际方向，包含Y轴分量）
                    Vector3 hookDashVelocity = hookDashDirection * hookDashSpeed;
                    thisRb.linearVelocity = hookDashVelocity;
                }
            }
        }

        /// <summary>
        /// 外部调用触发Hook冲刺
        /// </summary>
        public void TriggerHookDash()
        {
            if (CanHookDash())
            {
                StartHookDash();
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

        #region 接口实现

        /// <summary>
        /// IPlayerHeadProvider 接口实现
        /// 获取玩家头部Transform
        /// </summary>
        /// <returns>头部Transform</returns>
        public Transform GetPlayerHead()
        {
            return playerHead;
        }

        /// <summary>
        /// IHookDashProvider 接口实现
        /// 外部调用处理hook冲刺
        /// </summary>
        public void OutHandleHookDash()
        {
            // 检测hook冲刺输入
            if (CanHookDash())
            {
                StartHookDash();
            }

            // 更新hook冲刺计时器
            if (CurrentStateType == MovementState.HookDashing)
            {
                hookDashTimer -= Time.deltaTime;
                if (hookDashTimer <= 0f)
                {
                    EndHookDash();
                }
            }
        }

        /// <summary>
        /// IMoveSpeedProvider 接口实现
        /// 设置额外的移动速度加成
        /// </summary>
        /// <param name="additionalSpeed">要添加的额外速度</param>
        public void SetAdditionalMoveSpeed(float additionalSpeed)
        {
            AddMoveSpeed = additionalSpeed * AddMoveSpeedMultiplier;
        }

        /// <summary>
        /// IMoveSpeedProvider 接口实现
        /// 获取当前的实际移动速度（基础速度 + 额外速度）
        /// </summary>
        /// <returns>实际移动速度</returns>
        public float GetActualMoveSpeed()
        {
            return finalVelocityMultiplier + AddMoveSpeed;
        }

        /// <summary>
        /// IBounceCubeProvider 接口实现
        /// 处理弹跳方块碰撞
        /// </summary>
        /// <param name="normal">弹跳方向</param>
        public void OutHandleBounceCube(Vector3 normal)
        {
            Debug.Log("触发弹跳方块，法线：" + normal);
            if (thisRb != null)
            {
                thisRb.linearVelocity = normal;
            }
        }

        // IWallSlidingProvider 接口的事件已在类的前面声明:
        // public event Action<Vector3> OnEnterWallSliding;
        // public event Action<Vector3> OnExitWallSliding;

        #endregion
    }
}
