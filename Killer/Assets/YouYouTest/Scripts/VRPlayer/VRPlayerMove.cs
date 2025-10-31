using System;
using UnityEngine;

public class VRPlayerMove : MonoBehaviour, IPlayerHeadProvider, IDashProvider, IHookDashProvider, IMoveSpeedProvider, IWallSlidingProvider
{
    #region 组件引用
    public Transform leftHand;
    public Transform rightHand;
    public Transform body;
    public Transform head;
    private Rigidbody thisRb;
    public VRBody vRBody;
    #endregion

    #region 移动设置
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float AddMoveSpeed = 3f;
    public float AddMoveSpeedMultiplier = 3f;
    public float decelerationSpeed = 5f; // 减速速度
    public float extraGravity = 10f; // 额外重力
    public Vector3 moveDirection;
    #endregion

    #region 跳跃设置
    [Header("跳跃设置")]
    public float jumpForce = 8f; // 跳跃力度
    public float raycastDistance = 1.2f; // 地面检测距离
    public LayerMask groundLayerMask; // 地面层级
    #endregion

    #region 状态管理
    private enum MovementState { Grounded, Jumping, Falling, Dashing, WallSliding, HookDashing }
    private MovementState currentState = MovementState.Grounded;
    #endregion

    #region 冲刺设置
    [Header("冲刺设置")]
    public float dashSpeed = 15f; // 冲刺速度
    public float dashDuration = 0.3f; // 冲刺持续时间
    public float dashCooldown = 1f; // 冲刺冷却时间

    // 冲刺相关私有变量
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;
    #endregion

    #region 手部移动冲刺设置
    [Header("手部移动冲刺设置")]
    public Transform rightHandTarget; // 右手目标Transform
    public float handMoveSpeedThreshold = 1f; // 手部移动速度阈值

    // 手部移动检测相关私有变量
    private Vector3 previousHandLocalPosition;
    #endregion


    #region Hook冲刺设置
    // hook冲刺相关
    private Transform hookTarget; // 目标hook的Transform
    private float hookDashTimer = 0f; // hook冲刺计时器
    private float hookDashDuration = 0.3f; // hook冲刺持续时间
    private Vector3 hookDashDirection; // hook冲刺方向
    #endregion

    #region 转向设置
    [Header("转向设置")]
    public float rotationAngle = 30f; // 每次转向的角度
    public float rotationDeadzone = 0.2f; // 摇杆中立区，必须回到中立区后才可再次触发

    // 转向相关私有变量
    private bool rotationArmed = true; // 只有回到中立区后才允许下一次触发
    #endregion

    #region 贴墙滑行设置
    // 贴墙滑行相关
    private Vector3 wallNormal; // 存储墙面法线
    private float wallSlideTimer = 0f; // 贴墙计时器
    private Vector3 wallSlideDirection; // 存储贴墙滑行方向（投影向量）
        
    #endregion

    #region 调试设置
    // log setting
    public bool needLog = false;
        public event Action<Vector3> OnEnterWallSliding;
        public event Action<Vector3> OnExitWallSliding;

    #endregion

    #region Unity生命周期
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisRb = GetComponent<Rigidbody>();

        // 如果没有设置地面层级，使用默认的Building层级
        if (groundLayerMask == 0)
        {
            groundLayerMask = LayerMask.GetMask("Building");
        }

        // 初始化右手位置
        if (rightHandTarget != null)
        {
            previousHandLocalPosition = rightHandTarget.localPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 直接使用强类型属性访问 actions
        Vector2 moveInput = InputActionsManager.Actions.XRILeftLocomotion.Move.ReadValue<Vector2>();

        // 计算移动方向
        CalculateMovement(moveInput);

        // 检测地面
        CheckGrounded();

        // 处理跳跃输入
        HandleJump();

        // 处理冲刺输入
        HandleDash();

        // 处理hook冲刺输入
        HandleHookDash();

        // 处理转向输入
        HandleRotation();

        // 处理贴墙滑行
        HandleWallSliding();

        // 检测手部移动并触发冲刺
        HandleHandMoveDash();

        // 更新状态
        UpdateState();
    }

