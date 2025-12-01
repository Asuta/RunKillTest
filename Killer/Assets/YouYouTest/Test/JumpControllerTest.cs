using UnityEngine;

public class JumpControllerTest : MonoBehaviour
{
    public Transform handT;
    public Transform parentT; // 第一级父物体引用
    private bool isDrawing = false;
    private bool wasDrawing = false;
    private Vector3 startLocalPosition;
    private Mesh lineMesh;
    private Material lineMaterial;
    public Rigidbody thisRb;
    public NewVRMove vRMove;
    public float forceMultiplier = 10f; // 力的倍数，用于调整施加的力的大小
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 创建简单的线段网格
        lineMesh = CreateLineMesh();
        
        // 创建基础材质
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = Color.red;

        // 初始化刚体引用
        if (thisRb == null)
        {
            thisRb = GetComponent<Rigidbody>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 检测右手扳机键状态
        float rightTrigger = InputActionsManager.Actions.XRIRightInteraction.ActivateValue.ReadValue<float>();
        isDrawing = rightTrigger > 0.1f;
        
        // 检测从非绘制状态变为绘制状态的瞬间
        if (isDrawing && !wasDrawing)
        {
            // 记录起始时相对于第一级父体的本地位置
            if (parentT != null)
            {
                startLocalPosition = parentT.InverseTransformPoint(handT.position);
            }
            else
            {
                // 如果没有指定父物体，使用世界坐标作为本地坐标
                startLocalPosition = handT.position;
            }
        }
        
        // 如果正在绘制，绘制线段
        if (isDrawing && handT != null)
        {
            Vector3 startWorldPosition;
            
            // 根据保存的本地位置计算当前的世界位置
            if (parentT != null)
            {
                startWorldPosition = parentT.TransformPoint(startLocalPosition);
            }
            else
            {
                // 如果没有指定父物体，直接使用保存的位置
                startWorldPosition = startLocalPosition;
            }
            
            // 获取当前的世界位置
            Vector3 currentWorldPosition = handT.position;
            
            // 更新线段网格的顶点（使用世界坐标）
            UpdateLineMesh(startWorldPosition, currentWorldPosition);
            
            // 绘制线段
            Graphics.DrawMesh(lineMesh, Vector3.zero, Quaternion.identity, lineMaterial, 0);
        }
        
        // 检测从绘制状态变为非绘制状态的瞬间（手松开）
        if (!isDrawing && wasDrawing)
        {
            // 计算线段的向量（从起点到终点）
            Vector3 startWorldPosition;
            
            // 根据保存的本地位置计算当前的世界位置
            if (parentT != null)
            {
                startWorldPosition = parentT.TransformPoint(startLocalPosition);
            }
            else
            {
                // 如果没有指定父物体，直接使用保存的位置
                startWorldPosition = startLocalPosition;
            }
            
            // 获取当前的世界位置
            Vector3 currentWorldPosition = handT.position;
            
            // 计算线段的方向和长度
            Vector3 lineDirection = currentWorldPosition - startWorldPosition;
            
            // 给刚体施加一个线性力（使用线段的向量作为力的方向和大小）
            if (thisRb != null)
            {
                // 可以根据需要调整力的倍数
                thisRb.AddForce(lineDirection * -forceMultiplier, ForceMode.Impulse);
                Debug.Log("手松开，给刚体施加线性力: " + lineDirection * forceMultiplier);
            }
        }
        
        // 更新状态
        wasDrawing = isDrawing;
    }
    
    private Mesh CreateLineMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "LineMesh";
        
        // 初始化顶点和索引
        Vector3[] vertices = new Vector3[2];
        int[] indices = new int[2] { 0, 1 };
        
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        
        return mesh;
    }
    
    private void UpdateLineMesh(Vector3 start, Vector3 end)
    {
        if (lineMesh != null)
        {
            Vector3[] vertices = new Vector3[2] { start, end };
            lineMesh.vertices = vertices;
            lineMesh.RecalculateBounds();
        }
    }
    
    private void OnDestroy()
    {
        if (lineMesh != null)
        {
            Destroy(lineMesh);
        }
        
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
