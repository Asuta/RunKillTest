using UnityEngine;

public class VRHandPullHook : MonoBehaviour
{
    public Transform handT;

    public IHookDashProvider hookDashProvider;

    private bool isRecording = false;
    private Vector3 recordedPosition;
    private bool hasTriggered = false; // 标记是否已经触发过距离条件
    
    // 画线相关
    private Material lineMaterial;
    private Mesh lineMesh;
    private bool shouldDrawLine = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hookDashProvider = GetComponent<IHookDashProvider>();
        
        // 初始化画线相关
        InitializeLineDrawing();
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
    
    void InitializeLineDrawing()
    {
        // 创建材质
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = Color.yellow;
        
        // 创建网格
        lineMesh = new Mesh();
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
        
        // 创建线条的顶点
        Vector3[] vertices = new Vector3[2];
        vertices[0] = handPosition;
        vertices[1] = hookPosition;
        
        // 创建线条的索引
        int[] indices = new int[2];
        indices[0] = 0;
        indices[1] = 1;
        
        // 更新网格
        lineMesh.Clear();
        lineMesh.vertices = vertices;
        lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
        
        // 绘制网格
        Graphics.DrawMesh(lineMesh, Vector3.zero, Quaternion.identity, lineMaterial, 0);
    }
    
    void OnDestroy()
    {
        // 清理资源
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
        
        if (lineMesh != null)
        {
            Destroy(lineMesh);
        }
    }
}
