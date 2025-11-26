using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 快照保存类型枚举
/// </summary>
public enum SnapshotSaveType
{
    TypeSelect,
    TypeLevel
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
    // 防止并发调用导致 layer 恢复被覆盖
    private static readonly object _captureLock = new object();

    /// <summary>
    /// 对选中的多个物体进行截图并保存
    /// </summary>
    /// <param name="targets">选中的游戏对象列表</param>
    /// <param name="saveType">保存类型枚举，决定保存到哪个子文件夹</param>
    /// <param name="name">图片的名称</param>
    /// <param name="zoomFactor">缩放因子，数值越小，相机离物体越近，画面越紧凑</param>
    /// <param name="width">截图宽度</param>
    /// <param name="height">截图高度</param>
    /// <param name="backgroundColor">背景颜色</param>
    /// <param name="cameraDirection">拍照时的观察角度</param>
    public static void CaptureAndSave(List<GameObject> targets, SnapshotSaveType saveType,
        string name, float zoomFactor = DefaultZoomFactor,
        int width = DefaultWidth, int height = DefaultHeight,
        Color? backgroundColor = null, Vector3? cameraDirection = null)
    {
        if (targets == null || targets.Count == 0) return;

        Camera tempCamera = null;
        Texture2D screenshot = null;
        // 将原始 Layer 记录提前声明，以便在 finally 中无论何种异常都能尝试恢复
        Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
        
        try
        {
            lock (_captureLock)
            {
                // 获取Snapshot层的LayerMask
                LayerMask snapshotLayer = LayerMask.GetMask("Snapshot");

                // 检查 Snapshot Layer 是否存在
                int layerIndex = GetLayerIndexFromMask(snapshotLayer);
                if (layerIndex < 0)
                {
                    Debug.LogWarning("ObjectSnapshot: 未找到名为 'Snapshot' 的 Layer，请在 Project Settings -> Tags and Layers 中创建该 Layer。默认将使用 layer 0 进行渲染，可能影响其它对象。");
                    layerIndex = 0;
                }

                // 0. 创建临时相机
                tempCamera = CreateTemporaryCamera(snapshotLayer, backgroundColor ?? DefaultBackgroundColor);

                // 1. 准备环境：记录物体原本的Layer，并移动到Snapshot Layer
                foreach (var go in targets)
                {
                    SetLayerRecursively(go, layerIndex, originalLayers);
                }

                // 2. 计算所有物体的合并包围盒 (Bounds)
                Bounds combinedBounds = CalculateBounds(targets);

                // 3. 设置摄像机位置和参数
                SetupCamera(tempCamera, combinedBounds, snapshotLayer, zoomFactor,
                    backgroundColor ?? DefaultBackgroundColor, cameraDirection ?? DefaultCameraDirection);

                // 4. 渲染并保存图片
                screenshot = RenderToTexture(tempCamera, width, height);
                byte[] bytes = screenshot.EncodeToPNG();

                // 根据枚举类型确定保存路径
                string subFolder = saveType == SnapshotSaveType.TypeSelect ? "SelectImages" : "LevelImages";
                string directoryPath = Path.Combine(Application.persistentDataPath, subFolder);

                // 确保目录存在
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 生成文件名（只使用提供的名称）
                string fileName = $"{name}.png";
                string savePath = Path.Combine(directoryPath, fileName);

                File.WriteAllBytes(savePath, bytes);

                Debug.Log($"截图已保存至: {savePath}");

                // 注意：不要在这里执行恢复/销毁（放到 finally 统一处理）
            }
        }
        finally
        {
            // 5. 尝试恢复物体原本的Layer（如果记录存在）
            if (originalLayers != null && originalLayers.Count > 0)
            {
                foreach (var kvp in originalLayers)
                {
                    var t = kvp.Key;
                    if (t == null)
                    {
                        Debug.LogWarning("ObjectSnapshot: 原始 Transform 在恢复前已被销毁或为 null。");
                        continue;
                    }

                    GameObject go = null;
                    try
                    {
                        go = t.gameObject;
                    }
                    catch
                    {
                        Debug.LogWarning("ObjectSnapshot: 无法获取 GameObject（Transform 名称: " + (t.name ?? "<unknown>") + "）。");
                        continue;
                    }

                    if (go == null)
                    {
                        Debug.LogWarning("ObjectSnapshot: GameObject 为 null，无法恢复 layer（Transform 名称: " + (t.name ?? "<unknown>") + "）。");
                        continue;
                    }

                    try
                    {
                        go.layer = kvp.Value;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("ObjectSnapshot: 恢复 layer 失败: " + GetTransformPath(go.transform) + " , 异常: " + ex.Message);
                    }
                }
            }
    
            // 销毁截图资源（如果存在）
            if (screenshot != null)
            {
                try { Object.Destroy(screenshot); } catch { }
                screenshot = null;
            }
    
            // 6. 销毁临时相机
            if (tempCamera != null)
            {
                try { Object.DestroyImmediate(tempCamera.gameObject); } catch { }
            }
        }
    }

    // --- 辅助逻辑 ---

    // 递归设置Layer，并记录原始Layer（仅在尚未记录时写入，防止被后续调用覆盖）
    private static void SetLayerRecursively(GameObject go, int newLayer, Dictionary<GameObject, int> record)
    {
        if (go == null) return;
        // 保护性处理：在遍历过程中对象可能被销毁，使用 try/catch 避免抛出
        try
        {
            if (!record.ContainsKey(go))
            {
                record[go] = go.layer;
            }
            go.layer = newLayer;
        }
        catch
        {
            // 如果在记录或设置 layer 时发生异常（对象可能已被销毁），直接返回，不继续递归该分支
            return;
        }

        var tr = go.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            var child = tr.GetChild(i);
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer, record);
            }
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
        if (mask.value == 0) return -1;
        int layer = 0;
        int maskVal = mask.value;
        while (maskVal > 0)
        {
            if ((maskVal & 1) == 1) return layer;
            maskVal >>= 1;
            layer++;
        }
        return -1;
    }

    // 获取 Transform 的层级路径（用于调试日志）
    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "<null>";
        var parts = new List<string>();
        var cur = t;
        while (cur != null)
        {
            parts.Add(cur.name ?? "<unnamed>");
            cur = cur.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
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
