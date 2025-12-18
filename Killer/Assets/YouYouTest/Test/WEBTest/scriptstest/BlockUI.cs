using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BlockUI : MonoBehaviour
{
    [Header("网格设置")]
    public int columns = 5;
    public int rows = 4;
    public Vector2 cellSize = new Vector2(100, 100);
    public Vector2 spacing = new Vector2(10, 10);

    [Header("样式设置")]
    public Color slotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    void Start()
    {
        SetupCanvasAndGrid();
    }

    /// <summary>
    /// 设置 Canvas 并生成网格
    /// </summary>
    public void SetupCanvasAndGrid()
    {
        // 1. 检查或创建 Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("EquipmentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 设置 CanvasScaler
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 确保有 EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            // 将当前脚本所在的物体移动到新 Canvas 下
            transform.SetParent(canvasObj.transform, false);
        }

        // 2. 设置容器 RectTransform 居中
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        GenerateGrid();
    }

    /// <summary>
    /// 生成装备格子网格
    /// </summary>
    public void GenerateGrid()
    {
        // 1. 清理现有的子物体（如果是重新生成）
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // 2. 配置 GridLayoutGroup
        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

        // 3. 动态创建格子
        int totalSlots = columns * rows;
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i);
        }

        // 4. 自动调整父容器大小（可选，如果需要根据内容撑开）
        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void CreateSlot(int index)
    {
        // 创建格子物体
        GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slotObj.transform.SetParent(transform, false);

        // 设置背景图样式
        Image bgImage = slotObj.GetComponent<Image>();
        bgImage.color = slotColor;
        bgImage.type = Image.Type.Sliced; // 假设使用九宫格图片，默认白色像素也可以

        // 添加一个简单的边框或内框（可选）
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(slotObj.transform, false);
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        Image iconImage = iconObj.GetComponent<Image>();
        iconImage.color = new Color(1, 1, 1, 0.1f); // 默认透明度低，表示空的
    }
}
