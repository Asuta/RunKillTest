using UnityEngine;

/// <summary>
/// VR手速控制系统 - 通过检测双手移动速度来控制玩家移动
/// 支持两种模式：幅值驱动和节奏驱动（步频*幅值）
/// </summary>
public class VRHandSpeed : MonoBehaviour
{
    #region 组件引用
    [Header("手部Transform引用")]
    public Transform leftHand;
    public Transform rightHand;
    
    [Header("测试模式")]
    public bool enableTestMode = true;
    public bool showTestUI = true;
    public bool logSpeedOutput = true; // 是否输出速度日志
    #endregion

    #region 基础参数
    [Header("基础参数")]
    [Tooltip("EMA滤波系数，值越大响应越快，推荐0.2-0.5")]
    [Range(0.1f, 0.9f)]
    public float emaAlpha = 0.3f;

    [Tooltip("手速死区，低于此值不触发移动，推荐0.05-0.2 m/s")]
    [Range(0.01f, 0.5f)]
    public float speedDeadZone = 0.1f;

    [Tooltip("速度缩放系数，控制手速到移动速度的映射比例")]
    [Range(0.5f, 5f)]
    public float speedMultiplier = 2f;

    [Tooltip("最大移动速度限制")]
    [Range(1f, 10f)]
    public float maxMoveSpeed = 5f;

    [Tooltip("最小持续时间窗口，手速超过阈值后持续此时间才生效")]
    [Range(0.05f, 0.5f)]
    public float minActiveTime = 0.15f;

    [Tooltip("速度衰减率，手停止时玩家速度缓和回0")]
    [Range(1f, 10f)]
    public float decelerationRate = 5f;
    #endregion

    #region 模式选择
    [Header("检测模式")]
    [Tooltip("使用幅值驱动模式（简单）")]
    public bool useAmplitudeMode = true;

    [Tooltip("使用节奏驱动模式（步频*幅值）")]
    public bool useRhythmMode = false;

    [Tooltip("使用相位交替检测增强")]
    public bool usePhaseDetection = false;
    #endregion

    #region 节奏模式参数
    [Header("节奏模式参数")]
    [Tooltip("峰值检测阈值，超过此值认为是有效峰值")]
    [Range(0.1f, 2f)]
    public float peakThreshold = 0.3f;

    [Tooltip("步频权重系数")]
    [Range(0.5f, 3f)]
    public float frequencyWeight = 1.5f;

    [Tooltip("幅值权重系数")]
    [Range(0.5f, 3f)]
    public float amplitudeWeight = 1f;

    [Tooltip("相位交替加成系数")]
    [Range(1f, 1.5f)]
    public float phaseBonus = 1.2f;
    #endregion

    #region 调试参数
    [Header("调试参数")]
    public bool enableDebugLog = false;
    public bool showDebugInfo = true;
    public float nowSpeed;
    #endregion

    #region 私有变量 - 速度计算
    // 位置记录（使用local position）
    private Vector3 lastLeftHandLocalPos;
    private Vector3 lastRightHandLocalPos;
    private bool isFirstFrame = true;

    // EMA滤波后的速度
    private float smoothedLeftSpeed;
    private float smoothedRightSpeed;

    // 当前计算出的移动速度
    private float currentMoveSpeed = 0f;
    private float targetMoveSpeed = 0f;

    // 激活状态计时
    private float activeTimer = 0f;
    private bool isActive = false;
    #endregion

    #region 私有变量 - 节奏检测
    // 峰值检测
    private float lastLeftSpeed = 0f;
    private float lastRightSpeed = 0f;
    private bool leftPeakDetected = false;
    private bool rightPeakDetected = false;

    // 步频计算
    private float leftPeakTime = 0f;
    private float rightPeakTime = 0f;
    private float leftFrequency = 0f;
    private float rightFrequency = 0f;
    private float leftAmplitude = 0f;
    private float rightAmplitude = 0f;

