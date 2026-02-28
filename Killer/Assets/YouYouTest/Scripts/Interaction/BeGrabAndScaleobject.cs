using UnityEngine;
using YouYouTest.CommandFramework;

/// <summary>
/// 缩放轴枚举
/// </summary>
public enum ScaleAxis
{
    X,
    Y,
    Z
}

/// <summary>
/// 缩放轴数据类
/// </summary>
public class ScaleAxisData
{
    public ScaleAxis Axis { get; set; }
    public float Value { get; set; }
}

public class BeGrabAndScaleobject : MonoBehaviour, IGrabable
{
    // IGrabable 接口实现
    public Transform ObjectTransform => transform;
    public GameObject ObjectGameObject => gameObject;

    [Header("平滑设置")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 15f;

    [Header("角度对齐")]
    [SerializeField, Min(0f)] private float rotationSnapHysteresis = 4f;

    [Header("跟随设置")]
    public bool freezeYaxis = false;

    [Header("缩放控制")]
    [SerializeField] private bool scaleControlEnabled = true;
    public bool ScaleControlEnabled
    {
        get => scaleControlEnabled;
        set => scaleControlEnabled = value;
    }
    
    private bool isGrabbed = false;
    private Transform primaryHand;   // 主手（用于位置/旋转跟随）
    private Transform secondaryHand; // 副手（用于双手缩放）
    
    private Vector3 offsetFromPrimary;
    private Quaternion rotationOffsetFromPrimary;
    
    // 用于批量间接抓取的中间数据
    private Transform indirectTarget;
    private Vector3 middlePosition;
    private Quaternion middleRotation = Quaternion.identity;
    private Vector3 indirectGrabOffset;
    private Quaternion indirectGrabRotationOffset;
    private bool isIndirectGrabbing = false;
    private Transform indirectRotationTarget; // 间接旋转跟随的目标
    
    // 双手缩放所需数据
    private bool isTwoHandScaling = false;
    private float initialHandsDistance = 0f;
    private Vector3 baseScale;
    private Quaternion twoHandRotationOffset = Quaternion.identity;
    private bool isNewScaleGesture = false; // 是否是新的缩放手势
    
    // 单轴缩放相关
    private ScaleAxisData scaleAxisData = new ScaleAxisData();
    private float recordScale = 0f; // 记录的缩放值
    private float recordHandDistance = 0f; // 记录的双手距离
    private ScaleAxis? lastScaleAxis = null; // 上次的缩放轴

    private EditorPlayer editorPlayer; // 用于检测当前是否双手都在抓
    private GameManager gameManager;
    
    // 命令系统相关
    private ScaleCommand currentScaleCommand; // 当前缩放命令
    private bool isCommandActive = false; // 是否有活跃的命令

    // 角度对齐状态（用于抑制在临界角附近来回跳）
    private bool snapStateInitialized = false;
    private int snapIndexX;
    private int snapIndexY;
    private int snapIndexZ;
    private float snapRawX;
    private float snapRawY;
    private float snapRawZ;

    private void Awake()
    {
        editorPlayer = FindFirstObjectByType<EditorPlayer>();
        gameManager = FindFirstObjectByType<GameManager>();
        // 初始化中间数据，保证间接跟随不会因为未初始化而跳变
        middlePosition = transform.position;
        middleRotation = transform.rotation;
    }

    private void Update()
    {
        // 优先处理批量间接抓取流程（如果启用，则独占控制，不与普通抓取逻辑混用）
        if (isIndirectGrabbing && indirectTarget != null)
        {
            // 立即跟随中心点，不使用插值
            transform.position = indirectTarget.position + indirectTarget.rotation * indirectGrabOffset;
            transform.rotation = indirectTarget.rotation * indirectGrabRotationOffset;
            
            // 中断后续普通抓取逻辑
            return;
        }
    
        // --- 下面为原有的同步两手/单手跟随逻辑（保持不变） ---
        // 同步两手抓取状态（以 EditorPlayer 为权威信息）
        bool leftHolding = editorPlayer != null && editorPlayer.leftGrabbedObject == (IGrabable)this;
        bool rightHolding = editorPlayer != null && editorPlayer.rightGrabbedObject == (IGrabable)this;
        bool nowTwoHand = leftHolding && rightHolding;
    
        // 进入双手状态时记录初始距离与缩放
        if (!isTwoHandScaling && nowTwoHand)
        {
            isTwoHandScaling = true;
            isNewScaleGesture = true; // 标记为新的缩放手势
            
            // 确认两只手引用
            if (editorPlayer != null)
            {
                if (primaryHand == null)
                {
                    // 若此前未建立主手，优先取已抓住的那只
                    primaryHand = leftHolding ? editorPlayer.leftHand : editorPlayer.rightHand;
                    offsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * (transform.position - primaryHand.position);
                    rotationOffsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * transform.rotation;
                }
                secondaryHand = (primaryHand == editorPlayer.leftHand) ? editorPlayer.rightHand : editorPlayer.leftHand;
                initialHandsDistance = Vector3.Distance(editorPlayer.leftHand.position, editorPlayer.rightHand.position);
                baseScale = transform.localScale;
                        Quaternion avgRot = Quaternion.Slerp(editorPlayer.leftHand.rotation, editorPlayer.rightHand.rotation, 0.5f);
                        twoHandRotationOffset = Quaternion.Inverse(avgRot) * transform.rotation;
            }
            
            // 创建缩放命令
                    CreateScaleCommand();
        }
        // 退出双手状态（其中一只手松开）
        else if (isTwoHandScaling && !nowTwoHand)
        {
            isTwoHandScaling = false;
            
            // 完成缩放命令
            CompleteScaleCommand();
            
            // 仅剩的一只手继续作为主手跟随
            if (leftHolding && editorPlayer != null)
            {
                primaryHand = editorPlayer.leftHand;
            }
            else if (rightHolding && editorPlayer != null)
            {
                primaryHand = editorPlayer.rightHand;
            }
            else
            {
                // 都不再抓，等待 OnReleased 统一清理
                primaryHand = null;
            }
            secondaryHand = null;
            // 重新计算与主手的偏移以避免跳变
            if (primaryHand != null)
            {
                offsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * (transform.position - primaryHand.position);
                rotationOffsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * transform.rotation;
            }
        }
    
        if (!isGrabbed || primaryHand == null) return;
    
        // 单手/主手 跟随位置和旋转（与 BeGrabobject 一致）
        Vector3 targetPos = primaryHand.position + primaryHand.rotation * offsetFromPrimary;
        Quaternion targetRot = primaryHand.rotation * rotationOffsetFromPrimary;
    
        // 双手时，位置保持与主手的相对位置，旋转跟随两手平均朝向（保持进入时的相对旋转偏移）
        if (isTwoHandScaling && editorPlayer != null && editorPlayer.leftHand != null && editorPlayer.rightHand != null)
        {
            // 保持与主手的相对位置，不移动到两手中心
            targetPos = primaryHand.position + primaryHand.rotation * offsetFromPrimary;
            Quaternion avgRot = Quaternion.Slerp(editorPlayer.leftHand.rotation, editorPlayer.rightHand.rotation, 0.5f);
            targetRot = avgRot * twoHandRotationOffset;
        }
    
        if (freezeYaxis)
        {
            Vector3 curEuler = transform.rotation.eulerAngles;
            Vector3 targetEuler = targetRot.eulerAngles;
            targetRot = Quaternion.Euler(curEuler.x, targetEuler.y, curEuler.z);
        }

        // 按配置进行角度对齐（15° 的倍数）
        bool enableRotationSnap = gameManager == null || gameManager.EnableGrabRotationSnap;
        if (enableRotationSnap)
        {
            targetRot = SnapRotation(targetRot, freezeYaxis);
        }
        else
        {
            snapStateInitialized = false;
        }
    
        float posAlpha = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
        float rotAlpha = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, posAlpha);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotAlpha);
    
