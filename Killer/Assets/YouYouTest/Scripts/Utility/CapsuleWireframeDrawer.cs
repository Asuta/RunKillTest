using UnityEngine;

/// <summary>
/// 胶囊线框绘制工具类
/// 提供绘制胶囊体线框的静态方法
/// </summary>
public static class CapsuleWireframeDrawer
{
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
        
        // 绘制结束胶囊体
        DrawWireCapsule(point1 + direction, point2 + direction, radius, color);
        
        // 绘制连接线（胶囊体的边缘）
        Vector3 up = (point2 - point1).normalized;
        Vector3 right = Vector3.Cross(up, Vector3.forward).normalized;
        if (right == Vector3.zero) right = Vector3.Cross(up, Vector3.up).normalized;
        Vector3 forward = Vector3.Cross(up, right).normalized;
        
        // 绘制4个方向的连接线
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 offset = right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
            
            Debug.DrawLine(point1 + offset, point1 + direction + offset, color);
            Debug.DrawLine(point2 + offset, point2 + direction + offset, color);
        }
    }

    /// <summary>
    /// 绘制线框胶囊体
    /// </summary>
    /// <param name="point1">胶囊体底部点</param>
    /// <param name="point2">胶囊体顶部点</param>
    /// <param name="radius">胶囊体半径</param>
    /// <param name="color">绘制颜色</param>
    public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius, Color color)
    {
        Vector3 up = (point2 - point1).normalized;
        Vector3 right = Vector3.Cross(up, Vector3.forward).normalized;
        if (right == Vector3.zero) right = Vector3.Cross(up, Vector3.up).normalized;
        Vector3 forward = Vector3.Cross(up, right).normalized;
        
        // 绘制顶部和底部的半球
        DrawWireHemisphere(point1, -up, right, forward, radius, color);
        DrawWireHemisphere(point2, up, right, forward, radius, color);
        
        // 绘制中间的圆柱部分
        int segments = 12;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * 360f / segments * Mathf.Deg2Rad;
            float angle2 = (i + 1) * 360f / segments * Mathf.Deg2Rad;
            
            Vector3 offset1 = right * Mathf.Cos(angle1) * radius + forward * Mathf.Sin(angle1) * radius;
            Vector3 offset2 = right * Mathf.Cos(angle2) * radius + forward * Mathf.Sin(angle2) * radius;
            
            Debug.DrawLine(point1 + offset1, point2 + offset1, color);
            Debug.DrawLine(point1 + offset1, point1 + offset2, color);
            Debug.DrawLine(point2 + offset1, point2 + offset2, color);
        }
    }

    /// <summary>
    /// 绘制线框半球
    /// </summary>
    /// <param name="center">半球中心点</param>
    /// <param name="normal">半球法线方向</param>
    /// <param name="right">右方向向量</param>
    /// <param name="forward">前方向向量</param>
    /// <param name="radius">半球半径</param>
    /// <param name="color">绘制颜色</param>
    public static void DrawWireHemisphere(Vector3 center, Vector3 normal, Vector3 right, Vector3 forward, float radius, Color color)
    {
        int segments = 12;
        int rings = 3;
        
        for (int ring = 1; ring <= rings; ring++)
        {
            float ringAngle = ring * 90f / rings * Mathf.Deg2Rad;
            float ringRadius = Mathf.Sin(ringAngle) * radius;
            float ringHeight = Mathf.Cos(ringAngle) * radius;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * 360f / segments * Mathf.Deg2Rad;
                float angle2 = (i + 1) * 360f / segments * Mathf.Deg2Rad;
                
                Vector3 point1 = center + normal * ringHeight +
                                right * Mathf.Cos(angle1) * ringRadius +
                                forward * Mathf.Sin(angle1) * ringRadius;
                
                Vector3 point2 = center + normal * ringHeight +
                                right * Mathf.Cos(angle2) * ringRadius +
                                forward * Mathf.Sin(angle2) * ringRadius;
                
                Debug.DrawLine(point1, point2, color);
                
                // 绘制到中心的连接线
                if (i % 3 == 0)
                {
                    Debug.DrawLine(center, point1, color);
                }
            }
        }
    }
}