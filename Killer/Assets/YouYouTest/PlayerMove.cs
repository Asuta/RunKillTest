using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public Transform forwardTarget;
    private Rigidbody thisRb;
    public float moveSpeed = 5f;
    
    
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
    }
    
    void FixedUpdate()
    {
        HandleMovement();
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
        if (thisRb == null || moveDirection == Vector3.zero)
            return;
            
        // 归一化并应用速度到刚体
        Vector3 normalizedDirection = moveDirection.normalized;
        thisRb.linearVelocity = normalizedDirection * moveSpeed;
    }
}
