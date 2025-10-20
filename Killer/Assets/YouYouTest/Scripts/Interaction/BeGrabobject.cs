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
        // 无论是否已经被抓取，都允许新的手抓取（切换到新的手）
        isGrabbed = true;
        grabHand = handTransform;
        
        // 计算相对于手的偏移（保持当前位置）
        offsetFromHand = Quaternion.Inverse(handTransform.rotation) * (transform.position - handTransform.position);
        
        // 计算初始旋转偏移（保持当前旋转）
        initialRotationOffset = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
        
        Debug.Log($"{gameObject.name} 被 {GetHandName(handTransform)} 抓住了");
    }
    
    /// <summary>
    /// 获取手部名称用于调试
    /// </summary>
    /// <param name="handTransform">手部transform</param>
    /// <returns>手部名称</returns>
    private string GetHandName(Transform handTransform)
    {
        // 通过EditorPlayer获取手部名称
        EditorPlayer editorPlayer = FindFirstObjectByType<EditorPlayer>();
        if (editorPlayer != null)
        {
            if (handTransform == editorPlayer.leftHand)
                return "左手";
            else if (handTransform == editorPlayer.rightHand)
                return "右手";
        }
        return "未知手部";
    }
    
    /// <summary>
    /// 松开时调用（根据释放的手判断是否仍由另一只手抓取）
    /// </summary>
    /// <param name="releasedHandTransform">释放的手部transform</param>
    public void OnReleased(Transform releasedHandTransform)
    {
        if (!isGrabbed) return; // 没有被抓取，不处理

        EditorPlayer editorPlayer = FindFirstObjectByType<EditorPlayer>();
        bool stillHeldByOther = false;
        Transform otherHand = null;

        if (editorPlayer != null && releasedHandTransform != null)
        {
            if (releasedHandTransform == editorPlayer.leftHand)
            {
                stillHeldByOther = editorPlayer.rightGrabbedObject == (IGrabable)this;
                otherHand = editorPlayer.rightHand;
            }
            else if (releasedHandTransform == editorPlayer.rightHand)
            {
                stillHeldByOther = editorPlayer.leftGrabbedObject == (IGrabable)this;
                otherHand = editorPlayer.leftHand;
            }
            else
            {
                // 无法识别释放自哪只手，保守判断是否仍被任一只手持有
                stillHeldByOther = (editorPlayer.leftGrabbedObject == (IGrabable)this) || (editorPlayer.rightGrabbedObject == (IGrabable)this);
                otherHand = (editorPlayer.leftGrabbedObject == (IGrabable)this) ? editorPlayer.leftHand :
                            (editorPlayer.rightGrabbedObject == (IGrabable)this) ? editorPlayer.rightHand : null;
            }
        }

        if (stillHeldByOther && otherHand != null)
        {
            // 切换到另一只手继续跟随，并重算偏移/旋转偏移，避免跳变
            grabHand = otherHand;
            offsetFromHand = Quaternion.Inverse(otherHand.rotation) * (transform.position - otherHand.position);
            initialRotationOffset = Quaternion.Inverse(otherHand.rotation) * transform.rotation;
            Debug.Log($"{gameObject.name} 从 {GetHandName(releasedHandTransform)} 释放，但仍由 {GetHandName(otherHand)} 抓取");
            return;
        }

        // 真正被完全释放
        isGrabbed = false;
        grabHand = null;
        Debug.Log($"{gameObject.name} 被完全松开了");
    }
}
