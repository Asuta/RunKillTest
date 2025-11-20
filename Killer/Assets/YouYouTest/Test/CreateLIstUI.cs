using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CreateLIstUI : MonoBehaviour
{
    // 初始生成的格子数量
    public int initialItemCount = 20;

    void Start()
    {
        CreateScrollableInventory();
    }

    /// <summary>
    /// 创建完整的滚动背包UI
    /// </summary>
    void CreateScrollableInventory()
    {
        // 0. 确保场景中有 EventSystem (UI交互必须)
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            // 默认添加 StandaloneInputModule，如果是新版InputSystem，Unity可能会提示替换或自动处理
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // 1. 创建 Canvas (如果场景中没有Canvas，或者你想独立创建一个)
        GameObject canvasGO = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. 创建 ScrollView 背景
        GameObject scrollViewGO = new GameObject("ScrollView");
        scrollViewGO.transform.SetParent(canvasGO.transform, false);
        RectTransform scrollViewRT = scrollViewGO.AddComponent<RectTransform>();
        scrollViewRT.sizeDelta = new Vector2(300, 400); // 背包界面的大小
        
        Image scrollViewImage = scrollViewGO.AddComponent<Image>();
        scrollViewImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 半透明深色背景

        ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20f;

        // 3. 创建 Viewport (视口)
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollViewGO.transform, false);
        RectTransform viewportRT = viewportGO.AddComponent<RectTransform>();
        // 铺满父物体，但留出滚动条的位置（如果需要）
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;
        viewportRT.pivot = new Vector2(0, 1);

        // 添加 Mask
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        Image viewportImage = viewportGO.AddComponent<Image>(); // Mask需要Image组件

        // 4. 创建 Content (内容容器)
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        
        // 顶部对齐
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 300); // 高度会由 ContentSizeFitter 控制

        // 布局组件 - 使用 GridLayoutGroup 实现田字格/网格布局
        GridLayoutGroup layoutGroup = contentGO.AddComponent<GridLayoutGroup>();
        layoutGroup.cellSize = new Vector2(80, 80); // 设置格子大小
        layoutGroup.spacing = new Vector2(10, 10);  // 设置格子间距
        layoutGroup.padding = new RectOffset(10, 10, 10, 10); // 设置边距
        layoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.constraint = GridLayoutGroup.Constraint.Flexible; // 自动换行

        ContentSizeFitter sizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 绑定 ScrollRect
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;

        // 5. 创建垂直滚动条 (Scrollbar Vertical)
        GameObject scrollbarGO = new GameObject("Scrollbar Vertical");
        scrollbarGO.transform.SetParent(scrollViewGO.transform, false);
        RectTransform scrollbarRT = scrollbarGO.AddComponent<RectTransform>();
        // 锚点在右侧
        scrollbarRT.anchorMin = new Vector2(1, 0);
        scrollbarRT.anchorMax = new Vector2(1, 1);
        scrollbarRT.pivot = new Vector2(1, 1);
        scrollbarRT.sizeDelta = new Vector2(20, 0); // 宽度20

        Image scrollbarImage = scrollbarGO.AddComponent<Image>();
        scrollbarImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Sliding Area
        GameObject slidingAreaGO = new GameObject("Sliding Area");
        slidingAreaGO.transform.SetParent(scrollbarGO.transform, false);
        RectTransform slidingAreaRT = slidingAreaGO.AddComponent<RectTransform>();
        slidingAreaRT.anchorMin = Vector2.zero;
        slidingAreaRT.anchorMax = Vector2.one;
        slidingAreaRT.sizeDelta = Vector2.zero;

        // Handle (滑块)
        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(slidingAreaGO.transform, false);
        RectTransform handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 20);
        
        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRT;

        // 绑定滚动条到 ScrollRect
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3;

        // 6. 添加初始物品
        for (int i = 0; i < initialItemCount; i++)
        {
            AddItem(contentGO.transform, i);
        }
    }

    /// <summary>
    /// 动态添加一个格子
    /// </summary>
    public void AddItem(Transform contentParent, int index)
    {
        GameObject itemGO = new GameObject($"Item_{index}");
        itemGO.transform.SetParent(contentParent, false);
        
        // 背景图
        Image itemImage = itemGO.AddComponent<Image>();
        itemImage.color = new Color(Random.value, Random.value, Random.value); // 随机颜色区分

        // GridLayoutGroup 会自动控制子物体的大小，所以不需要 LayoutElement 来控制宽高
        // LayoutElement le = itemGO.AddComponent<LayoutElement>();
        // le.minHeight = 50; 
        // le.preferredHeight = 50;

        // 添加文本显示
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(itemGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.text = $"Item {index}";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        // 尝试获取一个默认字体，如果没有则使用Arial
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
    }
}
