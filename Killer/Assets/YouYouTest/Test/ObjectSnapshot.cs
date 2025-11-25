using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 快照保存类型枚举
/// </summary>
public enum SnapshotSaveType
{
    TypeA,
    TypeB
}

/// <summary>
/// 对象快照工具类 - 静态工具类，用于对游戏对象进行截图并保存
/// </summary>
public static class ObjectSnapshot
{
    // 默认配置值
    private static readonly Vector3 DefaultCameraDirection = new Vector3(1f, 0.3f, 1f).normalized;
    private const float DefaultZoomFactor = 0.8f;
    private const int DefaultWidth = 512;
    private const int DefaultHeight = 384;
    private static readonly Color DefaultBackgroundColor = new Color(0.23f, 0.72f, 0.73f, 1f);
    private const float DefaultFieldOfView = 60f;

    /// <summary>
    /// 对选中的多个物体进行截图并保存
    /// </summary>
    /// <param name="targets">选中的游戏对象列表</param>
    /// <param name="saveType">保存类型枚举，决定保存到哪个子文件夹</param>
    /// <param name="snapshotLayer">专门用于拍照的Layer</param>
    /// <param name="zoomFactor">缩放因子，数值越小，相机离物体越近，画面越紧凑</param>
    /// <param name="width">截图宽度</param>
    /// <param name="height">截图高度</param>
    /// <param name="backgroundColor">背景颜色</param>
    /// <param name="cameraDirection">拍照时的观察角度</param>
    public static void CaptureAndSave(List<GameObject> targets, SnapshotSaveType saveType, 
        LayerMask snapshotLayer, float zoomFactor = DefaultZoomFactor, 
        int width = DefaultWidth, int height = DefaultHeight, 
        Color? backgroundColor = null, Vector3? cameraDirection = null)
    {
        if (targets == null || targets.Count == 0) return;

        Camera tempCamera = null;
        
        try
        {
            // 0. 创建临时相机
            tempCamera = CreateTemporaryCamera(snapshotLayer, backgroundColor ?? DefaultBackgroundColor);

            // 1. 准备环境：记录物体原本的Layer，并移动到Snapshot Layer
            Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();
            int layerIndex = GetLayerIndexFromMask(snapshotLayer);

            foreach (var go in targets)
            {
                SetLayerRecursively(go.transform, layerIndex, originalLayers);
            }

            // 2. 计算所有物体的合并包围盒 (Bounds)
            Bounds combinedBounds = CalculateBounds(targets);

            // 3. 设置摄像机位置和参数
            SetupCamera(tempCamera, combinedBounds, snapshotLayer, zoomFactor, 
                backgroundColor ?? DefaultBackgroundColor, cameraDirection ?? DefaultCameraDirection);

            // 4. 渲染并保存图片
            Texture2D screenshot = RenderToTexture(tempCamera, width, height);
            byte[] bytes = screenshot.EncodeToPNG();
            
            // 根据枚举类型确定保存路径
            string subFolder = saveType == SnapshotSaveType.TypeA ? "a" : "b";
            string directoryPath = Path.Combine(Application.persistentDataPath, subFolder);
            
            // 确保目录存在
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // 生成文件名（使用时间戳）
            string fileName = $"snapshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string savePath = Path.Combine(directoryPath, fileName);
            
            File.WriteAllBytes(savePath, bytes);

            Debug.Log($"截图已保存至: {savePath}");

            // 5. 清理：恢复物体原本的Layer，销毁临时资源
            foreach (var kvp in originalLayers)
            {
                kvp.Key.gameObject.layer = kvp.Value;
            }
            Object.Destroy(screenshot);
        }
        finally
        {
            // 6. 销毁临时相机
            if (tempCamera != null)
            {
                Object.DestroyImmediate(tempCamera.gameObject);
            }
        }
    }

    // --- 辅助逻辑 ---

    // 递归设置Layer，并记录原始Layer
    private static void SetLayerRecursively(Transform trans, int newLayer, Dictionary<Transform, int> record)
    {
        record[trans] = trans.gameObject.layer;
        trans.gameObject.layer = newLayer;
        foreach (Transform child in trans)
        {
            SetLayerRecursively(child, newLayer, record);
        }
    }

    // 计算所有Renderer的合并包围盒
    private static Bounds CalculateBounds(List<GameObject> targets)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        foreach (var go in targets)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var render in renderers)
            {
                if (!hasBounds)
                {
                    bounds = render.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(render.bounds);
                }
            }
        }
        return bounds;
    }

    // 调整摄像机以适应包围盒
    private static void SetupCamera(Camera camera, Bounds bounds, LayerMask snapshotLayer, 
        float zoomFactor, Color backgroundColor, Vector3 cameraDirection)
    {
        // 基础设置
        camera.enabled = true;
        camera.cullingMask = snapshotLayer;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        
        // 1. 算出物体大概有多大（半径）
        float objectRadius = bounds.extents.magnitude;
        // 2. 设置相机朝向（看向物体中心）
        camera.transform.rotation = Quaternion.LookRotation(-cameraDirection);
        // 3. 根据相机类型计算位置/大小
        if (camera.orthographic)
        {
            // --- 如果是正交相机 (Orthographic) ---
            // 正交相机不靠距离控制大小，而是靠 orthographicSize
            camera.transform.position = bounds.center - camera.transform.forward * 100f; // 放远点无所谓，只要不被裁剪
            camera.orthographicSize = objectRadius * zoomFactor;
        }
        else
        {
            // --- 如果是透视相机 (Perspective) ---
            // 透视相机靠距离控制大小
            float fov = camera.fieldOfView;
            // 核心修改：这里把原来的 1.5f 换成了 zoomFactor，让你能自由控制
            float distance = (objectRadius * zoomFactor) / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
            camera.transform.position = bounds.center - camera.transform.forward * distance;
        }
    }

    // 执行渲染
    private static Texture2D RenderToTexture(Camera camera, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        camera.targetTexture = rt;
        camera.Render();

        // 读取像素
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        // 清理
        camera.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    // 辅助：从LayerMask获取Layer的Index
    private static int GetLayerIndexFromMask(LayerMask mask)
    {
        int layer = 0;
        int maskVal = mask.value;
        while (maskVal > 0)
        {
            if ((maskVal & 1) == 1) return layer;
            maskVal >>= 1;
            layer++;
        }
        return 0;
    }

    /// <summary>
    /// 创建一个临时相机用于截图
    /// </summary>
    /// <param name="snapshotLayer">拍照用的Layer</param>
    /// <param name="backgroundColor">背景颜色</param>
    /// <returns>临时创建的相机</returns>
    private static Camera CreateTemporaryCamera(LayerMask snapshotLayer, Color backgroundColor)
    {
        // 创建一个新的游戏对象作为相机容器
        GameObject cameraGO = new GameObject("TemporarySnapshotCamera");
        
        // 添加相机组件
        Camera camera = cameraGO.AddComponent<Camera>();
        
        // 设置相机基本参数
        camera.enabled = true;
        camera.cullingMask = snapshotLayer;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        camera.orthographic = false; // 默认使用透视相机
        camera.fieldOfView = DefaultFieldOfView; // 默认视野角度
        
        // 确保相机不会渲染到屏幕
        camera.targetTexture = null;
        
        // 设置相机位置为原点，稍后会在SetupCamera中重新设置
        camera.transform.position = Vector3.zero;
        camera.transform.rotation = Quaternion.identity;
        
        return camera;
    }
}
