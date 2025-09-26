using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    #region 移动设置
    [Header("移动设置")]
    public Transform forwardTarget;
    public float moveSpeed = 5f;
    public float decelerationSpeed = 5f; // 归零速度
    public float raycastDistance = 10f; // 射线检测距离
    public float extraGravity = 10f; // 额外重力
    #endregion

    #region 跳跃设置
    [Header("跳跃设置")]
    public float jumpForce = 10f; // 跳跃力度
    #endregion

    #region 冲刺设置
    [Header("冲刺设置")]
    public float dashSpeed = 15f; // 冲刺速度
    public float dashDuration = 0.3f; // 冲刺持续时间
    public float dashCooldown = 1f; // 冲刺冷却时间
    #endregion

    #region 私有字段
    private Rigidbody thisRb;
    private Vector3 moveDirection;
    private int buildingLayerMask;
    
    // 状态管理
    private enum MovementState { Grounded, Jumping, Dashing, Falling }
    private MovementState currentState = MovementState.Grounded;
    
    // 冲刺相关
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;
    #endregion

    #region Unity生命周期
    void Start()
    {
        thisRb = GetComponent<Rigidbody>();
        buildingLayerMask = LayerMask.GetMask("Building");
    }

    void Update()
    {
        CalculateMovement();
        CheckBuildingBelow();
        HandleJump();
        HandleDash();
        HandleAttack();
        UpdateState();
    }
    
    void FixedUpdate()
    {
        HandleMovement();
        HandleDashMovement();
        ApplyExtraGravity();
    }
    #endregion

    #region 移动方法
    void CalculateMovement()
    {
        if (forwardTarget == null)
            return;
            
        moveDirection = Vector3.zero;
        
        // 基于 forwardTarget 的方向计算移动
        if (Input.GetKey(KeyCode.W))
            moveDirection += forwardTarget.forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection -= forwardTarget.forward;
        if (Input.GetKey(KeyCode.A))
            moveDirection -= forwardTarget.right;
        if (Input.GetKey(KeyCode.D))
            moveDirection += forwardTarget.right;
    }
    
    void HandleMovement()
    {
        if (thisRb == null)
            return;
            
        // 检查是否有WASD按键按下
        bool isWASDPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        
        if (IsGrounded() && !isWASDPressed)
        {
            // 如果在地面上且没有WASD按键按下，让x轴和z轴速度渐渐归零
            Vector3 currentVelocity = thisRb.linearVelocity;
            Vector3 targetVelocity = new Vector3(0, currentVelocity.y, 0); // 只保留y轴速度
            
            // 使用Lerp逐渐减速
            thisRb.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, decelerationSpeed * Time.fixedDeltaTime);
        }
        else if (moveDirection != Vector3.zero && currentState != MovementState.Dashing)
        {
            // 归一化并应用速度到刚体，只控制x和z轴，保持y轴速度不变
            Vector3 normalizedDirection = moveDirection.normalized;
            Vector3 horizontalVelocity = new Vector3(normalizedDirection.x * moveSpeed, thisRb.linearVelocity.y, normalizedDirection.z * moveSpeed);
            thisRb.linearVelocity = horizontalVelocity;
        }
    }
    #endregion

    #region 建筑检测
    void CheckBuildingBelow()
    {
        if (forwardTarget == null)
            return;
            
        // 从 forwardTarget 位置向下发射射线
        Ray ray = new Ray(forwardTarget.position, Vector3.down);
        RaycastHit hit;
        
        // 绘制射线（红色表示未命中，绿色表示命中）
        Color rayColor = Color.red;
        bool hitBuilding = false;
        
        if (Physics.Raycast(ray, out hit, raycastDistance, buildingLayerMask))
        {
            rayColor = Color.green;
            hitBuilding = true;
        }
        
        // 更新状态（冲刺状态下不更新地面状态）
        if (currentState != MovementState.Dashing)
        {
            if (hitBuilding)
                currentState = MovementState.Grounded;
            else if (currentState == MovementState.Grounded)
                currentState = MovementState.Falling;
        }
        
        // 在Scene视图中绘制射线
        Debug.DrawRay(forwardTarget.position, Vector3.down * raycastDistance, rayColor);
    }
    
    public bool IsHittingBuilding()
    {
        return currentState == MovementState.Grounded;
    }
    
    private bool IsGrounded()
    {
        return currentState == MovementState.Grounded;
    }
    #endregion

    #region 跳跃方法
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && thisRb != null && CanJump())
        {
            PerformJump();
        }
    }
    
    private bool CanJump()
    {
        return IsGrounded() && currentState != MovementState.Dashing;
    }
    
    private void PerformJump()
    {
        // 给刚体一个向上的速度来实现跳跃
        Vector3 currentVelocity = thisRb.linearVelocity;
        thisRb.linearVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
        currentState = MovementState.Jumping;
    }
    #endregion

    #region 冲刺方法
    void HandleDash()
    {
        // 更新冷却计时器
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // 检测冲刺输入（左Shift键）
        if (Input.GetKeyDown(KeyCode.LeftShift) && CanDash())
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
               currentState != MovementState.Dashing &&
               moveDirection != Vector3.zero;
    }

    void StartDash()
    {
        currentState = MovementState.Dashing;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = moveDirection.normalized;
    }

    void EndDash()
    {
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

    #region 攻击方法
    void HandleAttack()
    {
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            PerformAttack();
        }
    }
    
    private void PerformAttack()
    {
        // 执行攻击动作，这里简单记录日志
        Debug.Log("开始攻击");
    }
    #endregion

    #region 工具方法
    void UpdateState()
    {
        // 从跳跃状态回到地面状态
        if (currentState == MovementState.Jumping && thisRb.linearVelocity.y <= 0)
        {
            currentState = MovementState.Falling;
        }
    }
    
    void ApplyExtraGravity()
    {
        // 应用额外重力
        if (thisRb != null && currentState != MovementState.Dashing)
        {
            thisRb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }
    #endregion
}