    // 相位检测
    private float lastPeakTime = 0f;
    private bool lastPeakWasLeft = false;
    private bool isAlternating = false;

    // 指定接口
    private IMoveSpeedProvider moveSpeedProvider;
    #endregion

    #region Unity生命周期
    void Start()
    {
        // 初始化位置记录（使用local position）
        if (leftHand != null)
            lastLeftHandLocalPos = leftHand.localPosition;
        if (rightHand != null)
            lastRightHandLocalPos = rightHand.localPosition;

        // 测试模式初始化
        if (enableTestMode)
        {
            Debug.Log("VR手速控制系统已启动 - 测试模式");
        }

        moveSpeedProvider = GetComponent<IMoveSpeedProvider>();
    }

    void Update()
    {
        if (leftHand == null || rightHand == null)
            return;

        // 计算手速
        CalculateHandSpeeds();

        // 应用EMA滤波
        ApplyEMAFilter();

        // 根据模式计算目标移动速度
        if (useAmplitudeMode)
        {
            CalculateAmplitudeModeSpeed();
        }
        else if (useRhythmMode)
        {
            CalculateRhythmModeSpeed();
        }

        // 应用激活时间窗口
        ApplyActiveTimeWindow();

        // 平滑过渡到目标速度
        SmoothSpeedTransition();
        
        // 更新公开的速度变量
        nowSpeed = currentMoveSpeed;

        // 通过接口将速度传递给VRPlayerMove
        if (moveSpeedProvider != null)
        {
            moveSpeedProvider.SetAdditionalMoveSpeed(currentMoveSpeed);
        }

        // 输出速度日志
        if (logSpeedOutput)
        {
            LogSpeedOutput();
        }

        // 调试信息
        if (showDebugInfo)
            UpdateDebugInfo();
    }
    #endregion

    #region 速度计算
    /// <summary>
    /// 计算双手的瞬时速度
    /// </summary>
    void CalculateHandSpeeds()
    {
        if (isFirstFrame)
        {
            lastLeftHandLocalPos = leftHand.localPosition;
            lastRightHandLocalPos = rightHand.localPosition;
            isFirstFrame = false;
            return;
        }

        float deltaTime = Time.deltaTime;

        // 计算左手速度（位置差分法，使用local position）
        Vector3 leftDelta = leftHand.localPosition - lastLeftHandLocalPos;
        float leftSpeed = leftDelta.magnitude / deltaTime;

        // 计算右手速度（位置差分法，使用local position）
        Vector3 rightDelta = rightHand.localPosition - lastRightHandLocalPos;
        float rightSpeed = rightDelta.magnitude / deltaTime;

        // 更新位置记录
        lastLeftHandLocalPos = leftHand.localPosition;
        lastRightHandLocalPos = rightHand.localPosition;

        // 保存原始速度用于峰值检测
        lastLeftSpeed = leftSpeed;
        lastRightSpeed = rightSpeed;

        if (enableDebugLog)
        {
            Debug.Log($"左手瞬时速度: {leftSpeed:F3} m/s, 右手瞬时速度: {rightSpeed:F3} m/s");
        }
    }

    /// <summary>
    /// 应用EMA滤波平滑速度数据
    /// </summary>
    void ApplyEMAFilter()
    {
        // EMA滤波公式: s(t) = α * v(t) + (1-α) * s(t-1)
        smoothedLeftSpeed = emaAlpha * lastLeftSpeed + (1f - emaAlpha) * smoothedLeftSpeed;
        smoothedRightSpeed = emaAlpha * lastRightSpeed + (1f - emaAlpha) * smoothedRightSpeed;

        if (enableDebugLog)
        {
            Debug.Log($"滤波后左手速度: {smoothedLeftSpeed:F3} m/s, 滤波后右手速度: {smoothedRightSpeed:F3} m/s");
        }
    }
    #endregion

