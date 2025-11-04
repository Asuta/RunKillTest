
using UnityEngine;

public class GameLobbyUIMove : MonoBehaviour
{
    public Transform moveTarget;
    public Vector3 offset;
    public Vector3 offset2;
    public Transform lookTarget;

    public float maxDistance = 10f;
     
    
    public float lerpSpeed = 10f; // 插值速度，可以调整这个值来控制插值的快慢
    
    // 存储原始值的变量
    private Vector3 originalOffset;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 保存原始值
        originalOffset = offset;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (moveTarget != null && GameManager.Instance != null)
        {
            Vector3 targetPosition = moveTarget.position + originalOffset + offset2;
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // 如果距离大于maxDistance，直接移动到目标位置
            if (distance > maxDistance)
            {
                transform.position = targetPosition;
            }
            else
            {
                // 使用插值平滑地移动到目标位置
                transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.deltaTime);
            }
        }

        if (lookTarget != null)
        {
            // 使用反向lookat，使得物体背离lookTarget的方向
            Vector3 directionFromTarget = transform.position - lookTarget.position;
            transform.rotation = Quaternion.LookRotation(directionFromTarget);
        }
        
    }
}
