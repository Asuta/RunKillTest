
using UnityEngine;

public class ItemUIMove : MonoBehaviour
{
    public Transform moveTarget;
    public Vector3 offset;
    public Vector3 offset2;
    public Transform lookTarget;

    public float maxDistance = 10f;
     
    
    public float lerpSpeed = 10f; // 插值速度，可以调整这个值来控制插值的快慢
    
    // 存储原始值的变量
    private Vector3 originalOffset;
    private float originalMaxDistance;
    
    // 当前缩放值（通过事件更新）
    private float currentScale = 1f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 保存原始值
        originalOffset = offset;
        originalMaxDistance = maxDistance;
        
        // 初始化当前缩放值
        if (GameManager.Instance != null)
        {
            currentScale = GameManager.Instance.VrEditorScale;
            // 注册缩放变化事件
            GameManager.Instance.OnVrEditorScaleChanged += OnScaleChanged;
        }
    }
    
    void OnDestroy()
    {
        // 取消注册事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVrEditorScaleChanged -= OnScaleChanged;
        }
    }
    
    /// <summary>
    /// 缩放变化事件处理
    /// </summary>
    private void OnScaleChanged(float oldScale, float newScale)
    {
        currentScale = newScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (moveTarget != null && GameManager.Instance != null)
        {
            float scale = currentScale;
            
            // 根据缩放因子调整偏移和最大距离
            Vector3 scaledOffset = originalOffset * scale;
            Vector3 scaledOffset2 = offset2 * scale;
            float scaledMaxDistance = originalMaxDistance * scale;
            
            Vector3 targetPosition = moveTarget.position + scaledOffset+scaledOffset2;
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // 如果距离大于maxDistance，直接移动到目标位置
            if (distance > scaledMaxDistance)
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