    #region 幅值驱动模式
    /// <summary>
    /// 计算幅值驱动模式下的目标速度
    /// </summary>
    void CalculateAmplitudeModeSpeed()
    {
        // 合并左右手速度（取平均值）
        float combinedSpeed = (smoothedLeftSpeed + smoothedRightSpeed) * 0.5f;

        // 应用死区
        if (combinedSpeed > speedDeadZone)
        {
            // 映射到移动速度
            targetMoveSpeed = (combinedSpeed - speedDeadZone) * speedMultiplier;
            targetMoveSpeed = Mathf.Min(targetMoveSpeed, maxMoveSpeed);
        }
        else
        {
            targetMoveSpeed = 0f;
        }

        if (enableDebugLog)
        {
            Debug.Log($"幅值模式 - 合并速度: {combinedSpeed:F3} m/s, 目标移动速度: {targetMoveSpeed:F3} m/s");
        }
    }
    #endregion

    #region 节奏驱动模式
    /// <summary>
    /// 计算节奏驱动模式下的目标速度
    /// </summary>
    void CalculateRhythmModeSpeed()
    {
        // 检测峰值
        DetectPeaks();

        // 计算步频和幅值
        CalculateFrequencyAndAmplitude();

        // 检测相位交替
        if (usePhaseDetection)
        {
            DetectPhaseAlternation();
        }

        // 计算节奏速度: speed = k_freq * f * A + k_amp * A
        float avgFrequency = (leftFrequency + rightFrequency) * 0.5f;
        float avgAmplitude = (leftAmplitude + rightAmplitude) * 0.5f;

        float rhythmSpeed = frequencyWeight * avgFrequency * avgAmplitude + amplitudeWeight * avgAmplitude;

        // 应用相位加成
        if (isAlternating && usePhaseDetection)
        {
            rhythmSpeed *= phaseBonus;
        }

        // 应用死区和限制
        if (rhythmSpeed > speedDeadZone)
        {
            targetMoveSpeed = Mathf.Min(rhythmSpeed, maxMoveSpeed);
        }
        else
        {
            targetMoveSpeed = 0f;
        }

        if (enableDebugLog)
        {
            Debug.Log($"节奏模式 - 步频: {avgFrequency:F2} Hz, 幅值: {avgAmplitude:F3} m/s, 交替: {isAlternating}, 目标速度: {targetMoveSpeed:F3} m/s");
        }
    }

    /// <summary>
    /// 检测速度峰值
    /// </summary>
    void DetectPeaks()
    {
        float currentTime = Time.time;

        // 左手峰值检测
        if (lastLeftSpeed > peakThreshold && smoothedLeftSpeed > peakThreshold)
        {
            if (!leftPeakDetected && smoothedLeftSpeed > lastLeftSpeed)
            {
                leftPeakDetected = true;
                leftPeakTime = currentTime;
                leftAmplitude = smoothedLeftSpeed;

                // 更新步频
                if (leftPeakTime > 0f)
                {
                    float timeDiff = currentTime - leftPeakTime;
                    if (timeDiff > 0.1f) // 避免除零和过小间隔
                    {
                        leftFrequency = 1f / timeDiff;
                    }
                }

                // 相位检测
                UpdatePhaseDetection(true, currentTime);
            }
        }
        else
        {
            leftPeakDetected = false;
        }

        // 右手峰值检测
        if (lastRightSpeed > peakThreshold && smoothedRightSpeed > peakThreshold)
        {
            if (!rightPeakDetected && smoothedRightSpeed > lastRightSpeed)
            {
                rightPeakDetected = true;
                rightPeakTime = currentTime;
                rightAmplitude = smoothedRightSpeed;

                // 更新步频
                if (rightPeakTime > 0f)
                {
                    float timeDiff = currentTime - rightPeakTime;
                    if (timeDiff > 0.1f) // 避免除零和过小间隔
                    {
                        rightFrequency = 1f / timeDiff;
                    }
                }

                // 相位检测
                UpdatePhaseDetection(false, currentTime);
            }
        }
        else
        {
            rightPeakDetected = false;
        }
    }

