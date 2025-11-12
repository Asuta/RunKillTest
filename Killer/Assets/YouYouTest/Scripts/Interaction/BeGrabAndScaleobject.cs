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

    [Header("跟随设置")]
    public bool freezeYaxis = false;
    
    private bool isGrabbed = false;
    private Transform primaryHand;   // 主手（用于位置/旋转跟随）
    private Transform secondaryHand; // 副手（用于双手缩放）
    
    private Vector3 offsetFromPrimary;
    private Quaternion rotationOffsetFromPrimary;
    
    // 用于“间接差值跟随”算法的中间数据（不依赖可视化 controlObject）
    // indirectTarget 表示要追踪的 Transform（例如手或其它目标）
    private Transform indirectTarget;
    private Vector3 middlePosition;
    private Quaternion middleRotation = Quaternion.identity;
    private Vector3 indirectGrabOffset;
    private Quaternion indirectGrabRotationOffset;
    private bool isIndirectGrabbing = false;
    
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
    
    // 命令系统相关
    private ScaleCommand currentScaleCommand; // 当前缩放命令
    private bool isCommandActive = false; // 是否有活跃的命令

    private void Awake()
    {
        editorPlayer = FindFirstObjectByType<EditorPlayer>();
        // 初始化中间数据，保证间接跟随不会因为未初始化而跳变
        middlePosition = transform.position;
        middleRotation = transform.rotation;
    }

    private void Update()
    {
        // 优先处理“间接差值跟随”流程（如果启用，则独占控制，不与普通抓取逻辑混用）
        if (isIndirectGrabbing && indirectTarget != null)
        {
            float posAlphaIndirect = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
            float rotAlphaIndirect = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
    
            // 中间数据以差值方式追踪 indirectTarget
            middlePosition = Vector3.Lerp(middlePosition, indirectTarget.position, posAlphaIndirect);
            middleRotation = Quaternion.Slerp(middleRotation, indirectTarget.rotation, rotAlphaIndirect);
    
            // 本物体（grabObject）立即依据中间数据与记录的偏移保持相对关系（无差值）
            transform.position = middlePosition + middleRotation * indirectGrabOffset;
            transform.rotation = middleRotation * indirectGrabRotationOffset;
    
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
    
        float posAlpha = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
        float rotAlpha = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, posAlpha);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotAlpha);
    
        // 双手缩放：使用单轴缩放
        if (isTwoHandScaling && editorPlayer != null && editorPlayer.leftHand != null && editorPlayer.rightHand != null)
        {
            PerformSingleAxisScaling();
        }
    }
    
    /// <summary>
    /// 执行单轴缩放
    /// </summary>
    private void PerformSingleAxisScaling()
    {
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

    // 释放一只手：若仍有另一只手抓取，则切回单手；否则完全释放
    public void OnReleased(Transform releasedHandTransform)
    {
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
    /// 开始“间接差值抓取”
    /// - 不修改现有 OnGrabbed/OnReleased 行为
    /// - 使用一个内部中间数据（middlePosition/middleRotation）以差值追踪 handTransform
    /// - 记录本物体相对于中间数据的偏移，随后每帧让物体立即保持该相对关系（无插值）
    /// </summary>
    public void StartIndirectGrab(Transform handTransform)
    {
        if (handTransform == null) return;
    
        indirectTarget = handTransform;
        // 为避免跳变，初始化中间数据为当前手的位置/旋转
        middlePosition = indirectTarget.position;
        middleRotation = indirectTarget.rotation;
    
        // 记录本物体相对于中间数据的偏移
        indirectGrabOffset = Quaternion.Inverse(middleRotation) * (transform.position - middlePosition);
        indirectGrabRotationOffset = Quaternion.Inverse(middleRotation) * transform.rotation;
    
        isIndirectGrabbing = true;
    }
    
    /// <summary>
    /// 停止间接抓取
    /// </summary>
    public void StopIndirectGrab()
    {
        isIndirectGrabbing = false;
        indirectTarget = null;
    }
    
    /// <summary>
    /// 创建缩放命令
    /// </summary>
    private void CreateScaleCommand()
    {
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