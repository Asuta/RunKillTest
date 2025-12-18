using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro; // 使用 TextMeshPro

// 这个脚本现在只负责生成UI，不负责控制逻辑
public class TabUI : MonoBehaviour
{
    // 要创建的 Tab 数量
    public int numberOfTabs = 3;

    [ContextMenu("Generate Tab UI")]
    void Start()  
    {
        // 1. 确保场景中有 Canvas 和 EventSystem
        Canvas mainCanvas = SetupCanvasAndEventSystem();

        // 2. 创建一个总的根对象来容纳所有生成的UI
        GameObject uiRoot = new GameObject("GeneratedTabUI");
        uiRoot.transform.SetParent(mainCanvas.transform, false);
        RectTransform rootRT = uiRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;
        rootRT.anchoredPosition = Vector2.zero;

        // 3. 在UI根对象上添加控制器组件。它会在Awake()中自我配置。
        uiRoot.AddComponent<TabController>();

        // 4. 创建用于容纳 Tab 按钮的容器
        GameObject buttonContainer = CreateButtonContainer(uiRoot.transform);

        // 5. 创建用于容纳页面的容器
        GameObject pageContainer = CreatePageContainer(uiRoot.transform);

        // 6. 循环创建 Tab 和 Page。控制器会自动找到它们。
        for (int i = 0; i < numberOfTabs; i++)
        {
            // 创建页面
            CreatePage(pageContainer.transform, i);

            // 创建 Tab 按钮
            CreateTabButton(buttonContainer.transform, i);
        }

        // 7. 生成器完成任务后可以自行销毁，以保持场景干净
        Destroy(gameObject); 
    }

    Canvas SetupCanvasAndEventSystem()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        return canvas;
    }

    GameObject CreateButtonContainer(Transform parent)
    {
        // 容器命名为 "TabButtonContainer" 以便控制器查找
        GameObject container = new GameObject("TabButtonContainer", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform rt = container.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(0, 50);

        HorizontalLayoutGroup layoutGroup = container.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);
        layoutGroup.spacing = 5;

        return container;
    }

    GameObject CreatePageContainer(Transform parent)
    {
        // 容器命名为 "PageContainer" 以便控制器查找
        GameObject container = new GameObject("PageContainer", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform rt = container.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(10, 10); // left, bottom
        rt.offsetMax = new Vector2(-10, -60); // right, top

        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        return container;
    }

    Button CreateTabButton(Transform parent, int index)
    {
        // 按钮命名为 "TabButton_X" 以便控制器排序
        GameObject buttonGO = new GameObject("TabButton_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        Image img = buttonGO.GetComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f);

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Tab " + (index + 1);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.black;
        buttonText.fontSize = 24;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        return buttonGO.GetComponent<Button>();
    }

    GameObject CreatePage(Transform parent, int index)
    {
        // 页面命名为 "Page_X" 以便控制器排序
        GameObject pageGO = new GameObject("Page_" + index, typeof(RectTransform), typeof(Image));
        pageGO.transform.SetParent(parent, false);

        RectTransform rt = pageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = pageGO.GetComponent<Image>();
        img.color = Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.8f), (float)index / numberOfTabs);

        GameObject textGO = new GameObject("PageText", typeof(RectTransform));
        textGO.transform.SetParent(pageGO.transform, false);
        TextMeshProUGUI pageText = textGO.AddComponent<TextMeshProUGUI>();
        pageText.text = "This is Page " + (index + 1);
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.color = Color.black;
        pageText.fontSize = 36;

        return pageGO;
    }
}
