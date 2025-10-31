using UnityEngine;

/// <summary>
/// VR手速系统测试脚本
/// 用于模拟手部移动并测试VRHandSpeed系统
/// </summary>
public class VRHandSpeedTest : MonoBehaviour
{
    [Header("测试设置")]
    public VRHandSpeed handSpeedSystem;
    public bool enableAutoTest = false;
    public float testSpeed = 1f;
    public float testFrequency = 2f; // Hz
    
    [Header("模拟手部Transform")]
    public Transform simulatedLeftHand;
    public Transform simulatedRightHand;
    
    [Header("测试模式")]
    public TestMode currentTestMode = TestMode.Amplitude;
    public enum TestMode { Amplitude, Rhythm, Alternating }
    
    private float testTimer = 0f;
    private Vector3 leftHandStartPos;
    private Vector3 rightHandStartPos;
    
    void Start()
    {
        // 如果没有设置手部Transform，创建模拟手部
        if (simulatedLeftHand == null)
        {
            CreateSimulatedHands();
        }
        
        // 如果没有设置VRHandSpeed系统，尝试获取
        if (handSpeedSystem == null)
        {
            handSpeedSystem = FindFirstObjectByType<VRHandSpeed>();
        }
        
        // 设置VRHandSpeed系统的手部引用
        if (handSpeedSystem != null)
        {
            handSpeedSystem.leftHand = simulatedLeftHand;
            handSpeedSystem.rightHand = simulatedRightHand;
            handSpeedSystem.logSpeedOutput = true; // 启用速度日志输出
        }
        
        // 记录起始位置
        if (simulatedLeftHand != null)
            leftHandStartPos = simulatedLeftHand.position;
        if (simulatedRightHand != null)
            rightHandStartPos = simulatedRightHand.position;
    }
    
    void Update()
    {
        if (!enableAutoTest || handSpeedSystem == null)
            return;
            
        testTimer += Time.deltaTime;
        
        switch (currentTestMode)
        {
            case TestMode.Amplitude:
                TestAmplitudeMode();
                break;
            case TestMode.Rhythm:
                TestRhythmMode();
                break;
            case TestMode.Alternating:
                TestAlternatingMode();
                break;
        }
    }
    
    /// <summary>
    /// 测试幅值模式 - 同时移动双手
    /// </summary>
    void TestAmplitudeMode()
    {
        if (simulatedLeftHand == null || simulatedRightHand == null)
            return;
            
        // 简单的上下移动
        float offset = Mathf.Sin(testTimer * testSpeed * 2f) * 0.2f;
        
        simulatedLeftHand.position = leftHandStartPos + Vector3.up * offset;
        simulatedRightHand.position = rightHandStartPos + Vector3.up * offset;
    }
    
    /// <summary>
    /// 测试节奏模式 - 有节奏的移动
    /// </summary>
    void TestRhythmMode()
    {
        if (simulatedLeftHand == null || simulatedRightHand == null)
            return;
            
        // 有节奏的上下移动
        float leftOffset = Mathf.Sin(testTimer * testFrequency * 2f * Mathf.PI) * 0.3f;
        float rightOffset = Mathf.Sin(testTimer * testFrequency * 2f * Mathf.PI) * 0.3f;
        
        simulatedLeftHand.position = leftHandStartPos + Vector3.up * leftOffset;
        simulatedRightHand.position = rightHandStartPos + Vector3.up * rightOffset;
    }
    
    /// <summary>
    /// 测试交替模式 - 左右手交替移动
    /// </summary>
    void TestAlternatingMode()
    {
        if (simulatedLeftHand == null || simulatedRightHand == null)
            return;
            
        // 左右手交替上下移动
        float leftOffset = Mathf.Sin(testTimer * testFrequency * 2f * Mathf.PI) * 0.3f;
        float rightOffset = Mathf.Sin(testTimer * testFrequency * 2f * Mathf.PI + Mathf.PI) * 0.3f; // 相位差180度
        
        simulatedLeftHand.position = leftHandStartPos + Vector3.up * leftOffset;
        simulatedRightHand.position = rightHandStartPos + Vector3.up * rightOffset;
    }
    
    /// <summary>
    /// 创建模拟手部Transform
    /// </summary>
    void CreateSimulatedHands()
    {
        // 创建左手
        GameObject leftHandObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftHandObj.name = "SimulatedLeftHand";
        leftHandObj.transform.position = transform.position + Vector3.left * 0.5f;
        leftHandObj.transform.localScale = Vector3.one * 0.1f;
        
        Renderer leftRenderer = leftHandObj.GetComponent<Renderer>();
        if (leftRenderer != null)
        {
            leftRenderer.material.color = Color.red;
        }
        
        simulatedLeftHand = leftHandObj.transform;
        
        // 创建右手
        GameObject rightHandObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightHandObj.name = "SimulatedRightHand";
        rightHandObj.transform.position = transform.position + Vector3.right * 0.5f;
        rightHandObj.transform.localScale = Vector3.one * 0.1f;
        
        Renderer rightRenderer = rightHandObj.GetComponent<Renderer>();
        if (rightRenderer != null)
        {
            rightRenderer.material.color = Color.blue;
        }
        
        simulatedRightHand = rightHandObj.transform;
    }
    
    /// <summary>
    /// 绘制测试UI
    /// </summary>
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 350));
        GUILayout.Label("VR手速系统测试", GUI.skin.box);
        
        GUILayout.Space(10);
        GUILayout.Label($"测试模式: {currentTestMode}");
        
        GUILayout.Space(10);
        enableAutoTest = GUILayout.Toggle(enableAutoTest, "启用自动测试");
        
        GUILayout.Space(10);
        GUILayout.Label("测试参数:");
        GUILayout.Label($"测试速度: {testSpeed:F2}");
        testSpeed = GUILayout.HorizontalSlider(testSpeed, 0.1f, 3f);
        
        GUILayout.Label($"测试频率: {testFrequency:F2} Hz");
        testFrequency = GUILayout.HorizontalSlider(testFrequency, 0.5f, 5f);
        
        GUILayout.Space(10);
        GUILayout.Label("测试模式选择:");
        if (GUILayout.Button("幅值模式"))
        {
            currentTestMode = TestMode.Amplitude;
            if (handSpeedSystem != null)
                handSpeedSystem.SwitchToAmplitudeMode();
        }
        if (GUILayout.Button("节奏模式"))
        {
            currentTestMode = TestMode.Rhythm;
            if (handSpeedSystem != null)
                handSpeedSystem.SwitchToRhythmMode();
        }
        if (GUILayout.Button("交替模式"))
        {
            currentTestMode = TestMode.Alternating;
            if (handSpeedSystem != null)
                handSpeedSystem.SwitchToRhythmMode();
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("重置系统"))
        {
            if (handSpeedSystem != null)
                handSpeedSystem.Reset();
            testTimer = 0f;
        }
        
        GUILayout.Space(10);
        GUILayout.Label("说明:");
        GUILayout.Label("• 红球: 左手");
        GUILayout.Label("• 蓝球: 右手");
        GUILayout.Label("• 查看Console获取速度输出");
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (simulatedLeftHand == null || simulatedRightHand == null)
            return;
            
        // 绘制起始位置
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(leftHandStartPos, 0.05f);
        Gizmos.DrawWireSphere(rightHandStartPos, 0.05f);
        
        // 绘制当前位置到起始位置的连线
        Gizmos.color = Color.red;
        Gizmos.DrawLine(leftHandStartPos, simulatedLeftHand.position);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(rightHandStartPos, simulatedRightHand.position);
    }
}