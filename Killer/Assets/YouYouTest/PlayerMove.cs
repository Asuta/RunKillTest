using UnityEngine;
using System.Collections;

public class PlayerMove : MonoBehaviour
{
    #region 移动设置
    [Header("移动设置")]
    public Transform forwardTarget;
    public float moveSpeed = 5f;
    public float decelerationSpeed = 5f; // 归零速度
    public float raycastDistance = 10f; // 射线检测距离
    public float extraGravity = 10f; // 额外重力
    public Vector3 nowVelocity; // 当前速度（仅用于调试显示）
    public string nowState; // 当前状态（仅用于调试显示）
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

    #region 攻击设置
    [Header("攻击设置")]
    public Transform hitBoxTrigger; // 攻击触发器
    public int attackDamage = 10; // 攻击伤害值
    #endregion

    #region 私有字段
    private Rigidbody thisRb;
    private Vector3 moveDirection;
    private int buildingLayerMask;

    // 状态管理
    private enum MovementState { Grounded, Jumping, Dashing, Falling, WallSliding }
    private MovementState currentState = MovementState.Grounded;

    // 冲刺相关
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    // 贴墙滑行相关
    private Vector3 wallNormal; // 存储墙面法线
    private float wallSlideTimer = 0f; // 贴墙计时器
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
        HandleWallSliding();
        
        // 更新当前状态用于调试显示
        UpdateNowState();
        
        // 绘制当前速度向量（Scene视图可见）
        DrawVelocityVector();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleDashMovement();
        ApplyExtraGravity();
        
        // 更新当前速度用于调试显示
        if (thisRb != null)
        {
            nowVelocity = thisRb.linearVelocity;
        }
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
        
        // 检查是否有攻击触发器
        if (hitBoxTrigger == null)
        {
            Debug.LogWarning("hitBoxTrigger 未设置，无法进行攻击检测");
            return;
        }
        
        // 显示攻击触发器0.1秒
        StartCoroutine(ShowHitBoxTemporarily());
        
        // 获取攻击触发器的位置和尺寸
        Vector3 center = hitBoxTrigger.position;
        Vector3 halfExtents = hitBoxTrigger.lossyScale / 2f;
        
        // 进行Box范围检测，查找tag为"enemy"的物体
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, hitBoxTrigger.rotation);
        
        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Debug.Log("击中！");
                // 对敌人造成伤害
                ICanBeHit enemy = collider.GetComponent<ICanBeHit>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                }
                else
                {
                    Debug.LogWarning("敌人没有实现 ICanBeHit 接口，无法造成伤害");
                }
            }
        }
    }
    
    private IEnumerator ShowHitBoxTemporarily()
    {
        // 获取MeshRenderer组件
        MeshRenderer meshRenderer = hitBoxTrigger.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // 开启MeshRenderer
            meshRenderer.enabled = true;
            
            // 等待0.1秒
            yield return new WaitForSeconds(0.1f);
            
            // 关闭MeshRenderer
            meshRenderer.enabled = false;
        }
    }
    #endregion

    #region 碰撞检测
    void OnCollisionEnter(Collision collision)
    {
        // 检测碰撞物体是否为Wall
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log("hahaha");
            
            // 获取第一个接触点的法线
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal;
                
                // 输出法线信息
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
                    
                    // 输出投影向量信息
                    Debug.Log($"速度在法线平面上的投影向量 (Y轴归零, 长度1): {horizontalProjection}");
                    
                    // 从碰撞点绘制投影向量（绿色）
                    Debug.DrawRay(contact.point, horizontalProjection, Color.green, 2f);
                }

                // 进入贴墙滑行状态
                EnterWallSliding(normal);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // 离开墙体时退出贴墙滑行状态
        if (collision.gameObject.CompareTag("Wall") && currentState == MovementState.WallSliding)
        {
            ExitWallSliding();
        }
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
    
    void DrawVelocityVector()
    {
        // 从body当前位置绘制速度向量
        Debug.DrawRay(forwardTarget.position, nowVelocity, Color.blue);
    }
    void UpdateNowState()
    {
        // 将枚举状态转换为字符串用于调试显示
        nowState = currentState.ToString();
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
            HandleWallSlideMovement();
        }
    }

    void EnterWallSliding(Vector3 normal)
    {
        currentState = MovementState.WallSliding;
        wallNormal = normal;
        wallSlideTimer = 0f;
        Debug.Log("进入贴墙滑行状态");
    }

    void ExitWallSliding()
    {
        if (currentState == MovementState.WallSliding)
        {
            // 根据当前是否在地面来决定下一个状态
            if (IsGrounded())
                currentState = MovementState.Grounded;
            else
                currentState = MovementState.Falling;
            
            Debug.Log("退出贴墙滑行状态");
        }
    }

    void CheckWallAttachment()
    {
        if (forwardTarget == null)
            return;

        // 从forwardTarget位置向墙面法线的反方向发射黄色射线
        Vector3 rayDirection = -wallNormal;
        Ray ray = new Ray(forwardTarget.position, rayDirection);
        RaycastHit hit;

        // 绘制黄色射线
        Color rayColor = Color.yellow;
        // 使用RaycastAll检测所有碰撞，然后过滤出Wall tag的物体
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);
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
            Debug.LogError("贴墙滑行时未检测到墙体，退出贴墙状态"); 
            // 如果射线没有检测到墙体，退出贴墙状态
            ExitWallSliding();
        }
        else
        {
            // 如果检测到墙体，更新墙面法线
            Debug.LogError("-------------"); 
        }

        // 在Scene视图中绘制射线，持续0.5秒
        Debug.DrawRay(forwardTarget.position, rayDirection * raycastDistance, rayColor, 0.5f);
    }

    void HandleWallSlideMovement()
    {
        if (thisRb == null)
            return;

        // 贴墙滑行时的移动逻辑
        // 可以在这里添加减速或其他特殊移动效果
        Vector3 currentVelocity = thisRb.linearVelocity;
        
        // 示例：在墙上时减少水平速度
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x * 0.8f, currentVelocity.y, currentVelocity.z * 0.8f);
        thisRb.linearVelocity = horizontalVelocity;
    }
    #endregion
}
