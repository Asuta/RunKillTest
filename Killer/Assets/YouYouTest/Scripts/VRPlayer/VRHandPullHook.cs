using UnityEngine;

public class VRHandPullHook : MonoBehaviour
{
    public Transform handT;

    public IHookDashProvider hookDashProvider;

    private bool isRecording = false;
    private Vector3 recordedPosition;
    private bool hasTriggered = false; // 标记是否已经触发过距离条件
    
    private bool shouldDrawLine = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hookDashProvider = GetComponent<IHookDashProvider>();
    }

    // Update is called once per frame
    void Update()
    {
        // 检测左手扳机键
        float leftTrigger = InputActionsManager.Actions.XRILeftInteraction.ActivateValue.ReadValue<float>();

        // 当左手扳机键按下时，开始记录位置
        if (leftTrigger > 0.1f && !isRecording && !hasTriggered)
        {
            isRecording = true;
            recordedPosition = handT.position;
            Debug.Log("开始记录位置: " + recordedPosition);
        }

        // 如果正在记录，实时计算距离
        if (isRecording)
        {
            float distance = Vector3.Distance(handT.position, recordedPosition);
            Debug.Log("当前距离: " + distance);

            // 如果距离大于0.3，结束计算并log hahaha
            if (distance > 0.3f)
            {
                isRecording = false;
                hasTriggered = true; // 标记已触发
                Debug.Log("hahaha");
                hookDashProvider.OutHandleHookDash();
            }
        }

        // 当扳机键释放时，重置状态
        if (leftTrigger <= 0.1f)
        {
            if (isRecording)
            {
                isRecording = false;
                Debug.Log("停止记录");
            }
            if (hasTriggered)
            {
                hasTriggered = false; // 重置触发状态
                Debug.Log("重置触发状态");
            }
        }
        
        // 检查是否需要画线
        UpdateLineDrawing();
    }
    
    void UpdateLineDrawing()
    {
        // 检测左手扳机键
        float leftTrigger = InputActionsManager.Actions.XRILeftInteraction.ActivateValue.ReadValue<float>();
        
        // 当按下按键时，检查GameManager中的closestAngleHook
        if (leftTrigger > 0.1f)
        {
            // 获取GameManager实例
            GameManager gameManager = GameManager.Instance;
            
            // 检查是否有最近的钩子
            if (gameManager != null && gameManager.ClosestAngleHook != null)
            {
                shouldDrawLine = true;
                DrawLineBetweenHandAndHook();
            }
            else
            {
                shouldDrawLine = false;
            }
        }
        else
        {
            shouldDrawLine = false;
        }
    }
    
    void DrawLineBetweenHandAndHook()
    {
        if (!shouldDrawLine || handT == null || GameManager.Instance?.ClosestAngleHook == null)
            return;
            
        Vector3 handPosition = handT.position;
        Vector3 hookPosition = GameManager.Instance.ClosestAngleHook.position;
        
        // 使用MeshDrawUtility绘制黄色线条（使用新的简单线条方法）
        MeshDrawUtility.DrawYellowLine(handPosition, hookPosition);
    }
}
