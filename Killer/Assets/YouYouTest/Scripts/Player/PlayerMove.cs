using UnityEngine;
using System.Collections;

public class PlayerMove : MonoBehaviour
{
    #region 移动设置
    [Header("移动设置")]
    public Transform forwardTarget;
    public Transform playerHead;
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
    public Transform defenseBoxTrigger; // 防御触发器
    public int attackDamage = 10; // 攻击伤害值
    public GameObject hitEffect; // 防御特效
    #endregion

    #region 私有字段
    private Rigidbody thisRb;
    private Vector3 moveDirection;
    private int buildingLayerMask;

    // 状态管理
    private enum MovementState { Grounded, Jumping, Dashing, Falling, WallSliding, HookDashing }
    private MovementState currentState = MovementState.Grounded;

    // 冲刺相关
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    // 贴墙滑行相关
    private Vector3 wallNormal; // 存储墙面法线
    private float wallSlideTimer = 0f; // 贴墙计时器
    private Vector3 wallSlideDirection; // 存储贴墙滑行方向（投影向量）

    // hook冲刺相关
    private Transform hookTarget; // 目标hook的Transform
    private float hookDashTimer = 0f; // hook冲刺计时器
    private float hookDashDuration = 0.3f; // hook冲刺持续时间
    private Vector3 hookDashDirection; // hook冲刺方向

    // log setting
    public bool needLog = false;

