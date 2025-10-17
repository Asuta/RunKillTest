using UnityEngine;

public class BeGrabobject  : MonoBehaviour, IGrabable
{
    private bool isGrabbed = false;
    private Transform grabHand; // 抓取它的手部transform
    private Vector3 offsetFromHand; // 相对于手的偏移
    private Quaternion initialRotationOffset; // 初始旋转偏移
    
    [Header("平滑设置")]
    [SerializeField] private float positionSmoothSpeed = 10f; // 位置平滑速度
    [SerializeField] private float rotationSmoothSpeed = 15f; // 旋转平滑速度
    [Header("跟随设置")]
    public bool freezeYaxis = false;

    
    // 实现接口属性
    public Transform ObjectTransform => transform;
    public GameObject ObjectGameObject => gameObject;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 如果被抓取，跟随手部移动
        if (isGrabbed && grabHand != null)
        {
            // 计算目标位置和旋转
            Vector3 targetPosition = grabHand.position + grabHand.rotation * offsetFromHand;
            Quaternion targetRotation = grabHand.rotation * initialRotationOffset;
            
            // 如果冻结Y轴，只保留Y轴旋转
            if (freezeYaxis)
            {
                // 获取当前旋转的欧拉角
                Vector3 currentEuler = transform.rotation.eulerAngles;
                // 获取目标旋转的欧拉角
                Vector3 targetEuler = targetRotation.eulerAngles;
                // 只使用目标的Y轴旋转，保持当前的X和Z轴旋转
                targetRotation = Quaternion.Euler(currentEuler.x, targetEuler.y, currentEuler.z);
            }
            
            // 使用Lerp进行平滑移动
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 被抓住时调用
    /// </summary>
    /// <param name="handTransform">抓住它的手部transform</param>
    public void OnGrabbed(Transform handTransform)
    {
        if (isGrabbed) return; // 已经被抓取了，不再处理
        
        isGrabbed = true;
        grabHand = handTransform;
        
        // 计算相对于手的偏移（保持当前位置）
        offsetFromHand = Quaternion.Inverse(handTransform.rotation) * (transform.position - handTransform.position);
        
        // 计算初始旋转偏移（保持当前旋转）
        initialRotationOffset = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
        
        Debug.Log($"{gameObject.name} 被抓住了");
    }
    
    /// <summary>
    /// 松开时调用
    /// </summary>
    public void OnReleased()
    {
        if (!isGrabbed) return; // 没有被抓取，不处理
        
        isGrabbed = false;
        grabHand = null;
        
        Debug.Log($"{gameObject.name} 被松开了");
    }
}
