using UnityEngine;

/// <summary>
/// 胶囊线框绘制工具类
/// 提供绘制胶囊体线框的静态方法
/// </summary>
public static class CapsuleWireframeDrawer
{
    // 默认绘制精度
    private const int DefaultSegments = 24;  // 圆周分段数
    private const int DefaultRings = 6;      // 半球环数

    /// <summary>
    /// 绘制CapsuleCast的可视化区域
    /// </summary>
    /// <param name="point1">胶囊体底部点</param>
    /// <param name="point2">胶囊体顶部点</param>
    /// <param name="radius">胶囊体半径</param>
    /// <param name="direction">投射方向</param>
    /// <param name="color">绘制颜色</param>
    public static void DrawCapsuleCastGizmo(Vector3 point1, Vector3 point2, float radius, Vector3 direction, Color color)
    {
        // 绘制起始胶囊体
        DrawWireCapsule(point1, point2, radius, color);
        
        // 绘制结束胶囊体（使用半透明颜色）
        Color endColor = new Color(color.r, color.g, color.b, color.a * 0.6f);
        DrawWireCapsule(point1 + direction, point2 + direction, radius, endColor);
        
        // 计算坐标系
        GetOrthogonalBasis(point2 - point1, out Vector3 up, out Vector3 right, out Vector3 forward);
        
        // 绘制连接线（8个方向，更平滑）
        Color lineColor = new Color(color.r, color.g, color.b, color.a * 0.3f);
        int connectionLines = 8;
        for (int i = 0; i < connectionLines; i++)
        {
            float angle = i * 360f / connectionLines * Mathf.Deg2Rad;
            Vector3 offset = right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
            
            Debug.DrawLine(point1 + offset, point1 + direction + offset, lineColor);
            Debug.DrawLine(point2 + offset, point2 + direction + offset, lineColor);
        }
    }

