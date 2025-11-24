using UnityEngine;

public class BeGrabobject  : MonoBehaviour, IGrabable
{
    #region 字段和属性
    
    [Header("基础抓取设置")]
    private bool isGrabbed = false;
    private Transform grabHand; // 抓取它的手部transform
    private Vector3 offsetFromHand; // 相对于手的偏移
    private Quaternion initialRotationOffset; // 初始旋转偏移
    private Rigidbody rb; // 刚体组件
    
    [Header("平滑设置")]
    [SerializeField] private float positionSmoothSpeed = 10f; // 位置平滑速度
    [SerializeField] private float rotationSmoothSpeed = 15f; // 旋转平滑速度
    
    [Header("跟随设置")]
    public bool freezeYaxis = false;
    
    [Header("间接抓取设置")]
    // 用于"间接差值跟随"算法的中间数据（不依赖可视化 controlObject）
    // indirectTarget 表示要追踪的 Transform（例如手或其它目标）
    private Transform indirectTarget;
    private Vector3 middlePosition;
    private Quaternion middleRotation = Quaternion.identity;
    private Vector3 indirectGrabOffset;
    private Quaternion indirectGrabRotationOffset;
    private bool isIndirectGrabbing = false;
    private Transform indirectRotationTarget; // 间接旋转跟随的目标

    // 实现接口属性
    public Transform ObjectTransform => transform;
    public GameObject ObjectGameObject => gameObject;
    
    #endregion
    
    #region Unity生命周期
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 初始化中间数据，保证间接跟随不会因为未初始化而跳变
        middlePosition = transform.position;
        middleRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // 优先处理"间接差值跟随"流程（如果启用，则独占控制，不与普通抓取逻辑混用）
        if (isIndirectGrabbing && indirectTarget != null)
        {
            UpdateIndirectGrab();
            return;
        }
    
        // 如果被抓取，跟随手部移动
        if (isGrabbed && grabHand != null)
        {
            UpdateNormalGrab();
        }
    }
    
    #endregion
    
    #region 抓取更新逻辑
    
    /// <summary>
    /// 更新间接抓取逻辑
    /// </summary>
    private void UpdateIndirectGrab()
    {
        float posAlphaIndirect = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
        float rotAlphaIndirect = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);

        // 中间数据以差值方式追踪 indirectTarget
        middlePosition = Vector3.Lerp(middlePosition, indirectTarget.position, posAlphaIndirect);
        // 只对Y轴进行插值，保持X轴和Z轴不变
        Vector3 currentEuler = middleRotation.eulerAngles;
        Vector3 targetEuler = indirectRotationTarget.rotation.eulerAngles;
        float newY = Mathf.LerpAngle(currentEuler.y, targetEuler.y, rotAlphaIndirect);
        middleRotation = Quaternion.Euler(currentEuler.x, newY, currentEuler.z);

        // 本物体（grabObject）立即依据中间数据与记录的偏移保持相对关系（无差值）
        transform.position = middlePosition + middleRotation * indirectGrabOffset;
        transform.rotation = middleRotation * indirectGrabRotationOffset;
    }
    
    /// <summary>
    /// 更新普通抓取逻辑
    /// </summary>
    private void UpdateNormalGrab()
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
    
    #endregion
    
    #region 抓取接口实现
    
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
        
        // 抓取时禁用插值，避免与直接控制Transform冲突
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.None;
        }
        
        Debug.Log($"{gameObject.name} 被 {GetHandName(handTransform)} 抓住了");
    }
    
    /// <summary>
    /// 统一抓取方法（包含设置状态和抓取）
    /// 对于 BeGrabobject，使用间接抓取方式
    /// </summary>
    /// <param name="handTransform">抓住它的手部transform</param>
    public void UnifiedGrab(Transform handTransform)
    {
        if (handTransform == null) return;
        
        // 设置抓取状态
        if (!isGrabbed)
        {
            isGrabbed = true;
            grabHand = handTransform;
        }
        
        // 执行批量间接抓取（使用自身作为中心点）
        BatchIndirectGrab(handTransform, transform);
        
        Debug.Log($"{gameObject.name} 被 {GetHandName(handTransform)} 统一抓取（间接抓取）");
    }
    
    /// <summary>
    /// 松开时调用（根据释放的手判断是否仍由另一只手抓取）
    /// </summary>
    /// <param name="releasedHandTransform">释放的手部transform</param>
    public void OnReleased(Transform releasedHandTransform)
    {
        // 停止间接抓取状态
        isIndirectGrabbing = false;
        indirectTarget = null;

        // 销毁子对象
        if (indirectRotationTarget != null)
        {
            Destroy(indirectRotationTarget.gameObject);
            indirectRotationTarget = null;
        }
        
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
        
        // 释放时恢复插值
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        Debug.Log($"{gameObject.name} 被完全松开了");
    }
    
    #endregion
    
    #region 批量间接抓取方法
    
    /// <summary>
    /// 批量间接抓取实现
    /// </summary>
    /// <param name="handTransform">抓取的手部transform</param>
    /// <param name="centerTransform">中心点transform</param>
    public void BatchIndirectGrab(Transform handTransform, Transform centerTransform)
    {
        if (handTransform == null || centerTransform == null) return;
        
        // 停止现有的间接抓取
        isIndirectGrabbing = false;
        indirectTarget = null;

        // 销毁子对象
        if (indirectRotationTarget != null)
        {
            Destroy(indirectRotationTarget.gameObject);
            indirectRotationTarget = null;
        }
        
        // 设置间接目标为中心点
        indirectTarget = centerTransform;
        
        // 创建用于旋转的子对象
        GameObject rotationTargetGO = new GameObject("IndirectRotationTarget");
        rotationTargetGO.transform.position = centerTransform.position;
        rotationTargetGO.transform.rotation = centerTransform.rotation;
        rotationTargetGO.transform.SetParent(centerTransform);
        indirectRotationTarget = rotationTargetGO.transform;
        
        // 初始化中间数据为中心点的当前位置和旋转
        middlePosition = centerTransform.position;
        middleRotation = centerTransform.rotation;
        
        // 记录本物体相对于中心点的偏移
        indirectGrabOffset = Quaternion.Inverse(middleRotation) * (transform.position - middlePosition);
        indirectGrabRotationOffset = Quaternion.Inverse(middleRotation) * transform.rotation;
        
        isIndirectGrabbing = true;
        Debug.Log($"{gameObject.name} 开始批量间接抓取，跟随中心点移动");
    }
    
    #endregion
    
    #region 工具方法
    
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
    
    #endregion
}