        // 双手缩放：使用单轴缩放
        if (isTwoHandScaling && scaleControlEnabled && editorPlayer != null && editorPlayer.leftHand != null && editorPlayer.rightHand != null)
        {
            PerformSingleAxisScaling();
        }
    }

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
    /// 执行单轴缩放
    /// </summary>
    private void PerformSingleAxisScaling()
    {
        if (!scaleControlEnabled) return;

        // 获取双手向量
        Vector3 handVector = editorPlayer.rightHand.position - editorPlayer.leftHand.position;
        
        // 计算与各轴的夹角
        float angleX = Vector3.Angle(handVector, transform.right);
        angleX = Mathf.Min(angleX, 180 - angleX);
        float angleY = Vector3.Angle(handVector, transform.up);
        angleY = Mathf.Min(angleY, 180 - angleY);
        float angleZ = Vector3.Angle(handVector, transform.forward);
        angleZ = Mathf.Min(angleZ, 180 - angleZ);
        
        // 确定最小夹角的轴
        ScaleAxis currentScaleAxis;
        if (angleX < angleY && angleX < angleZ)
        {
            currentScaleAxis = ScaleAxis.X;
            scaleAxisData.Value = transform.localScale.x;
        }
        else if (angleY < angleX && angleY < angleZ)
        {
            currentScaleAxis = ScaleAxis.Y;
            scaleAxisData.Value = transform.localScale.y;
        }
        else
        {
            currentScaleAxis = ScaleAxis.Z;
            scaleAxisData.Value = transform.localScale.z;
        }
        
        scaleAxisData.Axis = currentScaleAxis;
        
        // 当开始新的缩放手势，或缩放轴改变时，记录初始值
        if (isNewScaleGesture || lastScaleAxis != currentScaleAxis)
        {
            isNewScaleGesture = false; // 消耗标记
            lastScaleAxis = currentScaleAxis;
            recordScale = scaleAxisData.Value;
            recordHandDistance = Vector3.Distance(
                editorPlayer.leftHand.position,
                editorPlayer.rightHand.position
            );
            Debug.Log($"{gameObject.name} 开始或重置单轴缩放，轴: {currentScaleAxis}, 初始值: {recordScale:F3}, 初始距离: {recordHandDistance:F3}");
        }
        
        // 根据两只手的距离和记录的距离来计算缩放比例
        float currentHandDistance = Vector3.Distance(
            editorPlayer.leftHand.position,
            editorPlayer.rightHand.position
        );
        
        if (recordHandDistance > 1e-4f)
        {
            float scaleRate = currentHandDistance / recordHandDistance;
            Vector3 currentScale = transform.localScale;
            
            switch (currentScaleAxis)
            {
                case ScaleAxis.X:
                    transform.localScale = new Vector3(
                        recordScale * scaleRate,
                        currentScale.y,
                        currentScale.z
                    );
                    break;
                case ScaleAxis.Y:
                    transform.localScale = new Vector3(
                        currentScale.x,
                        recordScale * scaleRate,
                        currentScale.z
                    );
                    break;
                case ScaleAxis.Z:
                    transform.localScale = new Vector3(
                        currentScale.x,
                        currentScale.y,
                        recordScale * scaleRate
                    );
                    break;
            }
        }
    }

    // 抓取：单手逻辑与 BeGrabobject 一致；第二只手抓取将进入双手缩放
    public void OnGrabbed(Transform handTransform)
    {
        if (handTransform == null) return;

        if (!isGrabbed)
        {
            isGrabbed = true;
            primaryHand = handTransform;
            secondaryHand = null;
            isTwoHandScaling = false;
            snapStateInitialized = false;

            offsetFromPrimary = Quaternion.Inverse(handTransform.rotation) * (transform.position - handTransform.position);
            rotationOffsetFromPrimary = Quaternion.Inverse(handTransform.rotation) * transform.rotation;

            Debug.Log($"{gameObject.name} 被 {GetHandName(handTransform)} 抓住了（单手）");
            return;
        }

        // 已被抓：若是另一只手加入，则进入双手缩放
        if (handTransform != primaryHand && secondaryHand == null)
        {
            secondaryHand = handTransform;
            // 若能拿到 EditorPlayer，立刻记录一次初始值
            if (editorPlayer == null) editorPlayer = FindFirstObjectByType<EditorPlayer>();
            if (editorPlayer != null)
            {
                initialHandsDistance = Vector3.Distance(editorPlayer.leftHand.position, editorPlayer.rightHand.position);
                baseScale = transform.localScale;
                Quaternion avgRot = Quaternion.Slerp(editorPlayer.leftHand.rotation, editorPlayer.rightHand.rotation, 0.5f);
                twoHandRotationOffset = Quaternion.Inverse(avgRot) * transform.rotation;
                isTwoHandScaling = true;
                isNewScaleGesture = true; // 标记为新的缩放手势
                Debug.Log($"{gameObject.name} 进入双手抓取，记录初始距离 {initialHandsDistance:F3} 与基准缩放 {baseScale}");
                
                // 创建缩放命令
                CreateScaleCommand();
            }
        }
    }
    
    /// <summary>
    /// 统一抓取方法（包含设置状态和抓取）
    /// 对于 BeGrabAndScaleobject，使用间接抓取方式
    /// </summary>
    /// <param name="handTransform">抓住它的手部transform</param>
    public void UnifiedGrab(Transform handTransform)
    {
        if (handTransform == null) return;
        
        // 设置抓取状态
        if (!isGrabbed)
        {
            isGrabbed = true;
            primaryHand = handTransform;
            secondaryHand = null;
            isTwoHandScaling = false;
        }
        
        // 执行批量间接抓取（使用自身作为中心点）
        BatchIndirectGrab(handTransform, transform);
        
        Debug.Log($"{gameObject.name} 被 {GetHandName(handTransform)} 统一抓取（间接抓取）");
    }

    // 释放一只手：若仍有另一只手抓取，则切回单手；否则完全释放
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
        
        if (!isGrabbed) return;

        // 若释放的是主手
        if (releasedHandTransform == primaryHand)
        {
            if (secondaryHand != null)
            {
                // 将副手提升为主手，退出双手缩放，并重算与新主手的偏移
                primaryHand = secondaryHand;
                secondaryHand = null;
                isTwoHandScaling = false;
                offsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * (transform.position - primaryHand.position);
                rotationOffsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * transform.rotation;
                Debug.Log($"{gameObject.name} 从 {GetHandName(releasedHandTransform)} 释放，继续由 {GetHandName(primaryHand)} 单手抓取");
                return;
            }
            // 没有其他手抓了，完全释放
            isGrabbed = false;
            isTwoHandScaling = false;
            primaryHand = null;
            secondaryHand = null;
            snapStateInitialized = false;
            
            // 清理命令状态
            CleanupCommand();
            
            Debug.Log($"{gameObject.name} 被完全松开了");
            return;
        }

        // 若释放的是副手，仅退出双手缩放，保持主手抓取
        if (releasedHandTransform == secondaryHand)
        {
            secondaryHand = null;
            isTwoHandScaling = false;
            Debug.Log($"{gameObject.name} 从 {GetHandName(releasedHandTransform)} 释放，退出双手抓取，保持单手");
            return;
        }

        // 兜底：尝试用 EditorPlayer 判定当前是否还被任一只手抓取
        if (editorPlayer == null) editorPlayer = FindFirstObjectByType<EditorPlayer>();
        bool leftHolding = editorPlayer != null && editorPlayer.leftGrabbedObject == (IGrabable)this;
        bool rightHolding = editorPlayer != null && editorPlayer.rightGrabbedObject == (IGrabable)this;
        if (!(leftHolding || rightHolding))
        {
            isGrabbed = false;
            isTwoHandScaling = false;
            primaryHand = null;
            secondaryHand = null;
            snapStateInitialized = false;
            
            // 清理命令状态
            CleanupCommand();
            
            Debug.Log($"{gameObject.name} 被完全松开了");
        }
    }

    private string GetHandName(Transform handTransform)
    {
        if (editorPlayer == null) editorPlayer = FindFirstObjectByType<EditorPlayer>();
        if (editorPlayer != null)
        {
            if (handTransform == editorPlayer.leftHand) return "左手";
            if (handTransform == editorPlayer.rightHand) return "右手";
        }
        return "未知手部";
    }
    
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
    
    /// <summary>
    /// 创建缩放命令
    /// </summary>
    private void CreateScaleCommand()
    {
        if (!scaleControlEnabled)
        {
            return;
        }

        if (!isCommandActive)
        {
            ScaleAxis? scaleAxis = lastScaleAxis;
            currentScaleCommand = new ScaleCommand(
                transform,
                baseScale,
                transform.position,
                transform.rotation,
                scaleAxis
            );
            isCommandActive = true;
            string axisInfo = scaleAxis.HasValue ? $"轴: {scaleAxis.Value}" : "整体缩放";
            Debug.Log($"{gameObject.name} 创建缩放命令，{axisInfo}，初始缩放: {baseScale}");
        }
    }
    
    /// <summary>
    /// 完成缩放命令
    /// </summary>
    private void CompleteScaleCommand()
    {
        if (isCommandActive && currentScaleCommand != null)
        {
            // 设置最终状态
            currentScaleCommand.SetEndTransform(
                transform.localScale,
                transform.position,
                transform.rotation
            );
            
            // 执行命令
            CommandHistory.Instance.ExecuteCommand(currentScaleCommand);
            
            string axisInfo = currentScaleCommand.GetScaleAxis().HasValue ? $"轴: {currentScaleCommand.GetScaleAxis().Value}" : "整体缩放";
            Debug.Log($"{gameObject.name} 完成缩放命令，{axisInfo}，最终缩放: {transform.localScale}");
            
            // 重置命令状态
            currentScaleCommand = null;
            isCommandActive = false;
            lastScaleAxis = null; // 重置缩放轴
        }
    }
    
    /// <summary>
    /// 在物体被完全释放时清理命令状态
    /// </summary>
    private void CleanupCommand()
    {
        if (isCommandActive && currentScaleCommand != null)
        {
            // 如果有未完成的命令，直接完成它
            CompleteScaleCommand();
        }
    }
}
