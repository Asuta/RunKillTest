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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisRb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // 直接使用强类型属性访问 actions
        Vector2 moveInput = InputActionsManager.Actions.XRILeftLocomotion.Move.ReadValue<Vector2>();
        
        // 计算移动方向
        CalculateMovement(moveInput);
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
}
