using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UICreate : MonoBehaviour
{
    [Header("标签页设置")]
    [SerializeField] private string[] tabNames = { "标签1", "标签2", "标签3" };

    [HideInInspector] public Canvas canvas;

    void Start()
    {
        CreateUI();
    }

    /// <summary>
    /// 创建带有标签页切换功能的UI界面（只负责创建，不添加逻辑）
    /// </summary>
    void CreateUI()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("TabCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

    // 在 Canvas 对象上添加 UITabController 并赋值引用（由 UITabController 负责后续逻辑注入）
    var controller = canvasObj.AddComponent<UITabController>();
    // 将当前的 UICreate 引用传给控制器，方便其在 Start/Initialize 中使用
    // controller.uiCreate = this;

        // 创建背景面板
        GameObject bgPanel = CreatePanel(canvasObj.transform, "BackgroundPanel", 
            new Vector2(800, 600), Vector2.zero);
        Image bgImage = bgPanel.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // 创建标签按钮容器
        GameObject tabButtonPanel = CreatePanel(bgPanel.transform, "TabButtonPanel",
            new Vector2(800, 60), new Vector2(0, 270));

        // 创建标签内容容器
        GameObject tabContentPanel = CreatePanel(bgPanel.transform, "TabContentPanel",
            new Vector2(780, 500), new Vector2(0, -30));

    // (不在此处保存按钮/内容引用，逻辑由另一个脚本处理)

        // 创建标签按钮和内容
        float buttonWidth = 780f / tabNames.Length;
        for (int i = 0; i < tabNames.Length; i++)
        {
            // 创建标签按钮
            float xPos = -390 + (i * buttonWidth) + (buttonWidth / 2);
            CreateButton(tabButtonPanel.transform, tabNames[i],
                new Vector2(buttonWidth - 10, 50), new Vector2(xPos, 0), i);

            // 创建标签内容
            GameObject tabContent = CreatePanel(tabContentPanel.transform, $"TabContent_{i}",
                new Vector2(780, 500), Vector2.zero);
            
            // 在每个标签内容中添加示例文本
            CreateText(tabContent.transform, $"这是{tabNames[i]}的内容",
                new Vector2(700, 400), Vector2.zero, 24);

            // 初始隐藏所有内容（除了第一个）
            tabContent.SetActive(i == 0);
        }
    }

    /// <summary>
    /// 创建面板
    /// </summary>
    GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        return panel;
    }

    /// <summary>
    /// 创建按钮（不添加逻辑）
    /// </summary>
    GameObject CreateButton(Transform parent, string text, Vector2 size, Vector2 position, int index)
    {
        GameObject buttonObj = new GameObject($"TabButton_{index}");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = Color.gray; // 默认颜色
        
        // 添加Button组件但不添加事件
        buttonObj.AddComponent<Button>();
        
        // 添加按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = size;
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return buttonObj;
    }

    /// <summary>
    /// 创建文本
    /// </summary>
    GameObject CreateText(Transform parent, string text, Vector2 size, Vector2 position, int fontSize)
    {
        GameObject textObj = new GameObject("ContentText");
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return textObj;
    }
}
