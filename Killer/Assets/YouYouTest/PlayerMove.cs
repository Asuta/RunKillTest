using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public Transform forwardTarget;
    private Rigidbody thisRb;
    public float moveSpeed = 5f;
    public float raycastDistance = 10f; // 射线检测距离
    public float decelerationSpeed = 5f; // 归零速度
    public float jumpForce = 10f; // 跳跃力度
    public float dashSpeed = 15f; // 冲刺速度
    public float dashDuration = 0.3f; // 冲刺持续时间
    public float dashCooldown = 1f; // 冲刺冷却时间
      
    private bool isHittingBuilding = false; // 记录是否射中building层
    private bool isDashing = false; // 是否正在冲刺
    private float dashTimer = 0f; // 冲刺计时器
    private float dashCooldownTimer = 0f; // 冲刺冷却计时器
    private Vector3 dashDirection; // 冲刺方向

    public float extraGravity = 10f; // 额外重力
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisRb = GetComponent<Rigidbody>();
    }

    private Vector3 moveDirection;
    
    // Update is called once per frame
    void Update()
    {
        CalculateMovement();
        CheckBuildingBelow();
        HandleJump();
        HandleDash();
    }
    
    void FixedUpdate()
    {
        HandleMovement();
        HandleDashMovement();
        // 应用额外重力
        if (thisRb != null && !isDashing)
        {
            thisRb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }
    
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
        
        if (isHittingBuilding && !isWASDPressed)
        {
            // 如果射中building层且没有WASD按键按下，让x轴和z轴速度渐渐归零
            Vector3 currentVelocity = thisRb.linearVelocity;
            Vector3 targetVelocity = new Vector3(0, currentVelocity.y, 0); // 只保留y轴速度
            
            // 使用Lerp逐渐减速
            thisRb.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, decelerationSpeed * Time.fixedDeltaTime);
        } 
        else if (moveDirection != Vector3.zero)
        {
            // 归一化并应用速度到刚体，只控制x和z轴，保持y轴速度不变
            Vector3 normalizedDirection = moveDirection.normalized;
            Vector3 horizontalVelocity = new Vector3(normalizedDirection.x * moveSpeed, thisRb.linearVelocity.y, normalizedDirection.z * moveSpeed);
            thisRb.linearVelocity = horizontalVelocity;
        }
    }
    
    void CheckBuildingBelow()
    {
        if (forwardTarget == null)
            return;
            
        // 从 forwardTarget 位置向下发射射线
        Ray ray = new Ray(forwardTarget.position, Vector3.down);
        RaycastHit hit;
        
        // 检测 "building" 层
        int buildingLayer = LayerMask.GetMask("Building");
        
        // 绘制射线（红色表示未命中，绿色表示命中）
        Color rayColor = Color.red;
        bool hitBuilding = false;
        
        if (Physics.Raycast(ray, out hit, raycastDistance, buildingLayer))
        {
            rayColor = Color.green;
            hitBuilding = true;
        }
        
        // 更新射中状态
        isHittingBuilding = hitBuilding;
        
        // 在Scene视图中绘制射线
        Debug.DrawRay(forwardTarget.position, Vector3.down * raycastDistance, rayColor);
    }
    
    // 获取当前是否射中building层的状态
    public bool IsHittingBuilding()
    {
        return isHittingBuilding;
    }
    
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && thisRb != null && !isDashing)
        {
            // 给刚体一个向上的速度来实现跳跃
            Vector3 currentVelocity = thisRb.linearVelocity;
            thisRb.linearVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
        }
    }

    void HandleDash()
    {
        // 更新冷却计时器
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // 检测冲刺输入（左Shift键）
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && !isDashing && moveDirection != Vector3.zero)
        {
            StartDash();
        }

        // 更新冲刺计时器
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = moveDirection.normalized;
    }

    void EndDash()
    {
        isDashing = false;
    }

    void HandleDashMovement()
    {
        if (isDashing && thisRb != null)
        {
            // 应用冲刺速度，只控制x和z轴，保持y轴速度不变
            Vector3 dashVelocity = new Vector3(dashDirection.x * dashSpeed, thisRb.linearVelocity.y, dashDirection.z * dashSpeed);
            thisRb.linearVelocity = dashVelocity;
        }
    }
}