    void FixedUpdate()
    {
        // 在FixedUpdate中处理移动，确保物理计算的一致性
        HandleMovement();
        HandleDashMovement();
        HandleHookDashMovement(); // 处理hook冲刺移动
        ApplyExtraGravity();
        if (currentState == MovementState.WallSliding)
        {
            HandleWallSlideMovement(); // 处理贴墙滑行移动
        }
    }
    #endregion

    #region 移动相关方法
    /// <summary>
    /// 根据摇杆输入计算移动方向
    /// </summary>
    /// <param name="moveInput">摇杆输入值</param>
    void CalculateMovement(Vector2 moveInput)
    {
        if (head == null)
            return;

        moveDirection = Vector3.zero;

        // 如果有摇杆输入
        if (moveInput.magnitude > 0.1f)
        {
            // 获取手柄的朝向，但只考虑水平方向的旋转
            Vector3 handForward = head.forward;
            Vector3 handRight = head.right;

            // 将Y轴归零，确保只在水平面上移动
            handForward.y = 0;
            handRight.y = 0;

            // 归一化方向向量
            handForward = handForward.normalized;
            handRight = handRight.normalized;

            // 根据摇杆输入和头部朝向计算移动方向
            moveDirection = handForward * moveInput.y + handRight * moveInput.x;
        }
    }