    /// <summary>
    /// 计算步频和幅值
    /// </summary>
    void CalculateFrequencyAndAmplitude()
    {
        // 这里可以添加更复杂的步频和幅值计算逻辑
        // 当前使用简单的峰值检测方法
    }

    /// <summary>
    /// 检测相位交替
    /// </summary>
    void UpdatePhaseDetection(bool isLeftPeak, float currentTime)
    {
        if (lastPeakTime > 0f)
        {
            float timeDiff = currentTime - lastPeakTime;
            
            // 检查是否是交替模式（左右手交替出现峰值）
            if (lastPeakWasLeft != isLeftPeak && timeDiff > 0.1f && timeDiff < 1f)
            {
                isAlternating = true;
            }
            else
            {
                isAlternating = false;
            }
        }

        lastPeakTime = currentTime;
        lastPeakWasLeft = isLeftPeak;
    }

    /// <summary>
    /// 检测相位交替状态
    /// </summary>
    void DetectPhaseAlternation()
    {
        // 相位检测逻辑已在UpdatePhaseDetection中实现
    }
    #endregion

    #region 速度应用
    /// <summary>
    /// 应用激活时间窗口
    /// </summary>
    void ApplyActiveTimeWindow()
    {
        if (targetMoveSpeed > 0f)
        {
            activeTimer += Time.deltaTime;
            if (activeTimer >= minActiveTime)
            {
                isActive = true;
            }
        }
        else
        {
            activeTimer = 0f;
            isActive = false;
        }
    }

