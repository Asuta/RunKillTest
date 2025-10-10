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
    private enum MovementState { Grounded, Jumping, Falling }
    private MovementState currentState = MovementState.Grounded;

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
        
        // 更新状态
        UpdateState();
    }
    
    void FixedUpdate()
    {
        // 在FixedUpdate中处理移动，确保物理计算的一致性
        HandleMovement();
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
        else
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
            
        // 使用射线检测地面
        Vector3 origin = body.position;
        Vector3 direction = Vector3.down;
        
        // 在Scene视图中绘制射线，便于调试
        Color rayColor = IsGrounded() ? Color.green : Color.red;
        Debug.DrawRay(origin, direction * raycastDistance, rayColor);
        
        // 进行射线检测
        bool isGrounded = Physics.Raycast(origin, direction, raycastDistance, groundLayerMask);
        
        // 更新状态
        if (isGrounded && currentState == MovementState.Falling)
        {
            currentState = MovementState.Grounded;
        }
        else if (!isGrounded && currentState == MovementState.Grounded)
        {
            currentState = MovementState.Falling;
        }
    }
    
    /// <summary>
    /// 处理跳跃输入
    /// </summary>
    void HandleJump()
    {
        // 检测跳跃输入（空格键）
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        
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
        return IsGrounded();
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