    /// <summary>
    /// 处理移动逻辑，参考PlayerMove.cs的实现
    /// </summary>
    void HandleMovement()
    {
        if (thisRb == null)
            return;

        // 检查是否有摇杆输入
        bool hasInput = moveDirection.magnitude > 0.1f;

        if (IsGrounded() && !hasInput)
        {
            // 如果在地面上且没有摇杆输入，让水平方向速度渐渐归零
            Vector3 currentVelocity = thisRb.linearVelocity;
            Vector3 targetVelocity = new Vector3(0, currentVelocity.y, 0); // 只保留y轴速度

            // 使用Lerp逐渐减速
            thisRb.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, decelerationSpeed * Time.fixedDeltaTime);
        }
        else if (moveDirection != Vector3.zero && currentState != MovementState.Dashing && currentState != MovementState.WallSliding && currentState != MovementState.HookDashing && IsGrounded())
        {
            // 归一化移动方向并应用速度到刚体，只控制x和z轴，保持y轴速度不变
            Vector3 normalizedDirection = moveDirection.normalized;

            // 计算实际移动速度：基础速度 + 额外速度（只有在需要移动时才添加额外速度）
            float actualMoveSpeed = moveSpeed;
            if (moveSpeed > 0 && AddMoveSpeed > 0)
            {
                actualMoveSpeed += AddMoveSpeed;
            }

            Vector3 horizontalVelocity = new Vector3(normalizedDirection.x * actualMoveSpeed, thisRb.linearVelocity.y, normalizedDirection.z * actualMoveSpeed);
            thisRb.linearVelocity = horizontalVelocity;
        }
        else if (currentState == MovementState.WallSliding)
        {
            // 贴墙滑行状态：按照投影向量方向自动滑行，摇杆输入不起作用，Y轴速度为0
            // 计算实际移动速度：基础速度 + 额外速度（只有在需要移动时才添加额外速度）
            float actualMoveSpeed = moveSpeed;
            if (moveSpeed > 0 && AddMoveSpeed > 0)
            {
                actualMoveSpeed += AddMoveSpeed;
            }

            Vector3 wallSlideVelocity = new Vector3(wallSlideDirection.x * actualMoveSpeed * 1.2f, 0, wallSlideDirection.z * actualMoveSpeed * 1.2f);
            thisRb.linearVelocity = wallSlideVelocity;
        }
    }
    #endregion

    #region 地面检测
    /// <summary>
    /// 检测是否在地面上
    /// </summary>
    void CheckGrounded()
    {
        if (body == null)
            return;

        // 使用CapsuleCast进行有厚度的地面检测，参考PlayerMove.cs的实现
        // 胶囊体参数：底部点、顶部点、半径
        Vector3 bottomPoint = body.position + Vector3.down * 0.1f; // 稍微向下偏移
        Vector3 topPoint = body.position + Vector3.up * 0.1f;      // 稍微向上偏移
        float capsuleRadius = 0.6f; // 胶囊体半径，可以根据需要调整
        RaycastHit hit;

        // 绘制检测区域（红色表示未命中，绿色表示命中）
        Color rayColor = Color.red;
        bool hitGround = false;

        // 使用CapsuleCast进行体积检测
        if (Physics.CapsuleCast(bottomPoint, topPoint, capsuleRadius, Vector3.down, out hit,
                               raycastDistance, groundLayerMask))
        {
            rayColor = Color.green;
            hitGround = true;
        }

        // 更新状态（冲刺状态和hook冲刺状态下不更新地面状态）
        if (currentState != MovementState.Dashing && currentState != MovementState.HookDashing)
        {
            if (hitGround && currentState == MovementState.Falling)
            {
                currentState = MovementState.Grounded;
            }
            else if (!hitGround && currentState == MovementState.Grounded)
            {
                currentState = MovementState.Falling;
            }
        }

        // 在Scene视图中绘制CapsuleCast的检测区域
        CapsuleWireframeDrawer.DrawCapsuleCastGizmo(bottomPoint, topPoint, capsuleRadius, Vector3.down * raycastDistance, rayColor);
    }

    /// <summary>
    /// 检查是否在地面上
    /// </summary>
    /// <returns>是否在地面上</returns>
    private bool IsGrounded()
    {
        return currentState == MovementState.Grounded;
    }
    #endregion

    #region 跳跃相关方法
    /// <summary>
    /// 处理跳跃输入
    /// </summary>
    void HandleJump()
    {
        // 检测跳跃输入（使用右手柄的SecondaryButton，通常是A键或B键）
        bool jumpInput = InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.IsPressed();

        if (jumpInput && CanJump())
        {
            PerformJump();
        }
    }

    /// <summary>
    /// 检查是否可以跳跃
    /// </summary>
    /// <returns>是否可以跳跃</returns>
    private bool CanJump()
    {
        return (IsGrounded() || currentState == MovementState.WallSliding) &&
               currentState != MovementState.Dashing &&
               currentState != MovementState.HookDashing;
    }

    /// <summary>
    /// 执行跳跃
    /// </summary>
    private void PerformJump()
    {
        if (thisRb == null)
            return;

        // 如果当前是贴墙滑行状态，先退出滑行状态并添加横向速度
        if (currentState == MovementState.WallSliding)
        {
            // 使用头部的前方方向作为速度方向，保持速度大小不变
            // 计算实际移动速度：基础速度 + 额外速度（只有在需要移动时才添加额外速度）
            float actualMoveSpeed = moveSpeed;
            if (moveSpeed > 0 && AddMoveSpeed > 0)
            {
                actualMoveSpeed += AddMoveSpeed;
            }

            Vector3 horizontalVelocity = head.forward.normalized * actualMoveSpeed;

            // 给刚体一个向上的速度和横向速度来实现贴墙跳跃
            thisRb.linearVelocity = new Vector3(horizontalVelocity.x, jumpForce, horizontalVelocity.z);

            //log
            CustomLog.Log(needLog, "执行贴墙跳跃  并退出贴墙滑行状态");
            ExitWallSliding();
        }
        else
        {
            // 普通跳跃：给刚体一个向上的速度来实现跳跃
            Vector3 currentVelocity = thisRb.linearVelocity;
            thisRb.linearVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
        }

        // 更新状态为跳跃
        currentState = MovementState.Jumping;
    }
    #endregion

    #region 冲刺相关方法
    /// <summary>
    /// 处理冲刺输入
    /// </summary>
    void HandleDash()
    {
        // 更新冷却计时器
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // 检测冲刺输入（使用右手柄的PrimaryButton，通常是A键或B键）
        bool dashInput = InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.IsPressed();

        if (dashInput && CanDash())
        {
            StartDash();
        }

        // 更新冲刺计时器
        if (currentState == MovementState.Dashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
    }

    private bool CanDash()
    {
        return dashCooldownTimer <= 0f &&
               currentState != MovementState.Dashing;
    }


    void StartDash()
    {
        StartDash(Vector3.zero);
    }

    void StartDash(Vector3 customDirection)
    {
        currentState = MovementState.Dashing;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // 如果提供了自定义方向，使用自定义方向
        if (customDirection != Vector3.zero)
        {
            dashDirection = customDirection.normalized;
        }
        // 如果当前没有移动方向，则使用头部的前方作为冲刺方向
        else if (moveDirection != Vector3.zero)
        {
            dashDirection = moveDirection.normalized;
        }
        else
        {
            // 使用头部的前方作为默认冲刺方向
            if (head != null)
            {
                Vector3 headForward = head.forward;
                headForward.y = 0; // 确保只在水平面上冲刺
                dashDirection = headForward.normalized;
            }
            else
            {
                dashDirection = transform.forward;
            }
        }
    }

    void EndDash()
    {
        // 冲刺结束时速度设置为头部朝向的移动速度
        if (thisRb != null)
        {
            Vector3 headForward = head.forward;
            headForward.y = 0; // 确保只在水平面上
            headForward = headForward.normalized;
            // 计算实际移动速度：基础速度 + 额外速度（只有在需要移动时才添加额外速度）
            float actualMoveSpeed = moveSpeed;
            if (moveSpeed > 0 && AddMoveSpeed > 0)
            {
                actualMoveSpeed += AddMoveSpeed;
            }

            thisRb.linearVelocity = headForward * actualMoveSpeed;
        }

        if (IsGrounded())
            currentState = MovementState.Grounded;
        else
            currentState = MovementState.Falling;
    }

    void HandleDashMovement()
    {
        if (currentState == MovementState.Dashing && thisRb != null)
        {
            // 应用冲刺速度，只控制x和z轴，保持y轴速度不变
            Vector3 dashVelocity = new Vector3(dashDirection.x * dashSpeed, thisRb.linearVelocity.y, dashDirection.z * dashSpeed);
            thisRb.linearVelocity = dashVelocity;
        }
    }
    #endregion

    #region 转向相关方法
    /// <summary>
    /// 处理转向输入 - 右摇杆控制转向
    /// </summary>
    void HandleRotation()
    {
        // 读取右摇杆的输入
        Vector2 rotationInput = InputActionsManager.Actions.XRIRightLocomotion.Move.ReadValue<Vector2>();
        float x = rotationInput.x;

        // 需要回到中立区后才允许再次触发，避免抖动导致的反向触发
        if (Mathf.Abs(x) < rotationDeadzone)
        {
            rotationArmed = true;
        }

        if (rotationArmed)
        {
            if (x >= 0.5f)
            {
                RotateAroundHead(rotationAngle);
                rotationArmed = false;
            }
            else if (x <= -0.5f)
            {
                RotateAroundHead(-rotationAngle);
                rotationArmed = false;
            }
        }
    }

    /// <summary>
    /// 以head为中心旋转this.transform
    /// </summary>
    /// <param name="angle">旋转角度（正数为顺时针，负数为逆时针）</param>
    void RotateAroundHead(float angle)
    {
        if (head == null || thisRb == null)
            return;

        // 获取body的世界坐标位置
        Vector3 bodyPosition = body.position;

        // 计算旋转前后的位置和旋转差异
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // 计算从当前位置到head的向量
        Vector3 directionToHead = currentPosition - bodyPosition;

        // 创建旋转四元数（绕Y轴旋转）
        Quaternion rotation = Quaternion.Euler(0, angle, 0);

        // 计算旋转后的新位置
        Vector3 newDirection = rotation * directionToHead;
        Vector3 newPosition = bodyPosition + newDirection;

        // 计算新的旋转
        Quaternion newRotation = rotation * currentRotation;

        // 使用 Rigidbody.MovePosition 和 MoveRotation 来移动和旋转
        // 这样不会和物理引擎冲突
        thisRb.MovePosition(newPosition);
        thisRb.MoveRotation(newRotation);
    }
    #endregion

    #region 状态更新
    /// <summary>
    /// 更新状态
    /// </summary>
    void UpdateState()
    {
        // 从跳跃状态回到下落状态
        if (currentState == MovementState.Jumping && thisRb.linearVelocity.y <= 0)
        {
            currentState = MovementState.Falling;
        }
    }
    #endregion

    #region 额外重力方法
    void ApplyExtraGravity()
    {
        // 应用额外重力（贴墙状态、冲刺状态和hook冲刺状态下不应用重力）
        if (thisRb != null && currentState != MovementState.Dashing && currentState != MovementState.WallSliding && currentState != MovementState.HookDashing)
        {
            thisRb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }
    #endregion

    #region hook冲刺方法
    void HandleHookDash()
    {
        // 检测hook冲刺输入（使用左摇杆的按下事件）
        bool hookDashInput = InputActionsManager.Actions.XRILeftInteraction.ScaleToggle.IsPressed();

        if (hookDashInput && CanHookDash())
        {
            StartHookDash();
        }

        // 更新hook冲刺计时器
        if (currentState == MovementState.HookDashing)
        {
            hookDashTimer -= Time.deltaTime;
            if (hookDashTimer <= 0f)
            {
                EndHookDash();
            }
        }
    }


    private bool CanHookDash()
    {
        // 检查GameManager中是否有ClosestAngleHook，并且当前不在hook冲刺状态
        return GameManager.Instance.ClosestAngleHook != null &&
               currentState != MovementState.HookDashing;
    }

    void StartHookDash()
    {
        currentState = MovementState.HookDashing;
        hookDashTimer = hookDashDuration;
        hookTarget = GameManager.Instance.ClosestAngleHook;

        // 计算冲向hook的方向
        if (hookTarget != null)
        {
            hookDashDirection = (hookTarget.position - transform.position).normalized;
        }
    }

    void EndHookDash()
    {
        // 到达hook位置后进入浮空状态，并归零速度
        currentState = MovementState.Falling;
        if (thisRb != null)
        {
            thisRb.linearVelocity = thisRb.linearVelocity.normalized * 10f;
        }
    }

    void HandleHookDashMovement()
    {
        if (currentState == MovementState.HookDashing && thisRb != null && hookTarget != null)
        {
            Debug.Log("Hook冲刺中");
            // 在FixedUpdate中根据距离调整移动，避免高速下直接越过目标
            float distanceToHook = Vector3.Distance(transform.position, hookTarget.position);
            float step = dashSpeed * Time.fixedDeltaTime;
            // 如果本次步进会到达或超过目标位置，则直接移动到目标并结束hook冲刺，避免穿透或跳过
            if (distanceToHook <= step)
            {
                // 精确移动到目标位置（FixedUpdate 中使用 MovePosition 更安全）
                thisRb.MovePosition(hookTarget.position);
                EndHookDash();
            }
            else
            {
                // 以冲刺速度冲向hook目标位置（使用立体的实际方向，包含Y轴分量）
                Vector3 hookDashVelocity = hookDashDirection * dashSpeed;
                thisRb.linearVelocity = hookDashVelocity;
            }
        }
    }
    #endregion

    #region 贴墙滑行方法
    void HandleWallSliding()
    {
        if (currentState == MovementState.WallSliding)
        {
            // 更新贴墙计时器
            wallSlideTimer += Time.deltaTime;

            // 持续检测是否还贴在墙上
            CheckWallAttachment();

            // 贴墙滑行时的特殊逻辑（例如减速等）
            // HandleWallSlideMovement(); // 移动到FixedUpdate中处理
        }
    }

    void EnterWallSliding(Vector3 normal)
    {
        currentState = MovementState.WallSliding;
        wallNormal = normal;
        wallSlideTimer = 0f;
        vRBody.StopFollow();


        // 禁用刚体重力
        if (thisRb != null)
        {
            thisRb.useGravity = false;
        }

        CustomLog.Log(needLog, "进入贴墙滑行状态");
        
        // 触发进入贴墙滑行事件，传递墙面法线
        OnEnterWallSliding?.Invoke(normal);
    }

    void ExitWallSliding()
    {
        if (currentState == MovementState.WallSliding)
        {
            vRBody.StartFollow();


            // 重新启用刚体重力
            if (thisRb != null)
            {
                thisRb.useGravity = true;
            }

            // 根据当前是否在地面来决定下一个状态
            if (IsGrounded())
                currentState = MovementState.Grounded;
            else
                currentState = MovementState.Falling;

            CustomLog.Log(needLog, "退出贴墙滑行状态");
            
            // 触发退出贴墙滑行事件，传回当前记录的墙面法线
            OnExitWallSliding?.Invoke(wallNormal);
        }
    }

    void CheckWallAttachment()
    {
        if (body == null)
            return;

        // 从body位置向墙面法线的反方向发射黄色射线
        Vector3 rayDirection = -wallNormal;
        Ray ray = new Ray(body.position, rayDirection);

        // 绘制黄色射线
        Color rayColor = Color.yellow;
        // 使用RaycastAll检测所有碰撞，然后过滤出Wall tag的物体
        RaycastHit[] hits = Physics.RaycastAll(ray, 6);
        bool stillAttached = false;

        foreach (RaycastHit hitInfo in hits)
        {
            if (hitInfo.collider.CompareTag("Wall"))
            {
                stillAttached = true;
                // CustomLog.Log(needLog, "（黄色射线）贴墙滑行时检测到墙体");
                break;
            }
        }

        if (!stillAttached)
        {
            CustomLog.Log(needLog, "贴墙滑行时未检测到墙体，退出贴墙状态");
            // 如果射线没有检测到墙体，退出贴墙状态
            ExitWallSliding();
        }

        // 在Scene视图中绘制射线，持续0.5秒
        Debug.DrawRay(body.position, rayDirection * raycastDistance, rayColor, 10f);
    }

    void HandleWallSlideMovement()
    {
        if (thisRb == null)
            return;

        // 贴墙滑行状态：按照投影向量方向自动滑行，Y轴速度为0
        // 计算实际移动速度：基础速度 + 额外速度（只有在需要移动时才添加额外速度）
        float actualMoveSpeed = moveSpeed;
        if (moveSpeed > 0 && AddMoveSpeed > 0)
        {
            actualMoveSpeed += AddMoveSpeed;
        }

        Vector3 wallSlideVelocity = new Vector3(wallSlideDirection.x * actualMoveSpeed * 1.2f, 0, wallSlideDirection.z * actualMoveSpeed * 1.2f);
        thisRb.linearVelocity = wallSlideVelocity;
    }
    #endregion

    #region 碰撞检测
    void OnCollisionEnter(Collision collision)
    {
        // 检测碰撞物体是否为Wall
        if (collision.gameObject.CompareTag("Wall"))
        {
            CustomLog.Log(needLog, "检测到墙体碰撞");

            // 获取第一个接触点的法线
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal;

                // 输出法线信息
                CustomLog.Log(needLog, $"碰撞点法线: {normal}");

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

                    // 输出投影向量信息
                    CustomLog.Log(needLog, $"速度在法线平面上的投影向量 (Y轴归零, 长度1): {horizontalProjection}");

                    // 从碰撞点绘制投影向量（绿色）
                    Debug.DrawRay(contact.point, horizontalProjection, Color.green, 2f);

                    // 检查投影向量是否为垂直方向（没有水平分量）
                    if (horizontalProjection != Vector3.zero && !IsGrounded())
                    {
                        // 保存投影向量用于贴墙滑行
                        wallSlideDirection = horizontalProjection;

                        // 进入贴墙滑行状态
                        EnterWallSliding(normal);
                    }
                    else
                    {
                        CustomLog.Log(needLog, "投影向量为垂直方向，不进入滑行状态");
                    }
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // 离开墙体时退出贴墙滑行状态
        if (collision.gameObject.CompareTag("Wall") && currentState == MovementState.WallSliding)
        {
            //log 
            CustomLog.Log(needLog, "离开墙体，退出贴墙滑行状态");
            ExitWallSliding();
        }
    }


    #endregion

    #region 手部移动冲刺方法
    /// <summary>
    /// 检测手部移动并触发冲刺
    /// </summary>
    void HandleHandMoveDash()
    {
        if (rightHandTarget == null)
            return;

        // 检测右手柄扳机键是否被按下
        bool triggerPressed = InputActionsManager.Actions.XRIRightInteraction.Activate.IsPressed();

        // 如果扳机键没有被按下，则不进行冲刺检测
        if (!triggerPressed)
        {
            // 更新上一帧的位置（重置位置跟踪）
            previousHandLocalPosition = rightHandTarget.localPosition;
            return;
        }

        // 计算当前帧的本地位置
        Vector3 currentLocalPosition = rightHandTarget.localPosition;

        // 计算移动速度（每帧移动的距离）
        float moveDistance = Vector3.Distance(currentLocalPosition, previousHandLocalPosition);
        float speed = moveDistance / Time.deltaTime;

        // 检查速度是否超过阈值
        if (speed > handMoveSpeedThreshold)
        {
            Debug.Log("手部移动速度超过阈值且扳机键被按下，触发冲刺");
            TriggerDash();
        }

        // 更新上一帧的位置
        previousHandLocalPosition = currentLocalPosition;
    }
    #endregion

    #region 接口实现
    /// <summary>
    /// 获取玩家头部Transform
    /// </summary>
    /// <returns>头部Transform</returns>
    public Transform GetPlayerHead()
    {
        return head;
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

    /// <summary>
    /// 外部调用处理hook冲刺
    /// </summary>
    public void OutHandleHookDash()
    {
        // 检测hook冲刺输入（使用左摇杆的按下事件）
        if (CanHookDash())
        {
            StartHookDash();
        }

        // 更新hook冲刺计时器
        if (currentState == MovementState.HookDashing)
        {
            hookDashTimer -= Time.deltaTime;
            if (hookDashTimer <= 0f)
            {
                EndHookDash();
            }
        }
    }

    /// <summary>
    /// 设置额外的移动速度加成
    /// </summary>
    /// <param name="additionalSpeed">要添加的额外速度</param>
    public void SetAdditionalMoveSpeed(float additionalSpeed)
    {
        AddMoveSpeed = additionalSpeed * AddMoveSpeedMultiplier;
    }

    /// <summary>
    /// 获取当前的实际移动速度（基础速度 + 额外速度）
    /// </summary>
    /// <returns>实际移动速度</returns>
    public float GetActualMoveSpeed()
    {
        return moveSpeed + AddMoveSpeed;
    }

    // events declared in the interface-compatible form earlier; remove older parameterless declarations
    // (kept for compatibility with IWallSlidingProvider as Action<Vector3>)
    





    #endregion
}