    /// <summary>
    /// 平滑过渡到目标速度
    /// </summary>
    void SmoothSpeedTransition()
    {
        if (isActive)
        {
            // 激活状态：平滑过渡到目标速度
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, Time.deltaTime * decelerationRate);
        }
        else
        {
            // 非激活状态：快速衰减到0
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * decelerationRate * 2f);
        }
    }

    /// <summary>
    /// 输出速度日志
    /// </summary>
    void LogSpeedOutput()
    {
        // 每秒输出一次速度信息
        if (Time.frameCount % 60 == 0) // 假设60FPS，每秒输出一次
        {
            string mode = useAmplitudeMode ? "幅值模式" : "节奏模式";
            string phaseInfo = usePhaseDetection && isAlternating ? " (交替)" : "";
            
            Debug.Log($"[VR手速] {mode}{phaseInfo} - 左手: {smoothedLeftSpeed:F3}m/s, 右手: {smoothedRightSpeed:F3}m/s, 输出速度: {currentMoveSpeed:F3}m/s");
            
            if (useRhythmMode)
            {
                Debug.Log($"[VR手速] 节奏信息 - 步频: {(leftFrequency + rightFrequency) * 0.5f:F2}Hz, 幅值: {(leftAmplitude + rightAmplitude) * 0.5f:F3}m/s");
            }
        }
    }
    #endregion

    #region 调试信息
    /// <summary>
    /// 更新调试信息
    /// </summary>
    void UpdateDebugInfo()
    {
        if (!showDebugInfo)
            return;

        // 这里可以添加OnGUI显示调试信息
        // 或者使用Unity的Debug.Log输出
    }

    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugInfo || leftHand == null || rightHand == null)
            return;

        // 绘制手部速度指示器（使用local position转换为世界坐标）
        Gizmos.color = Color.cyan;
        Vector3 leftWorldPos = leftHand.TransformPoint(leftHand.localPosition);
        Vector3 rightWorldPos = rightHand.TransformPoint(rightHand.localPosition);
        Gizmos.DrawLine(leftWorldPos, leftWorldPos + Vector3.up * smoothedLeftSpeed);
        Gizmos.DrawLine(rightWorldPos, rightWorldPos + Vector3.up * smoothedRightSpeed);

        // 绘制移动速度指示器（从当前位置向前）
        if (enableTestMode)
        {
            Gizmos.color = Color.green;
            Vector3 moveDir = transform.forward;
            moveDir.y = 0;
            Gizmos.DrawLine(transform.position, transform.position + moveDir.normalized * currentMoveSpeed);
        }
    }

    /// <summary>
    /// 绘制测试UI
    /// </summary>
    void OnGUI()
    {
        if (!showTestUI || !enableTestMode)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("VR手速控制系统 - 测试界面", GUI.skin.box);
        
        GUILayout.Space(10);
        GUILayout.Label($"左手速度: {smoothedLeftSpeed:F3} m/s");
        GUILayout.Label($"右手速度: {smoothedRightSpeed:F3} m/s");
        GUILayout.Label($"当前移动速度: {currentMoveSpeed:F3} m/s");
        GUILayout.Label($"目标移动速度: {targetMoveSpeed:F3} m/s");
        GUILayout.Label($"激活状态: {(isActive ? "是" : "否")}");
        
        GUILayout.Space(10);
        GUILayout.Label("模式控制:");
        if (GUILayout.Button(useAmplitudeMode ? "幅值模式 (当前)" : "切换到幅值模式"))
        {
            SwitchToAmplitudeMode();
        }
        if (GUILayout.Button(useRhythmMode ? "节奏模式 (当前)" : "切换到节奏模式"))
        {
            SwitchToRhythmMode();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("参数调节:");
        GUILayout.Label($"EMA系数: {emaAlpha:F2}");
        emaAlpha = GUILayout.HorizontalSlider(emaAlpha, 0.1f, 0.9f);
        
        GUILayout.Label($"死区: {speedDeadZone:F2} m/s");
        speedDeadZone = GUILayout.HorizontalSlider(speedDeadZone, 0.01f, 0.5f);
        
        GUILayout.Label($"速度倍数: {speedMultiplier:F2}");
        speedMultiplier = GUILayout.HorizontalSlider(speedMultiplier, 0.5f, 5f);
        
        GUILayout.Space(10);
        if (GUILayout.Button("重置系统"))
        {
            Reset();
        }
        
        GUILayout.EndArea();
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取当前计算出的移动速度
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        return currentMoveSpeed;
    }

    /// <summary>
    /// 获取左手当前速度
    /// </summary>
    public float GetLeftHandSpeed()
    {
        return smoothedLeftSpeed;
    }

    /// <summary>
    /// 获取右手当前速度
    /// </summary>
    public float GetRightHandSpeed()
    {
        return smoothedRightSpeed;
    }

    /// <summary>
    /// 切换到幅值模式
    /// </summary>
    public void SwitchToAmplitudeMode()
    {
        useAmplitudeMode = true;
        useRhythmMode = false;
    }

    /// <summary>
    /// 切换到节奏模式
    /// </summary>
    public void SwitchToRhythmMode()
    {
        useAmplitudeMode = false;
        useRhythmMode = true;
    }

    /// <summary>
    /// 重置所有状态
    /// </summary>
    public void Reset()
    {
        smoothedLeftSpeed = 0f;
        smoothedRightSpeed = 0f;
        currentMoveSpeed = 0f;
        targetMoveSpeed = 0f;
        activeTimer = 0f;
        isActive = false;
        isFirstFrame = true;
        
        // 重置位置记录（使用local position）
        if (leftHand != null)
            lastLeftHandLocalPos = leftHand.localPosition;
        if (rightHand != null)
            lastRightHandLocalPos = rightHand.localPosition;
        
        // 重置节奏检测变量
        leftPeakDetected = false;
        rightPeakDetected = false;
        leftFrequency = 0f;
        rightFrequency = 0f;
        leftAmplitude = 0f;
        rightAmplitude = 0f;
        isAlternating = false;
    }

    #endregion
}
