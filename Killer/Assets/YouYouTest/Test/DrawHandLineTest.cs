using UnityEngine;

public class DrawHandLineTest : MonoBehaviour
{
    public Transform handT;
    public Transform parentT; // 第一级父物体引用
    public bool startDraw;
    private bool wasStartDraw = false;
    private Vector3 startLocalPosition;
    private Mesh lineMesh;
    private Material lineMaterial;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 创建简单的线段网格
        lineMesh = CreateLineMesh();
        
        // 创建基础材质
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        // 检测startDraw从false变为true的瞬间
        if (startDraw && !wasStartDraw)
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
        
        // 如果startDraw为true，绘制线段
        if (startDraw && handT != null)
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
        
        // 更新状态
        wasStartDraw = startDraw;
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
