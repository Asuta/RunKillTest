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
    private GameManager gameManager;
    
    [Header("平滑设置")]
    [SerializeField] private float positionSmoothSpeed = 10f; // 位置平滑速度
    [SerializeField] private float rotationSmoothSpeed = 15f; // 旋转平滑速度

    [Header("角度对齐")]
    [SerializeField] private RotationSnapMode rotationSnapMode = RotationSnapMode.Off;
    [SerializeField, Min(0f)] private float rotationSnapHysteresis = 4f;
    
    [Header("跟随设置")]
    public bool freezeYaxis = false;
    
    [Header("批量间接抓取设置")]
    // 用于批量间接抓取的中间数据
    private Transform indirectTarget;
    private Vector3 middlePosition;
    private Quaternion middleRotation = Quaternion.identity;
    private Vector3 indirectGrabOffset;
    private Quaternion indirectGrabRotationOffset;
    private bool isIndirectGrabbing = false;
    private Transform indirectRotationTarget; // 间接旋转跟随的目标

    // 角度对齐状态（用于抑制在临界角附近来回跳）
    private bool snapStateInitialized = false;
    private int snapIndexX;
    private int snapIndexY;
    private int snapIndexZ;
    private float snapRawX;
    private float snapRawY;
    private float snapRawZ;

    // 实现接口属性
    public Transform ObjectTransform => transform;
    public GameObject ObjectGameObject => gameObject;
    
    #endregion
    
    #region Unity生命周期
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindFirstObjectByType<GameManager>();
        // 初始化中间数据，保证间接跟随不会因为未初始化而跳变
        middlePosition = transform.position;
        middleRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // 优先处理批量间接抓取流程（如果启用，则独占控制，不与普通抓取逻辑混用）
        if (isIndirectGrabbing && indirectTarget != null)
        {
            // 立即跟随中心点，不使用插值
            transform.position = indirectTarget.position + indirectTarget.rotation * indirectGrabOffset;
            transform.rotation = indirectTarget.rotation * indirectGrabRotationOffset;
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

        bool enableRotationSnap = gameManager == null || gameManager.EnableGrabRotationSnap;
        if (rotationSnapMode != RotationSnapMode.Off && enableRotationSnap)
        {
            targetRotation = SnapRotation(targetRotation, freezeYaxis);
        }
        else
        {
            snapStateInitialized = false;
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

        snapStateInitialized = false;
        
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

        isIndirectGrabbing = false;
        indirectTarget = null;

        if (indirectRotationTarget != null)
        {
            Destroy(indirectRotationTarget.gameObject);
            indirectRotationTarget = null;
        }

        OnGrabbed(handTransform);
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
        snapStateInitialized = false;
        
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
        
        // 销毁旧的旋转目标
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
        
        // 记录本物体相对于中心点的偏移
        indirectGrabOffset = Quaternion.Inverse(centerTransform.rotation) * (transform.position - centerTransform.position);
        indirectGrabRotationOffset = Quaternion.Inverse(centerTransform.rotation) * transform.rotation;
        
        isIndirectGrabbing = true;
        Debug.Log($"{gameObject.name} 开始批量间接抓取，跟随中心点移动");
    }
    
    #endregion
    
    #region 工具方法

    private Quaternion SnapRotation(Quaternion rotation, bool yOnly)
    {
        float snapStep = GetRotationSnapStep();
        Vector3 euler = rotation.eulerAngles;

        if (!snapStateInitialized)
        {
            snapRawX = euler.x;
            snapRawY = euler.y;
            snapRawZ = euler.z;
            snapIndexX = Mathf.RoundToInt(snapRawX / snapStep);
            snapIndexY = Mathf.RoundToInt(snapRawY / snapStep);
            snapIndexZ = Mathf.RoundToInt(snapRawZ / snapStep);
            snapStateInitialized = true;
        }

        if (yOnly)
        {
            snapRawY = UnwrapAngle(snapRawY, euler.y);
            snapIndexY = UpdateSnapIndexSchmitt(snapIndexY, snapRawY, snapStep, rotationSnapHysteresis);
            euler.y = NormalizeAngle(snapIndexY * snapStep);
        }
        else
        {
            snapRawX = UnwrapAngle(snapRawX, euler.x);
            snapRawY = UnwrapAngle(snapRawY, euler.y);
            snapRawZ = UnwrapAngle(snapRawZ, euler.z);

            snapIndexX = UpdateSnapIndexSchmitt(snapIndexX, snapRawX, snapStep, rotationSnapHysteresis);
            snapIndexY = UpdateSnapIndexSchmitt(snapIndexY, snapRawY, snapStep, rotationSnapHysteresis);
            snapIndexZ = UpdateSnapIndexSchmitt(snapIndexZ, snapRawZ, snapStep, rotationSnapHysteresis);
            euler.x = NormalizeAngle(snapIndexX * snapStep);
            euler.y = NormalizeAngle(snapIndexY * snapStep);
            euler.z = NormalizeAngle(snapIndexZ * snapStep);
        }

        return Quaternion.Euler(euler);
    }

    private float GetRotationSnapStep()
    {
        return 15f;
    }

    private int UpdateSnapIndexSchmitt(int currentIndex, float rawAngle, float step, float hysteresis)
    {
        float halfStep = step * 0.5f;
        float hysteresisValue = Mathf.Clamp(Mathf.Max(0f, hysteresis), 0f, halfStep - 0.0001f);
        float threshold = halfStep + hysteresisValue;

        int guard = 0;
        while (guard < 12)
        {
            float snappedAngle = currentIndex * step;
            float delta = rawAngle - snappedAngle;

            if (delta > threshold)
            {
                currentIndex++;
                guard++;
                continue;
            }

            if (delta < -threshold)
            {
                currentIndex--;
                guard++;
                continue;
            }

            break;
        }

        return currentIndex;
    }

    private float UnwrapAngle(float previousRaw, float currentWrapped)
    {
        float previousWrapped = NormalizeAngle(previousRaw);
        float delta = Mathf.DeltaAngle(previousWrapped, currentWrapped);
        return previousRaw + delta;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
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
    
    #endregion
}