    /// <summary>
    /// 绘制线框胶囊体
    /// </summary>
    /// <param name="point1">胶囊体底部点</param>
    /// <param name="point2">胶囊体顶部点</param>
    /// <param name="radius">胶囊体半径</param>
    /// <param name="color">绘制颜色</param>
    /// <param name="segments">圆周分段数</param>
    public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius, Color color, int segments = DefaultSegments)
    {
        // 计算坐标系
        GetOrthogonalBasis(point2 - point1, out Vector3 up, out Vector3 right, out Vector3 forward);
        
        // 绘制顶部和底部的半球
        DrawWireHemisphere(point1, -up, right, forward, radius, color, segments);
        DrawWireHemisphere(point2, up, right, forward, radius, color, segments);
        
        // 绘制中间的圆柱部分 - 4条主轴线
        int mainLines = 4;
        for (int i = 0; i < mainLines; i++)
        {
            float angle = i * 360f / mainLines * Mathf.Deg2Rad;
            Vector3 offset = right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
            Debug.DrawLine(point1 + offset, point2 + offset, color);
        }
        
        // 绘制顶部和底部的圆环
        DrawWireCircle(point1, up, right, forward, radius, color, segments);
        DrawWireCircle(point2, up, right, forward, radius, color, segments);
    }

    /// <summary>
    /// 绘制线框圆环
    /// </summary>
    public static void DrawWireCircle(Vector3 center, Vector3 normal, Vector3 right, Vector3 forward, float radius, Color color, int segments = DefaultSegments)
    {
        float angleStep = 360f / segments * Mathf.Deg2Rad;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;
            
            Vector3 p1 = center + right * Mathf.Cos(angle1) * radius + forward * Mathf.Sin(angle1) * radius;
            Vector3 p2 = center + right * Mathf.Cos(angle2) * radius + forward * Mathf.Sin(angle2) * radius;
            
            Debug.DrawLine(p1, p2, color);
        }
    }

    /// <summary>
    /// 绘制线框半球（改进版本，更平滑美观）
    /// </summary>
    /// <param name="center">半球中心点</param>
    /// <param name="normal">半球法线方向</param>
    /// <param name="right">右方向向量</param>
    /// <param name="forward">前方向向量</param>
    /// <param name="radius">半球半径</param>
    /// <param name="color">绘制颜色</param>
    /// <param name="segments">圆周分段数</param>
    /// <param name="rings">纬度环数</param>
    public static void DrawWireHemisphere(Vector3 center, Vector3 normal, Vector3 right, Vector3 forward, 
        float radius, Color color, int segments = DefaultSegments, int rings = DefaultRings)
    {
        // 绘制纬度线（水平环）
        for (int ring = 1; ring <= rings; ring++)
        {
            float phi = ring * (Mathf.PI * 0.5f) / rings;
            float ringRadius = Mathf.Sin(phi) * radius;
            float ringHeight = Mathf.Cos(phi) * radius;
            
            Vector3 ringCenter = center + normal * ringHeight;
            
            float angleStep = 360f / segments * Mathf.Deg2Rad;
            for (int i = 0; i < segments; i++)
            {
                float theta1 = i * angleStep;
                float theta2 = (i + 1) * angleStep;
                
                Vector3 p1 = ringCenter + right * Mathf.Cos(theta1) * ringRadius + forward * Mathf.Sin(theta1) * ringRadius;
                Vector3 p2 = ringCenter + right * Mathf.Cos(theta2) * ringRadius + forward * Mathf.Sin(theta2) * ringRadius;
                
                Debug.DrawLine(p1, p2, color);
            }
        }
        
        // 绘制经度线（垂直弧线）- 4条主弧线
        int meridians = 4;
        for (int m = 0; m < meridians; m++)
        {
            float theta = m * Mathf.PI * 0.5f; // 0, 90, 180, 270度
            
            int arcSegments = rings * 2;
            for (int i = 0; i < arcSegments; i++)
            {
                float phi1 = i * (Mathf.PI * 0.5f) / arcSegments;
                float phi2 = (i + 1) * (Mathf.PI * 0.5f) / arcSegments;
                
                Vector3 p1 = center + normal * Mathf.Cos(phi1) * radius + 
                            (right * Mathf.Cos(theta) + forward * Mathf.Sin(theta)) * Mathf.Sin(phi1) * radius;
                Vector3 p2 = center + normal * Mathf.Cos(phi2) * radius + 
                            (right * Mathf.Cos(theta) + forward * Mathf.Sin(theta)) * Mathf.Sin(phi2) * radius;
                
                Debug.DrawLine(p1, p2, color);
            }
        }
    }

    /// <summary>
    /// 根据给定的方向向量计算正交基
    /// </summary>
    private static void GetOrthogonalBasis(Vector3 direction, out Vector3 up, out Vector3 right, out Vector3 forward)
    {
        up = direction.normalized;
        
        // 选择一个不平行于up的向量来计算叉积
        Vector3 reference = Mathf.Abs(Vector3.Dot(up, Vector3.up)) < 0.99f ? Vector3.up : Vector3.forward;
        
        right = Vector3.Cross(up, reference).normalized;
        forward = Vector3.Cross(right, up).normalized;
    }

    /// <summary>
    /// 绘制简化版胶囊体（性能更好，适合大量绘制）
    /// </summary>
    public static void DrawWireCapsuleSimple(Vector3 point1, Vector3 point2, float radius, Color color)
    {
        DrawWireCapsule(point1, point2, radius, color, 12);
    }

    /// <summary>
    /// 绘制高精度胶囊体（更平滑，适合单个显示）
    /// </summary>
    public static void DrawWireCapsuleHighQuality(Vector3 point1, Vector3 point2, float radius, Color color)
    {
        DrawWireCapsule(point1, point2, radius, color, 36);
    }

    /// <summary>
    /// 使用Gizmos绘制胶囊体（用于OnDrawGizmos）
    /// </summary>
    public static void DrawGizmoCapsule(Vector3 point1, Vector3 point2, float radius, int segments = DefaultSegments)
    {
        GetOrthogonalBasis(point2 - point1, out Vector3 up, out Vector3 right, out Vector3 forward);
        
        // 绘制顶部和底部的半球
        DrawGizmoHemisphere(point1, -up, right, forward, radius, segments);
        DrawGizmoHemisphere(point2, up, right, forward, radius, segments);
        
        // 绘制4条主轴线
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 offset = right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
            Gizmos.DrawLine(point1 + offset, point2 + offset);
        }
        
        // 绘制圆环
        DrawGizmoCircle(point1, right, forward, radius, segments);
        DrawGizmoCircle(point2, right, forward, radius, segments);
    }

    private static void DrawGizmoCircle(Vector3 center, Vector3 right, Vector3 forward, float radius, int segments)
    {
        float angleStep = 360f / segments * Mathf.Deg2Rad;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            Vector3 p1 = center + right * Mathf.Cos(a1) * radius + forward * Mathf.Sin(a1) * radius;
            Vector3 p2 = center + right * Mathf.Cos(a2) * radius + forward * Mathf.Sin(a2) * radius;
            Gizmos.DrawLine(p1, p2);
        }
    }

    private static void DrawGizmoHemisphere(Vector3 center, Vector3 normal, Vector3 right, Vector3 forward, float radius, int segments)
    {
        int rings = DefaultRings;
        
        // 纬度线
        for (int ring = 1; ring <= rings; ring++)
        {
            float phi = ring * (Mathf.PI * 0.5f) / rings;
            float ringRadius = Mathf.Sin(phi) * radius;
            float ringHeight = Mathf.Cos(phi) * radius;
            Vector3 ringCenter = center + normal * ringHeight;
            
            float angleStep = 360f / segments * Mathf.Deg2Rad;
            for (int i = 0; i < segments; i++)
            {
                float t1 = i * angleStep;
                float t2 = (i + 1) * angleStep;
                Vector3 p1 = ringCenter + right * Mathf.Cos(t1) * ringRadius + forward * Mathf.Sin(t1) * ringRadius;
                Vector3 p2 = ringCenter + right * Mathf.Cos(t2) * ringRadius + forward * Mathf.Sin(t2) * ringRadius;
                Gizmos.DrawLine(p1, p2);
            }
        }
        
        // 经度线
        for (int m = 0; m < 4; m++)
        {
            float theta = m * Mathf.PI * 0.5f;
            int arcSegments = rings * 2;
            for (int i = 0; i < arcSegments; i++)
            {
                float phi1 = i * (Mathf.PI * 0.5f) / arcSegments;
                float phi2 = (i + 1) * (Mathf.PI * 0.5f) / arcSegments;
                Vector3 dir = right * Mathf.Cos(theta) + forward * Mathf.Sin(theta);
                Vector3 p1 = center + normal * Mathf.Cos(phi1) * radius + dir * Mathf.Sin(phi1) * radius;
                Vector3 p2 = center + normal * Mathf.Cos(phi2) * radius + dir * Mathf.Sin(phi2) * radius;
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}