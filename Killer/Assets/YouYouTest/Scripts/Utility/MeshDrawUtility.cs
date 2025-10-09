using UnityEngine;

public static class MeshDrawUtility
{
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