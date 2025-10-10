using UnityEngine;

public class VRPlayer : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;
    public Transform body;
    public Transform head;
    private Rigidbody thisRb;
    
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float decelerationSpeed = 5f; // 减速速度
    public Vector3 moveDirection;
    
    [Header("跳跃设置")]
    public float jumpForce = 8f; // 跳跃力度
    public float raycastDistance = 1.2f; // 地面检测距离
    public LayerMask groundLayerMask; // 地面层级
    
    // 状态管理
    private enum MovementState { Grounded, Jumping, Falling, Dashing }
    private MovementState currentState = MovementState.Grounded;

    [Header("冲刺设置")]
    public float dashSpeed = 15f; // 冲刺速度
    public float dashDuration = 0.3f; // 冲刺持续时间
    public float dashCooldown = 1f; // 冲刺冷却时间
    
    // 冲刺相关
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisRb = GetComponent<Rigidbody>();
        
        // 如果没有设置地面层级，使用默认的Building层级
        if (groundLayerMask == 0)
        {
            groundLayerMask = LayerMask.GetMask("Building");
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
        
        // 更新状态
        UpdateState();
    }
    
    void FixedUpdate()
    {
        // 在FixedUpdate中处理移动，确保物理计算的一致性
        HandleMovement();
        HandleDashMovement();
    }
    
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
            Vector3 handForward = leftHand.forward;
            Vector3 handRight = leftHand.right;
            
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

        if (!hasInput)
        {
            // 如果没有输入，让水平方向速度渐渐归零
            Vector3 currentVelocity = thisRb.linearVelocity;
            Vector3 targetVelocity = new Vector3(0, currentVelocity.y, 0); // 只保留y轴速度

            // 使用Lerp逐渐减速
            thisRb.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, decelerationSpeed * Time.fixedDeltaTime);
        }
        else if (currentState != MovementState.Dashing)
        {
            // 归一化移动方向并应用速度到刚体，只控制x和z轴，保持y轴速度不变
            Vector3 normalizedDirection = moveDirection.normalized;
            Vector3 horizontalVelocity = new Vector3(normalizedDirection.x * moveSpeed, thisRb.linearVelocity.y, normalizedDirection.z * moveSpeed);
            thisRb.linearVelocity = horizontalVelocity;
        }
    }
    
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

        // 更新状态（冲刺状态下不更新地面状态）
        if (currentState != MovementState.Dashing)
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
    /// 处理跳跃输入
    /// </summary>
    void HandleJump()
    {
        // 检测跳跃输入（空格键）
        bool jumpInput = InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.IsPressed();
        
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
        return IsGrounded() && currentState != MovementState.Dashing;
    }
    
    /// <summary>
    /// 执行跳跃
    /// </summary>
    private void PerformJump()
    {
        if (thisRb == null)
            return;
            
        // 给刚体一个向上的速度来实现跳跃
        Vector3 currentVelocity = thisRb.linearVelocity;
        thisRb.linearVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
        
        // 更新状态为跳跃
        currentState = MovementState.Jumping;
    }
    
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
        currentState = MovementState.Dashing;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // 如果当前没有移动方向，则使用左手柄的前方作为冲刺方向
        if (moveDirection != Vector3.zero)
        {
            dashDirection = moveDirection.normalized;
        }
        else
        {
            // 使用左手柄的前方作为默认冲刺方向
            if (leftHand != null)
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
        // 冲刺结束时恢复正常移动
        if (thisRb != null)
        {
            Vector3 currentVelocity = thisRb.linearVelocity;
            thisRb.linearVelocity = new Vector3(currentVelocity.x * 0.5f, currentVelocity.y, currentVelocity.z * 0.5f);
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
    
    /// <summary>
    /// 检查是否在地面上
    /// </summary>
    /// <returns>是否在地面上</returns>
    private bool IsGrounded()
    {
        return currentState == MovementState.Grounded;
    }
}
