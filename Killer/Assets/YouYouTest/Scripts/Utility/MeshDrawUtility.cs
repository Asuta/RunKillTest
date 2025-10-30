using UnityEngine;
using System.Collections.Generic;

public static class MeshDrawUtility
{
    // 材质缓存
    private static Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    private static Mesh lineMesh;
    /// <summary>
    /// 创建线网格
    /// </summary>
    /// <param name="start">起点位置</param>
    /// <param name="end">终点位置</param>
    /// <param name="thickness">线厚度</param>
    /// <returns>创建的线网格</returns>
    public static Mesh CreateLineMesh(Vector3 start, Vector3 end, float thickness)
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

    /// <summary>
    /// 创建球体网格
    /// </summary>
    /// <param name="radius">球体半径</param>
    /// <returns>创建的球体网格</returns>
    public static Mesh CreateSphereMesh(float radius)
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

    /// <summary>
    /// 绘制线
    /// </summary>
    /// <param name="start">起点位置</param>
    /// <param name="end">终点位置</param>
    /// <param name="material">线材质</param>
    /// <param name="thickness">线厚度</param>
    public static void DrawLine(Vector3 start, Vector3 end, Material material, float thickness = 0.02f)
    {
        if (material == null) return;

        // 创建线的网格
        Mesh lineMesh = CreateLineMesh(start, end, thickness);
        Graphics.DrawMesh(lineMesh, Vector3.zero, Quaternion.identity, material, 0);
    }

    /// <summary>
    /// 在两点之间绘制简单线条（使用MeshTopology.Lines）
    /// </summary>
    /// <param name="startPoint">起点</param>
    /// <param name="endPoint">终点</param>
    /// <param name="color">线条颜色</param>
    /// <param name="layer">渲染层级</param>
    public static void DrawSimpleLine(Vector3 startPoint, Vector3 endPoint, Color color, int layer = 0)
    {
        // 获取或创建材质
        Material material = GetMaterial(color);
        
        // 获取或创建线条网格
        Mesh mesh = GetSimpleLineMesh();
        
        // 创建线条的顶点
        Vector3[] vertices = new Vector3[2];
        vertices[0] = startPoint;
        vertices[1] = endPoint;
        
        // 创建线条的索引
        int[] indices = new int[2];
        indices[0] = 0;
        indices[1] = 1;
        
        // 更新网格
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        
        // 绘制网格
        Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, layer);
    }
    
    /// <summary>
    /// 在两点之间绘制黄色线条
    /// </summary>
    /// <param name="startPoint">起点</param>
    /// <param name="endPoint">终点</param>
    /// <param name="layer">渲染层级</param>
    public static void DrawYellowLine(Vector3 startPoint, Vector3 endPoint, int layer = 0)
    {
        DrawSimpleLine(startPoint, endPoint, Color.yellow, layer);
    }
    
    /// <summary>
    /// 在两点之间绘制红色线条
    /// </summary>
    /// <param name="startPoint">起点</param>
    /// <param name="endPoint">终点</param>
    /// <param name="layer">渲染层级</param>
    public static void DrawRedLine(Vector3 startPoint, Vector3 endPoint, int layer = 0)
    {
        DrawSimpleLine(startPoint, endPoint, Color.red, layer);
    }
    
    /// <summary>
    /// 在两点之间绘制绿色线条
    /// </summary>
    /// <param name="startPoint">起点</param>
    /// <param name="endPoint">终点</param>
    /// <param name="layer">渲染层级</param>
    public static void DrawGreenLine(Vector3 startPoint, Vector3 endPoint, int layer = 0)
    {
        DrawSimpleLine(startPoint, endPoint, Color.green, layer);
    }
    
    /// <summary>
    /// 在两点之间绘制蓝色线条
    /// </summary>
    /// <param name="startPoint">起点</param>
    /// <param name="endPoint">终点</param>
    /// <param name="layer">渲染层级</param>
    public static void DrawBlueLine(Vector3 startPoint, Vector3 endPoint, int layer = 0)
    {
        DrawSimpleLine(startPoint, endPoint, Color.blue, layer);
    }
    
    /// <summary>
    /// 获取或创建指定颜色的材质
    /// </summary>
    /// <param name="color">颜色</param>
    /// <returns>材质</returns>
    private static Material GetMaterial(Color color)
    {
        if (materialCache.TryGetValue(color, out Material material))
        {
            return material;
        }
        
        // 创建新材质
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        
        // 缓存材质
        materialCache[color] = material;
        
        return material;
    }
    
    /// <summary>
    /// 获取简单线条网格
    /// </summary>
    /// <returns>线条网格</returns>
    private static Mesh GetSimpleLineMesh()
    {
        if (lineMesh == null)
        {
            lineMesh = new Mesh();
        }
        
        return lineMesh;
    }
    
    /// <summary>
    /// 清理所有缓存的资源
    /// </summary>
    public static void Cleanup()
    {
        // 清理材质
        foreach (var material in materialCache.Values)
        {
            if (material != null)
            {
                Object.Destroy(material);
            }
        }
        materialCache.Clear();
        
        // 清理网格
        if (lineMesh != null)
        {
            Object.Destroy(lineMesh);
            lineMesh = null;
        }
    }

    /// <summary>
    /// 绘制球体
    /// </summary>
    /// <param name="position">球体位置</param>
    /// <param name="material">球体材质</param>
    /// <param name="mesh">球体网格</param>
    public static void DrawSphere(Vector3 position, Material material, Mesh mesh)
    {
        if (material == null || mesh == null) return;

        Graphics.DrawMesh(mesh, position, Quaternion.identity, material, 0);
    }
}