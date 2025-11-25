using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ObjectSnapshot : MonoBehaviour
{
    [Header("设置")]
    public Camera snapshotCamera; // 拖入一个专用的摄像机，或者代码里动态生成
    [Header("调整")]
    [Range(0.1f, 2f)]
    public float zoomFactor = 0.8f; // 数值越小，相机离物体越近，画面越紧凑
    public LayerMask snapshotLayer; // 专门用于拍照的Layer，例如新建一个叫 "Snapshot"
    public int width = 512;
    public int height = 384;
    public Color backgroundColor = new Color(0, 0, 0, 0); // 默认背景透明

    // 拍照时的观察角度（例如从斜上方看）
    private Vector3 cameraDirection = new Vector3(1f, 0.3f, 1f).normalized;

    /// <summary>
    /// 对选中的多个物体进行截图并保存
    /// </summary>
    /// <param name="targets">选中的游戏对象列表</param>
    /// <param name="savePath">保存路径 (例如 Application.persistentDataPath + "/save1_thumb.png")</param>
    public void CaptureAndSave(List<GameObject> targets, string savePath)
    {
        if (targets == null || targets.Count == 0) return;

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
        SetupCamera(combinedBounds);

        // 4. 渲染并保存图片
        Texture2D screenshot = RenderToTexture();
        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);

        Debug.Log($"截图已保存至: {savePath}");

        // 5. 清理：恢复物体原本的Layer，销毁临时资源
        foreach (var kvp in originalLayers)
        {
            kvp.Key.gameObject.layer = kvp.Value;
        }
        Destroy(screenshot);
    }

    // --- 辅助逻辑 ---

    // 递归设置Layer，并记录原始Layer
    private void SetLayerRecursively(Transform trans, int newLayer, Dictionary<Transform, int> record)
    {
        record[trans] = trans.gameObject.layer;
        trans.gameObject.layer = newLayer;
        foreach (Transform child in trans)
        {
            SetLayerRecursively(child, newLayer, record);
        }
    }

    // 计算所有Renderer的合并包围盒
    private Bounds CalculateBounds(List<GameObject> targets)
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

    // 调整摄像机以适应包围盒    // 调整摄像机以适应包围盒
    private void SetupCamera(Bounds bounds)
    {
        // 基础设置
        snapshotCamera.enabled = true;
        snapshotCamera.cullingMask = snapshotLayer;
        snapshotCamera.clearFlags = CameraClearFlags.SolidColor;
        // snapshotCamera.backgroundColor = backgroundColor; // 如果你还是想用代码控制背景色，把这行取消注释
        // 1. 算出物体大概有多大（半径）
        float objectRadius = bounds.extents.magnitude;
        // 2. 设置相机朝向（看向物体中心）
        snapshotCamera.transform.rotation = Quaternion.LookRotation(-cameraDirection);
        // 3. 根据相机类型计算位置/大小
        if (snapshotCamera.orthographic)
        {
            // --- 如果是正交相机 (Orthographic) ---
            // 正交相机不靠距离控制大小，而是靠 orthographicSize
            snapshotCamera.transform.position = bounds.center - snapshotCamera.transform.forward * 100f; // 放远点无所谓，只要不被裁剪
            snapshotCamera.orthographicSize = objectRadius * zoomFactor;
        }
        else
        {
            // --- 如果是透视相机 (Perspective) ---
            // 透视相机靠距离控制大小
            float fov = snapshotCamera.fieldOfView;
            // 核心修改：这里把原来的 1.5f 换成了 zoomFactor，让你能自由控制
            float distance = (objectRadius * zoomFactor) / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
            snapshotCamera.transform.position = bounds.center - snapshotCamera.transform.forward * distance;
        }
    }

    // 执行渲染
    private Texture2D RenderToTexture()
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        snapshotCamera.targetTexture = rt;
        snapshotCamera.Render();

        // 读取像素
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        // 清理
        snapshotCamera.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    // 辅助：从LayerMask获取Layer的Index
    private int GetLayerIndexFromMask(LayerMask mask)
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
}