    // 防御相关
    private bool isDefending = false;
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
        HandleHookDash(); // 处理hook冲刺
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
        HandleHookDashMovement(); // 处理hook冲刺移动
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
        else if (moveDirection != Vector3.zero && currentState != MovementState.Dashing && currentState != MovementState.WallSliding && currentState != MovementState.HookDashing && IsGrounded())
        {
            // 归一化并应用速度到刚体，只控制x和z轴，保持y轴速度不变
            Vector3 normalizedDirection = moveDirection.normalized;
            Vector3 horizontalVelocity = new Vector3(normalizedDirection.x * moveSpeed, thisRb.linearVelocity.y, normalizedDirection.z * moveSpeed);
            thisRb.linearVelocity = horizontalVelocity;
        }
        else if (currentState == MovementState.WallSliding)
        {
            // 贴墙滑行状态：按照投影向量方向自动滑行，WASD不起作用，Y轴速度为0
            Vector3 wallSlideVelocity = new Vector3(wallSlideDirection.x * moveSpeed * 1.2f, 0, wallSlideDirection.z * moveSpeed * 1.2f);
            thisRb.linearVelocity = wallSlideVelocity;
        }
    }
    #endregion

    #region 地面检测
    void CheckBuildingBelow()
    {
        if (forwardTarget == null)
            return;

        // 使用CapsuleCast进行有厚度的地面检测
        // 胶囊体参数：底部点、顶部点、半径
        Vector3 bottomPoint = forwardTarget.position + Vector3.down * 0.1f; // 稍微向下偏移
        Vector3 topPoint = forwardTarget.position + Vector3.up * 0.1f;      // 稍微向上偏移
        float capsuleRadius = 0.9f; // 胶囊体半径，可以根据需要调整
        RaycastHit hit;

        // 绘制检测区域（红色表示未命中，绿色表示命中）
        Color rayColor = Color.red;
        bool hitBuilding = false;

        // 使用CapsuleCast进行体积检测
        if (Physics.CapsuleCast(bottomPoint, topPoint, capsuleRadius, Vector3.down, out hit,
                               raycastDistance, buildingLayerMask))
        {
            rayColor = Color.green;
            hitBuilding = true;
        }

        // 更新状态（冲刺状态和hook冲刺状态下不更新地面状态）
        if (currentState != MovementState.Dashing && currentState != MovementState.HookDashing)
        {
            if (hitBuilding)
                currentState = MovementState.Grounded;
            else if (currentState == MovementState.Grounded)
                currentState = MovementState.Falling;
        }

        // 在Scene视图中绘制CapsuleCast的检测区域
        DrawCapsuleCastGizmo(bottomPoint, topPoint, capsuleRadius, Vector3.down * raycastDistance, rayColor);
    }

    // 辅助方法：绘制CapsuleCast的可视化区域
    void DrawCapsuleCastGizmo(Vector3 point1, Vector3 point2, float radius, Vector3 direction, Color color)
    {
        // 绘制起始胶囊体
        DrawWireCapsule(point1, point2, radius, color);
        
        // 绘制结束胶囊体
        DrawWireCapsule(point1 + direction, point2 + direction, radius, color);
        
        // 绘制连接线（胶囊体的边缘）
        Vector3 up = (point2 - point1).normalized;
        Vector3 right = Vector3.Cross(up, Vector3.forward).normalized;
        if (right == Vector3.zero) right = Vector3.Cross(up, Vector3.up).normalized;
        Vector3 forward = Vector3.Cross(up, right).normalized;
        
        // 绘制4个方向的连接线
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 offset = right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
            
            Debug.DrawLine(point1 + offset, point1 + direction + offset, color);
            Debug.DrawLine(point2 + offset, point2 + direction + offset, color);
        }
    }

    // 绘制线框胶囊体
    void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius, Color color)
    {
        Vector3 up = (point2 - point1).normalized;
        Vector3 right = Vector3.Cross(up, Vector3.forward).normalized;
        if (right == Vector3.zero) right = Vector3.Cross(up, Vector3.up).normalized;
        Vector3 forward = Vector3.Cross(up, right).normalized;
        
        // 绘制顶部和底部的半球
        DrawWireHemisphere(point1, -up, right, forward, radius, color);
        DrawWireHemisphere(point2, up, right, forward, radius, color);
        
        // 绘制中间的圆柱部分
        int segments = 12;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * 360f / segments * Mathf.Deg2Rad;
            float angle2 = (i + 1) * 360f / segments * Mathf.Deg2Rad;
            
            Vector3 offset1 = right * Mathf.Cos(angle1) * radius + forward * Mathf.Sin(angle1) * radius;
            Vector3 offset2 = right * Mathf.Cos(angle2) * radius + forward * Mathf.Sin(angle2) * radius;
            
            Debug.DrawLine(point1 + offset1, point2 + offset1, color);
            Debug.DrawLine(point1 + offset1, point1 + offset2, color);
            Debug.DrawLine(point2 + offset1, point2 + offset2, color);
        }
    }

    // 绘制线框半球
    void DrawWireHemisphere(Vector3 center, Vector3 normal, Vector3 right, Vector3 forward, float radius, Color color)
    {
        int segments = 12;
        int rings = 3;
        
        for (int ring = 1; ring <= rings; ring++)
        {
            float ringAngle = ring * 90f / rings * Mathf.Deg2Rad;
            float ringRadius = Mathf.Sin(ringAngle) * radius;
            float ringHeight = Mathf.Cos(ringAngle) * radius;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * 360f / segments * Mathf.Deg2Rad;
                float angle2 = (i + 1) * 360f / segments * Mathf.Deg2Rad;
                
                Vector3 point1 = center + normal * ringHeight +
                                right * Mathf.Cos(angle1) * ringRadius +
                                forward * Mathf.Sin(angle1) * ringRadius;
                
                Vector3 point2 = center + normal * ringHeight +
                                right * Mathf.Cos(angle2) * ringRadius +
                                forward * Mathf.Sin(angle2) * ringRadius;
                
                Debug.DrawLine(point1, point2, color);
                
                // 绘制到中心的连接线
                if (i % 3 == 0)
                {
                    Debug.DrawLine(center, point1, color);
                }
            }
        }
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
        return (IsGrounded() || currentState == MovementState.WallSliding) &&
               currentState != MovementState.Dashing &&
               currentState != MovementState.HookDashing; // hook冲刺状态下不能跳跃
    }

    private void PerformJump()
    {
        // 如果当前是贴墙滑行状态，先退出滑行状态并添加横向速度
        if (currentState == MovementState.WallSliding)
        {
            CustomLog.Log(needLog, "跳跃退出滑行");
            
            // 使用forwardTarget的前方方向作为速度方向，保持速度大小不变
            Vector3 horizontalVelocity = forwardTarget.forward.normalized * moveSpeed;
            
            // 给刚体一个向上的速度和横向速度来实现贴墙跳跃
            thisRb.linearVelocity = new Vector3(horizontalVelocity.x, jumpForce, horizontalVelocity.z);
            
            ExitWallSliding();
        }
        else
        {
            // 普通跳跃：给刚体一个向上的速度来实现跳跃
            Vector3 currentVelocity = thisRb.linearVelocity;
            thisRb.linearVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
        }
        
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
               currentState != MovementState.HookDashing; // 可以在hook冲刺状态下使用冲刺打断
    }

    void StartDash()
    {
        currentState = MovementState.Dashing;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // 如果当前没有移动方向，则使用forwardTarget的前方作为冲刺方向
        if (moveDirection != Vector3.zero)
        {
            dashDirection = moveDirection.normalized;
        }
        else
        {
            // 使用forwardTarget的前方作为默认冲刺方向
            dashDirection = forwardTarget.forward.normalized;
        }
    }

    void EndDash()
    {
        // 冲刺结束时速度归零
        if (thisRb != null)
        {
            thisRb.linearVelocity = forwardTarget.forward*moveSpeed;
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

    #region hook冲刺方法
    void HandleHookDash()
    {
        // 检测鼠标右键点击
        if (Input.GetKeyDown(KeyCode.E) && CanHookDash())
        {
            CustomLog.Log(needLog, "试图开始hook冲刺");
            StartHookDash();
        }
        else
        {
            // CustomLog.Log(needLog, "无法开始hook冲刺");
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

        CustomLog.Log(needLog, "开始hook冲刺");
    }

    void EndHookDash()
    {
        // 到达hook位置后进入浮空状态，并归零速度
        currentState = MovementState.Falling;
        if (thisRb != null)
        {
            thisRb.linearVelocity = thisRb.linearVelocity.normalized * 6f;
        }
        CustomLog.Log(needLog, "结束hook冲刺，速度归零，进入浮空状态");
    }

    void HandleHookDashMovement()
    {
        if (currentState == MovementState.HookDashing && thisRb != null && hookTarget != null)
        {
            Debug.Log("Hook冲刺中");
            // 以冲刺速度冲向hook目标位置（使用立体的实际方向，包含Y轴分量）
            Vector3 hookDashVelocity = hookDashDirection * dashSpeed;
            thisRb.linearVelocity = hookDashVelocity;

            // 检查是否到达hook位置附近
            float distanceToHook = Vector3.Distance(transform.position, hookTarget.position);
            if (distanceToHook < 1f) // 到达目标附近
            {
                EndHookDash();
            }
        }
        else
        {
            // log state信息
            Debug.Log($"当前状态: {currentState}, hookTarget: {(hookTarget != null ? hookTarget.name : "null")}");


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

        // 检测鼠标右键按下和释放
        if (Input.GetMouseButtonDown(1))
        {
            StartDefense();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            EndDefense();
        }

        // 持续防御检测
        if (isDefending)
        {
            PerformDefense();
        }
    }

    private void PerformAttack()
    {
        // 执行攻击动作，这里简单记录日志
        CustomLog.Log(needLog, "开始攻击");

        // 检查是否有攻击触发器
        if (hitBoxTrigger == null)
        {
            CustomLog.LogWarning(needLog, "hitBoxTrigger 未设置，无法进行攻击检测");
            return;
        }

        // 显示攻击触发器0.1秒
        StartCoroutine(ShowHitBoxTemporarily());

        // 获取攻击触发器的位置和尺寸
        Vector3 center = hitBoxTrigger.position;
        Vector3 halfExtents = hitBoxTrigger.lossyScale / 2f;

        // 进行Box范围检测，查找tag为"enemy"的物体
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, hitBoxTrigger.rotation);

        bool hitEnemy = false;

        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                hitEnemy = true;
                CustomLog.Log(needLog, "击中！");
                // 对敌人造成伤害
                Rigidbody enemyRb = collider.attachedRigidbody;
                if (enemyRb != null)
                {
                    ICanBeHit enemy = enemyRb.GetComponent<ICanBeHit>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(attackDamage);
                    }
                    else
                    {
                        CustomLog.LogWarning(needLog, "敌人没有实现 ICanBeHit 接口，无法造成伤害");
                    }
                }
                else
                {
                    CustomLog.LogWarning(needLog, "敌人没有刚体组件，无法造成伤害");
                }
            }
        }

        // 生成攻击特效（无论是否击中敌人）
        if (hitEffect != null)
        {
            // 在攻击触发器位置生成特效
            GameObject effect = Instantiate(hitEffect, hitBoxTrigger.position, hitBoxTrigger.rotation);
            // 向前旋转90度（绕X轴旋转）
            effect.transform.rotation = hitBoxTrigger.rotation * Quaternion.Euler(90f, 130f, 0f);
            effect.transform.localScale *= 2;
            // 特效会自动播放（如果包含粒子系统或动画）
        }
        else
        {
            CustomLog.LogWarning(needLog, "hitEffect 未设置，无法显示攻击特效");
        }
    }


    private void StartDefense()
    {
        isDefending = true;
        CustomLog.Log(needLog, "开始持续防御");

        // 检查是否有防御触发器
        if (defenseBoxTrigger == null)
        {
            CustomLog.LogWarning(needLog, "defenseBoxTrigger 未设置，无法进行防御检测");
            return;
        }

        // 显示防御触发器
        ShowDefenseBox(true);
    }

    private void EndDefense()
    {
        isDefending = false;
        CustomLog.Log(needLog, "结束防御");

        // 隐藏防御触发器
        ShowDefenseBox(false);
    }

    private void PerformDefense()
    {
        // 执行防御动作，这里简单记录日志
        CustomLog.Log(needLog, "持续防御中");

        // 检查是否有防御触发器
        if (defenseBoxTrigger == null)
        {
            CustomLog.LogWarning(needLog, "defenseBoxTrigger 未设置，无法进行防御检测");
            return;
        }

        // 获取防御触发器的位置和尺寸
        Vector3 center = defenseBoxTrigger.position;
        Vector3 halfExtents = defenseBoxTrigger.lossyScale / 2f;

        // 进行Box范围检测，查找tag为"EnemyBullet"的物体
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, defenseBoxTrigger.rotation);

        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag("EnemyBullet"))
            {
                CustomLog.Log(needLog, "格挡！");
                // 对子弹进行防御处理（例如销毁子弹或反弹）
                EnemyBullet bullet = collider.GetComponent<EnemyBullet>();
                if (bullet != null)
                {
                    // 这里可以添加子弹防御逻辑，比如销毁子弹或改变其方向
                    Destroy(bullet.gameObject);
                }
            }
        }
    }

    private IEnumerator ShowHitBoxTemporarily()
    {
        if (hitBoxTrigger != null)
        {
            // 开启GameObject
            hitBoxTrigger.gameObject.SetActive(true);

            // 等待0.1秒
            yield return new WaitForSeconds(0.1f);

            // 关闭GameObject
            hitBoxTrigger.gameObject.SetActive(false);
        }
    }

    private void ShowDefenseBox(bool show)
    {
        if (defenseBoxTrigger != null)
        {
            // 开启或关闭GameObject
            defenseBoxTrigger.gameObject.SetActive(show);
        }
    }

    private IEnumerator ShowDefenseBoxTemporarily()
    {
        // 获取MeshRenderer组件
        MeshRenderer meshRenderer = defenseBoxTrigger.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // 开启MeshRenderer
            meshRenderer.enabled = true;
            defenseBoxTrigger.GetComponent<Collider>().enabled = true;

            // 等待0.1秒
            yield return new WaitForSeconds(1f);

            // 关闭MeshRenderer
            meshRenderer.enabled = false;
            defenseBoxTrigger.GetComponent<Collider>().enabled = false;
        }
    }
    #endregion

    #region 碰撞检测
    void OnCollisionEnter(Collision collision)
    {
        // 检测碰撞物体是否为Wall
        if (collision.gameObject.CompareTag("Wall"))
        {
            CustomLog.Log(needLog, "hahaha");

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
                    if (horizontalProjection != Vector3.zero)
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
            CustomLog.Log(needLog, "离开墙体时退出贴墙滑行状态");
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
        // 应用额外重力（贴墙状态、冲刺状态和hook冲刺状态下不应用重力）
        if (thisRb != null && currentState != MovementState.Dashing && currentState != MovementState.WallSliding && currentState != MovementState.HookDashing)
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

        // 禁用刚体重力
        if (thisRb != null)
        {
            thisRb.useGravity = false;
        }

        CustomLog.Log(needLog, "进入贴墙滑行状态");
    }

    void ExitWallSliding()
    {
        if (currentState == MovementState.WallSliding)
        {
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
        }
    }

    void CheckWallAttachment()
    {
        if (forwardTarget == null)
            return;

        // 从forwardTarget位置向墙面法线的反方向发射黄色射线
        Vector3 rayDirection = -wallNormal;
        Ray ray = new Ray(forwardTarget.position, rayDirection);
        // RaycastHit hit;

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
            CustomLog.LogError(needLog, "贴墙滑行时未检测到墙体，退出贴墙状态");
            // 如果射线没有检测到墙体，退出贴墙状态
            ExitWallSliding();
        }
        else
        {
            // 如果检测到墙体，更新墙面法线
            // Debug.LogError("-------------"); 
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
