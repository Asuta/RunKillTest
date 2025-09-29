using UnityEngine;

public class Enemy : MonoBehaviour, ICanBeHit
{
    public int health = 100;
    public Transform target;
    public Transform lineStart;
    
    // 用于绘制的材质和网格
    public Material lineMaterial;
    public Material sphereMaterial;
    private Mesh sphereMesh;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Enemy took {damage} damage, remaining health: {health}");
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died!");
        // 在这里添加敌人死亡的逻辑，比如播放动画、掉落物品等
        Destroy(gameObject);
    }

    public void SetTarget( Transform PlayerHead)
    {
        target = PlayerHead;
    }

    private void Start()
    {
        // 创建球体网格
        sphereMesh = CreateSphereMesh(0.1f);
    }

    private void Update()
    {
        // 如果target不为null，绘制线和球
        if (target != null && lineStart != null)
        {
            DrawTargetVisualization();
        }
    }

    private void DrawTargetVisualization()
    {
        // 计算终点位置（target位置减1）
        Vector3 targetPosition = target.position;
        targetPosition = targetPosition + Vector3.up * -0.5f; // 降低一些高度，避免与视线重合
        Vector3 direction = (targetPosition - lineStart.position).normalized;
        Vector3 endPoint = targetPosition - direction * 1f; // 减1个单位

        // 绘制线
        DrawLine(lineStart.position, endPoint);

        // 绘制球
        DrawSphere(endPoint);
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        if (lineMaterial == null) return;

        // 创建线的网格
        Mesh lineMesh = CreateLineMesh(start, end, 0.02f);
        Graphics.DrawMesh(lineMesh, Vector3.zero, Quaternion.identity, lineMaterial, 0);
    }

    private void DrawSphere(Vector3 position)
    {
        if (sphereMaterial == null || sphereMesh == null) return;

        Graphics.DrawMesh(sphereMesh, position, Quaternion.identity, sphereMaterial, 0);
    }

    private Mesh CreateLineMesh(Vector3 start, Vector3 end, float thickness)
    {
        Mesh mesh = new Mesh();
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * thickness;

        Vector3[] vertices = new Vector3[4]
        {
            start - perpendicular,
            start + perpendicular,
            end + perpendicular,
            end - perpendicular
        };

        int[] triangles = new int[6] { 0, 1, 2, 0, 2, 3 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    private Mesh CreateSphereMesh(float radius)
    {
        // 创建一个简单的球体网格
        // 这里使用Unity内置的球体，但为了简单起见，我们创建一个简单的八面体
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[6]
        {
            new Vector3(0, radius, 0),
            new Vector3(0, -radius, 0),
            new Vector3(radius, 0, 0),
            new Vector3(-radius, 0, 0),
            new Vector3(0, 0, radius),
            new Vector3(0, 0, -radius)
        };

        int[] triangles = new int[24]
        {
            0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2,
            1, 4, 2, 1, 2, 5, 1, 5, 3, 1, 3, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }
}