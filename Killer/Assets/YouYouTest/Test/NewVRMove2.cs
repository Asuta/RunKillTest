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

            // 禁用刚体重力并立即重置速度为贴墙滑行速度
            if (controller.thisRb != null)
            {
                controller.thisRb.useGravity = false;
                
                // 立即重置速度，防止原来的高速度导致被挤出墙面
                float actualMoveSpeed = controller.finalVelocityMultiplier * controller.wallSlideSpeedMultiplier;
                Vector3 wallSlideVelocity = new Vector3(
                    controller.wallSlideDirection.x * actualMoveSpeed,
                    0,
                    controller.wallSlideDirection.z * actualMoveSpeed
                );
                controller.thisRb.linearVelocity = wallSlideVelocity;
            }
            
            // 启动贴墙保护计时器
            controller.StartWallSlideProtection();

            // 触发进入贴墙滑行事件
            controller.InvokeOnEnterWallSliding(controller.wallNormal);
        }

        public override void Update()
        {
            // 更新贴墙计时器
            controller.wallSlideTimer += Time.deltaTime;
            
            // 更新贴墙保护计时器
            controller.UpdateWallSlideProtection();

            // 持续检测是否还贴在墙上
            controller.CheckWallAttachment();
            
            // 检查状态是否仍然是贴墙状态
            if (controller.CurrentStateType != MovementState.WallSliding)
                return;

            // 检测贴墙状态下的跳跃
            controller.WallSlidingJumpCheck();
            
            if (controller.CurrentStateType != MovementState.WallSliding)
                return;

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







        [Tooltip("Lerp跟随速度，值越大跟随越快")]
        public float lerpSpeed = 5f;

        [Header("二段跳（空中位移跳跃）")]
        [Tooltip("是否启用二段跳：在空中额外允许跳跃一次")]
        public bool enableDoubleJump = true;

        [Tooltip("空中位移跳跃的持续施力时间（秒）")]
        public float airborneMoveJumpSustainDuration = 0.2f;

        [Tooltip("把‘手的位移差(米)’换算为‘角色速度变化(米/秒)’的倍率（每个 FixedUpdate 以 VelocityChange 施加）")]
        public float airborneMoveJumpVelocityChangeMultiplier = 25f;

        [Tooltip("空中位移跳跃 Y 轴倍率（用于让上抬更明显/更弱）")]
        public float airborneMoveJumpYMultiplier = 1f;

        [Tooltip("单次 FixedUpdate 允许施加的最大速度变化（米/秒），防止瞬间过大")]
        public float airborneMoveJumpMaxVelocityChangePerStep = 8f;


        #region 私有变量

        // 用于检测grip键状态变化
        private bool lastLeftGripPressed;
        private bool lastRightGripPressed;
        private bool lastLeftTriggerPressed;
        private bool lastRightTriggerPressed;

        // 记录Trigger松开的时间，用于处理"同时松开"的容差
        private float lastLeftTriggerReleaseTime = -100f;
        private float lastRightTriggerReleaseTime = -100f;

        // 二段跳（空中位移跳跃）状态
        private bool canAirDoubleJump = true;
        private float airborneMoveJumpTimer = 0f;
        private bool lastIs3DMovementMode = false;
        private Vector3 prevLeftHandLocalPosForAirJump;
        private Vector3 prevRightHandLocalPosForAirJump;

        // 贴墙跳跃（持续施力）状态
        private float wallMoveJumpTimer = 0f;
        private Vector3 prevLeftHandLocalPosForWallJump;
        private Vector3 prevRightHandLocalPosForWallJump;

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

            // 二段跳输入检测（空中位移跳跃，持续施力）
            HandleAirborneMoveJump();
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

            // 空中位移跳跃：在 0.2 秒内持续施力（VelocityChange）
            ApplyAirborneMoveJumpSustainForce();

            // 贴墙跳跃：在 0.2 秒内持续施力（VelocityChange）
            ApplyWallMoveJumpSustainForce();
            DebugGraph.Log("final speeeeed", thisRb.linearVelocity.magnitude);
        }

        #endregion

        #region 状态机管理

        // 状态切换方法
        public void ChangeState(MovementStateBase newState)
        {
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
            
            currentState?.Exit();
            currentState = newState;
            CurrentStateType = newStateType;
            currentState.Enter();

            // 状态切换时，对二段跳相关状态做最小化维护
            if (CurrentStateType == MovementState.Grounded)
            {
                canAirDoubleJump = true;
                airborneMoveJumpTimer = 0f;
            }
            else if (CurrentStateType == MovementState.WallSliding)
            {
                // 每次进入贴墙滑行（碰到滑行墙并进入该状态）时，刷新一次二段跳次数
                canAirDoubleJump = true;
            }
        }

        private bool GetIs3DMovementMode()
        {
            bool leftGripPressed = InputActionsManager.Actions.XRILeftInteraction.Select.IsPressed();
            bool rightGripPressed = InputActionsManager.Actions.XRIRightInteraction.Select.IsPressed();
            bool leftTriggerPressed = InputActionsManager.Actions.XRILeftInteraction.Activate.IsPressed();
            bool rightTriggerPressed = InputActionsManager.Actions.XRIRightInteraction.Activate.IsPressed();

            return (leftGripPressed && leftTriggerPressed) || (rightGripPressed && rightTriggerPressed);
        }

        /// <summary>
        /// 空中二段跳：在空中额外允许一次“位移跳跃”，并在短时间内持续施力
        /// </summary>
        private void HandleAirborneMoveJump()
        {
            bool is3DMode = GetIs3DMovementMode();
            bool risingEdge = is3DMode && !lastIs3DMovementMode;
            lastIs3DMovementMode = is3DMode;

            if (!enableDoubleJump)
                return;

            // 关键：如果当前正在进行贴墙跳的持续施力（或者刚刚触发），则不允许触发二段跳
            // 这样可以确保贴墙跳动作本身不会因为 Rising Edge 立即消耗掉二段跳机会
            if (wallMoveJumpTimer > 0f)
                return;

            // 只允许在空中触发一次
            if (CurrentStateType != MovementState.Airborne)
                return;

            // 冲刺/贴墙/Hook 冲刺期间不允许触发（避免逻辑打架）
            if (CurrentStateType == MovementState.Dashing || CurrentStateType == MovementState.WallSliding || CurrentStateType == MovementState.HookDashing)
                return;

            if (!risingEdge)
                return;

            if (!canAirDoubleJump)
                return;

            StartAirborneMoveJumpSustain();
        }

        private void StartAirborneMoveJumpSustain()
        {
            // 避免与贴墙跳跃持续施力叠加
            wallMoveJumpTimer = 0f;

            canAirDoubleJump = false;
            airborneMoveJumpTimer = Mathf.Max(0f, airborneMoveJumpSustainDuration);

            if (leftSphereTarget != null)
                prevLeftHandLocalPosForAirJump = leftSphereTarget.localPosition;
            if (rightSphereTarget != null)
                prevRightHandLocalPosForAirJump = rightSphereTarget.localPosition;

            Debug.Log($"触发二段跳（空中位移跳跃），持续施力 {airborneMoveJumpTimer:0.###} 秒");
        }

        private void StartWallMoveJumpSustain()
        {
            // 避免与空中二段跳持续施力叠加
            airborneMoveJumpTimer = 0f;

            wallMoveJumpTimer = Mathf.Max(0f, airborneMoveJumpSustainDuration);

            if (leftSphereTarget != null)
                prevLeftHandLocalPosForWallJump = leftSphereTarget.localPosition;
            if (rightSphereTarget != null)
                prevRightHandLocalPosForWallJump = rightSphereTarget.localPosition;

            Debug.Log($"触发贴墙跳跃（持续施力），持续施力 {wallMoveJumpTimer:0.###} 秒");
        }

        private Transform GetAirJumpReferenceTransform()
        {
            if (vrOrigin != null)
                return vrOrigin;
            if (playerHead != null)
                return playerHead;
            return transform;
        }

        private void ApplyAirborneMoveJumpSustainForce()
        {
            if (thisRb == null)
                return;

            // 运行时关闭二段跳：立刻停止持续施力
            if (!enableDoubleJump)
            {
                airborneMoveJumpTimer = 0f;
                return;
            }

            if (airborneMoveJumpTimer <= 0f)
                return;

            // 如果不在空中（比如落地了），直接结束持续施力
            if (CurrentStateType != MovementState.Airborne)
            {
                airborneMoveJumpTimer = 0f;
                return;
            }

            airborneMoveJumpTimer -= Time.fixedDeltaTime;

            // 使用手部 target 的本地坐标位移差：避免玩家自身位移/刚体运动导致世界坐标变化污染输入
            Vector3 localDeltaSum = Vector3.zero;
            Transform deltaReference = null;

            if (leftSphereTarget != null)
            {
                Vector3 current = leftSphereTarget.localPosition;
                localDeltaSum += (current - prevLeftHandLocalPosForAirJump);
                prevLeftHandLocalPosForAirJump = current;
                if (deltaReference == null) deltaReference = leftSphereTarget.parent;
            }
            if (rightSphereTarget != null)
            {
                Vector3 current = rightSphereTarget.localPosition;
                localDeltaSum += (current - prevRightHandLocalPosForAirJump);
                prevRightHandLocalPosForAirJump = current;
                if (deltaReference == null) deltaReference = rightSphereTarget.parent;
            }

            // 将 local 位移差转换到世界方向：用 target 的父物体作为参考坐标系（最符合 localPosition 的定义）
            if (deltaReference == null)
                deltaReference = GetAirJumpReferenceTransform();

            Vector3 worldDelta = deltaReference.TransformVector(localDeltaSum);

            // 手往某方向移动，一般希望角色往相反方向加速，所以取反
            Vector3 deltaV = -worldDelta * airborneMoveJumpVelocityChangeMultiplier;
            deltaV = new Vector3(deltaV.x, deltaV.y * airborneMoveJumpYMultiplier, deltaV.z);

            // 关键：分离限幅，避免 Y 分量过大时把 XZ 挤没（导致“看起来只能竖直跳”）
            if (airborneMoveJumpMaxVelocityChangePerStep > 0f)
            {
                Vector3 horizontalDeltaV = new Vector3(deltaV.x, 0, deltaV.z);
                if (horizontalDeltaV.magnitude > airborneMoveJumpMaxVelocityChangePerStep)
                {
                    horizontalDeltaV = horizontalDeltaV.normalized * airborneMoveJumpMaxVelocityChangePerStep;
                }

                float clampedY = Mathf.Clamp(deltaV.y, -airborneMoveJumpMaxVelocityChangePerStep, airborneMoveJumpMaxVelocityChangePerStep);
                deltaV = new Vector3(horizontalDeltaV.x, clampedY, horizontalDeltaV.z);
            }

            // 持续施加速度变化（与质量无关）
            thisRb.AddForce(deltaV, ForceMode.VelocityChange);

            // 速度上限保护：水平速度不超过 maxTranslationSpeed，Y 轴不超过 maxJumpForceY
            Vector3 v = thisRb.linearVelocity;
            Vector3 horizontal = new Vector3(v.x, 0, v.z);

            // 地面逻辑里水平速度会再乘 finalVelocityMultiplier（所以实际水平可远大于 maxTranslationSpeed）
            float horizontalMax = Mathf.Max(0f, maxTranslationSpeed * Mathf.Max(1f, finalVelocityMultiplier));
            if (horizontalMax > 0f && horizontal.magnitude > horizontalMax)
            {
                horizontal = horizontal.normalized * horizontalMax;
                v = new Vector3(horizontal.x, v.y, horizontal.z);
            }
            v = new Vector3(v.x, Mathf.Clamp(v.y, -maxJumpForceY, maxJumpForceY), v.z);
            thisRb.linearVelocity = v;
        }

        private void ApplyWallMoveJumpSustainForce()
        {
            if (thisRb == null)
                return;

            if (wallMoveJumpTimer <= 0f)
                return;

            // 贴墙跳跃触发后会退出贴墙状态，此处不强制要求状态；只要计时器在走就持续施力
            wallMoveJumpTimer -= Time.fixedDeltaTime;

            Vector3 localDeltaSum = Vector3.zero;
            Transform deltaReference = null;

            if (leftSphereTarget != null)
            {
                Vector3 current = leftSphereTarget.localPosition;
                localDeltaSum += (current - prevLeftHandLocalPosForWallJump);
                prevLeftHandLocalPosForWallJump = current;
                if (deltaReference == null) deltaReference = leftSphereTarget.parent;
            }
            if (rightSphereTarget != null)
            {
                Vector3 current = rightSphereTarget.localPosition;
                localDeltaSum += (current - prevRightHandLocalPosForWallJump);
                prevRightHandLocalPosForWallJump = current;
                if (deltaReference == null) deltaReference = rightSphereTarget.parent;
            }

            if (deltaReference == null)
                deltaReference = GetAirJumpReferenceTransform();

            Vector3 worldDelta = deltaReference.TransformVector(localDeltaSum);

            Vector3 deltaV = -worldDelta * airborneMoveJumpVelocityChangeMultiplier;
            deltaV = new Vector3(deltaV.x, deltaV.y * airborneMoveJumpYMultiplier, deltaV.z);

            if (airborneMoveJumpMaxVelocityChangePerStep > 0f)
            {
                Vector3 horizontalDeltaV = new Vector3(deltaV.x, 0, deltaV.z);
                if (horizontalDeltaV.magnitude > airborneMoveJumpMaxVelocityChangePerStep)
                {
                    horizontalDeltaV = horizontalDeltaV.normalized * airborneMoveJumpMaxVelocityChangePerStep;
                }

                float clampedY = Mathf.Clamp(deltaV.y, -airborneMoveJumpMaxVelocityChangePerStep, airborneMoveJumpMaxVelocityChangePerStep);
                deltaV = new Vector3(horizontalDeltaV.x, clampedY, horizontalDeltaV.z);
            }

            thisRb.AddForce(deltaV, ForceMode.VelocityChange);

            Vector3 v = thisRb.linearVelocity;
            Vector3 horizontal = new Vector3(v.x, 0, v.z);
            float horizontalMax = Mathf.Max(0f, maxTranslationSpeed * Mathf.Max(1f, finalVelocityMultiplier));
            if (horizontalMax > 0f && horizontal.magnitude > horizontalMax)
            {
                horizontal = horizontal.normalized * horizontalMax;
                v = new Vector3(horizontal.x, v.y, horizontal.z);
            }
            v = new Vector3(v.x, Mathf.Clamp(v.y, -maxJumpForceY, maxJumpForceY), v.z);
            thisRb.linearVelocity = v;
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
            // 在空中状态下，始终使用蓝色表示纯物理模式
            // Color velocityColor = Color.blue;
            // Debug.DrawLine(transform.position, transform.position + thisRb.linearVelocity * 11f, velocityColor, 0.1f);

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
                // 改为“持续施力”的贴墙跳跃：方向/力度由 0.2 秒内手部位移差决定
                wallJumpProtection = true;

                // 先退出贴墙滑行，避免 WallSlidingMovement 覆盖速度
                ExitWallSliding();

                // 启动持续施力计时器
                StartWallMoveJumpSustain();

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
                return;

            // 从body位置向墙面法线的反方向发射射线
            Vector3 rayDirection = -wallNormal;
            Ray ray = new Ray(bodyPosition.position, rayDirection);

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
                // 检查是否在保护期内
                if (!IsInWallSlideProtection())
                {
                    // 如果射线没有检测到墙体，退出贴墙状态
                    ExitWallSliding();
                }
            }

            // 在Scene视图中绘制射线
            Debug.DrawRay(bodyPosition.position, rayDirection * wallCheckRayDistance, Color.yellow, 0.1f);
        }

        /// <summary>
        /// 进入贴墙滑行状态
        /// </summary>
        public void EnterWallSliding(Vector3 normal)
        {
            // 如果已经在贴墙滑行状态，只更新墙面法线
            if (CurrentStateType == MovementState.WallSliding)
            {
                wallNormal = normal;
                return;
            }

            wallNormal = normal;
            wallSlideTimer = 0f;

            ChangeState(new WallSlidingState(this));
        }

        /// <summary>
        /// 退出贴墙滑行状态
        /// </summary>
        public void ExitWallSliding()
        {
            if (CurrentStateType == MovementState.WallSliding)
            {
                // 清除residualVelocity
                residualVelocity = Vector3.zero;

                // 根据当前是否在地面来决定下一个状态
                if (isOnGround)
                    ChangeState(new GroundedState(this));
                else
                    ChangeState(new AirborneState(this));
            }
        }

        /// <summary>
        /// 启动贴墙保护计时器
        /// </summary>
        public void StartWallSlideProtection()
        {
            wallSlideProtectionTimer = WALL_SLIDE_PROTECTION_DURATION;
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
            // 检查是否为墙体
            if (!collision.gameObject.CompareTag("Wall"))
                return;

            // 检查是否已经在贴墙状态
            if (CurrentStateType == MovementState.WallSliding)
                return;

            // 检查接触点
            if (collision.contactCount <= 0)
                return;

            ContactPoint contact = collision.GetContact(0);
            Vector3 normal = contact.normal;

            // 从gizmo绘制法线
            Debug.DrawRay(contact.point, normal * 2f, Color.red, 2f);

            if (thisRb == null)
                return;

            Vector3 velocity = thisRb.linearVelocity;

            // 计算速度在墙面上的投影向量
            Vector3 projection = Vector3.ProjectOnPlane(velocity, normal);
            Vector3 horizontalProjection = new Vector3(projection.x, 0, projection.z);

            if (horizontalProjection != Vector3.zero)
                horizontalProjection = horizontalProjection.normalized;

            // 绘制投影向量
            Debug.DrawRay(contact.point, horizontalProjection * 2f, Color.green, 2f);

            bool isDashing = CurrentStateType == MovementState.Dashing;
            bool isAirborne = !isOnGround;
            bool hasHorizontalProjection = horizontalProjection != Vector3.zero;

            // 判断是否可以进入贴墙滑行
            bool canEnterWallSlide = hasHorizontalProjection && isAirborne;
            bool dashingIntoWall = isDashing && isAirborne;

            if (canEnterWallSlide || dashingIntoWall)
            {
                // 如果是冲刺状态且投影向量为零，使用冲刺方向计算滑行方向
                if (isDashing && horizontalProjection == Vector3.zero)
                {
                    Vector3 dashProjection = Vector3.ProjectOnPlane(dashDirection, normal);
                    horizontalProjection = new Vector3(dashProjection.x, 0, dashProjection.z);
                    if (horizontalProjection != Vector3.zero)
                        horizontalProjection = horizontalProjection.normalized;
                }

                // 如果没有有效的滑行方向，不进入滑行状态
                if (horizontalProjection == Vector3.zero)
                    return;

                // 保存投影向量用于贴墙滑行
                wallSlideDirection = horizontalProjection;

                // 进入贴墙滑行状态
                EnterWallSliding(normal);
            }
        }

        void OnCollisionExit(Collision collision)
        {
            // 贴墙滑行状态下，完全不依赖 OnCollisionExit 来退出
            // 只依靠 CheckWallAttachment 的射线检测来判断是否脱离墙面
        }

        void OnCollisionStay(Collision collision)
        {
            // 不需要处理
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
            // 冲刺结束时将速度变为当前速度的50%
            if (thisRb != null)
            {
                // 获取当前速度并减少到50%
                Vector3 currentVelocity = thisRb.linearVelocity;
                thisRb.linearVelocity = currentVelocity * 0.43f;
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
