using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ShowcaseCarousel : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("The container that holds all the items (Content in ScrollView)")]
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Settings")]
    [SerializeField] private float scrollDuration = 0.5f;
    [SerializeField] private Ease scrollEase = Ease.OutQuad;

    [Tooltip("If true, will force all items to match the viewport width")]
    [SerializeField] private bool matchViewportWidth = true;

    [Tooltip("If 0, will try to auto-calculate from first child width + layout spacing")]
    [SerializeField] private float itemWidth = 0f;

    private int currentIndex = 0;
    private int totalItems = 0;
    private bool isRefreshing = false;

    private void Awake()
    {
        TryAutoWireReferences();
        // If content is still missing after trying to find it, try to generate it
        if (contentContainer == null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                // In play mode, we can't use Editor Undo, so we just call the logic directly
                // But GenerateUIStructure is an editor-only method.
                // We should really ensure structure exists.
                // However, since the user wants it to "generate itself", let's call a runtime-safe version if possible.
                // Or simply rely on the fact that if it's missing, we are likely in a fresh scene test.

                // NOTE: GenerateUIStructure is inside #if UNITY_EDITOR. We need to expose a runtime version or call it if we are in Editor Play Mode.
                // But usually, one generates the UI in Edit Mode.
                // If the user means "Generate automatically when I click Play", we can do this:
                GenerateUIStructure_Runtime();
                TryAutoWireReferences(); // Wire again after generation
            }
#endif
        }
    }

    private void Start()
    {
        TryAutoWireReferences();
        Initialize();

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(OnPrevClick);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClick);
        }

        UpdateButtons();

        StartCoroutine(DelayedRefresh());
    }

    public void Initialize()
    {
        TryAutoWireReferences();
        if (contentContainer == null)
        {
            Debug.LogError("ShowcaseCarousel: Content Container is not assigned!");
            return;
        }

        totalItems = contentContainer.childCount;

        // Auto-calculate item width if not set
        if (itemWidth <= 0.01f && totalItems > 0)
        {
            RectTransform firstChild = contentContainer.GetChild(0) as RectTransform;
            if (firstChild != null)
            {
                float spacing = 0f;
                HorizontalLayoutGroup layoutGroup = contentContainer.GetComponent<HorizontalLayoutGroup>();
                if (layoutGroup != null)
                {
                    spacing = layoutGroup.spacing;
                }

                // Try to get width from LayoutElement first, as rect.width might not be updated yet
                LayoutElement layoutElement = firstChild.GetComponent<LayoutElement>();
                if (layoutElement != null && layoutElement.preferredWidth > 0)
                {
                    itemWidth = layoutElement.preferredWidth + spacing;
                }
                else
                {
                    // Force rebuild to ensure rect width is correct
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);
                    itemWidth = firstChild.rect.width + spacing;
                }
            }
        }
    }

    private void TryAutoWireReferences()
    {
        if (contentContainer == null)
        {
            Transform t = transform.Find("Viewport/Content");
            if (t != null)
            {
                contentContainer = t as RectTransform;
            }
            else
            {
                RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rects.Length; i++)
                {
                    if (rects[i] != null && rects[i].name == "Content")
                    {
                        contentContainer = rects[i];
                        break;
                    }
                }
            }
        }

        if (prevButton == null)
        {
            Transform t = transform.Find("Btn_Prev");
            if (t != null)
            {
                prevButton = t.GetComponent<Button>();
            }
            if (prevButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].name == "Btn_Prev")
                    {
                        prevButton = buttons[i];
                        break;
                    }
                }
            }
        }

        if (nextButton == null)
        {
            Transform t = transform.Find("Btn_Next");
            if (t != null)
            {
                nextButton = t.GetComponent<Button>();
            }
            if (nextButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].name == "Btn_Next")
                    {
                        nextButton = buttons[i];
                        break;
                    }
                }
            }
        }
    }

    public void Refresh()
    {
        Initialize();
        RefreshLayoutAndSizing();
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        RefreshLayoutAndSizing();
    }

    private void RefreshLayoutAndSizing()
    {
        if (!Application.isPlaying) return;
        if (contentContainer == null) return;
        if (isRefreshing) return;

        isRefreshing = true;

        RectTransform viewport = contentContainer.parent as RectTransform;
        if (matchViewportWidth && viewport != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

            float viewportWidth = viewport.rect.width;
            if (viewportWidth > 0f)
            {
                float w = Mathf.Round(viewportWidth);

                for (int i = 0; i < contentContainer.childCount; i++)
                {
                    Transform child = contentContainer.GetChild(i);
                    RectTransform childRect = child as RectTransform;
                    if (childRect == null) continue;

                    LayoutElement le = child.GetComponent<LayoutElement>();
                    if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = w;
                    childRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

                float spacing = 0f;
                HorizontalLayoutGroup layoutGroup = contentContainer.GetComponent<HorizontalLayoutGroup>();
                if (layoutGroup != null)
                {
                    spacing = layoutGroup.spacing;
                }

                itemWidth = w + spacing;
            }
        }

        if (itemWidth > 0.01f)
        {
            contentContainer.DOKill();
            Vector2 pos = contentContainer.anchoredPosition;
            pos.x = -1f * (currentIndex * itemWidth);
            contentContainer.anchoredPosition = pos;
        }

        UpdateButtons();
        isRefreshing = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!Application.isPlaying) return;
        if (!matchViewportWidth) return;
        RefreshLayoutAndSizing();
    }

    private void OnPrevClick()
    {
        if (currentIndex > 0)
        {
            ScrollTo(currentIndex - 1);
        }
    }

    private void OnNextClick()
    {
        if (currentIndex < totalItems - 1)
        {
            ScrollTo(currentIndex + 1);
        }
    }

    public void ScrollTo(int index)
    {
        if (index < 0 || index >= totalItems) return;

        currentIndex = index;

        // Calculate target position
        // Moving content to the LEFT (negative x) reveals items on the RIGHT.
        float targetX = -1 * (currentIndex * itemWidth);

        // Kill any existing tweens on this transform to avoid conflicts
        contentContainer.DOKill();
        contentContainer.DOAnchorPosX(targetX, scrollDuration).SetEase(scrollEase);

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (prevButton != null) prevButton.interactable = currentIndex > 0;
        if (nextButton != null) nextButton.interactable = currentIndex < totalItems - 1;
    }

    private void OnDestroy()
    {
        if (contentContainer != null)
        {
            contentContainer.DOKill();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate UI Structure")]
    private void GenerateUIStructure()
    {
        // 1. Setup Root RectTransform
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(800, 400);

        // 2. Create Viewport (Mask)
        if (contentContainer == null)
        {
            GameObject viewportObj = CreateUIObject("Viewport", transform);
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(50, 0); // Left padding for button
            viewportRect.offsetMax = new Vector2(-50, 0); // Right padding for button

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f); // Transparent but raycast target
            viewportObj.AddComponent<RectMask2D>();

            // 3. Create Content
            GameObject contentObj = CreateUIObject("Content", viewportObj.transform);
            contentContainer = contentObj.GetComponent<RectTransform>();
            contentContainer.anchorMin = new Vector2(0, 0.5f);
            contentContainer.anchorMax = new Vector2(0, 0.5f);
            contentContainer.pivot = new Vector2(0, 0.5f); // Pivot left-center
            contentContainer.sizeDelta = new Vector2(0, 400); // Height matches root

            HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true; // Enable control width to respect LayoutElement
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 0; // No spacing for single page view
            hlg.padding = new RectOffset(0, 0, 0, 0); // No padding

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add dummy items
            float itemWidth = 700f; // Matches Viewport width (800 - 50 - 50)
            for (int i = 0; i < 5; i++)
            {
                GameObject item = CreateUIObject($"Item {i + 1}", contentObj.transform);
                Image img = item.AddComponent<Image>();
                img.color = Color.HSVToRGB(i * 0.15f, 0.7f, 0.9f);
                LayoutElement le = item.AddComponent<LayoutElement>();
                le.preferredWidth = itemWidth;
                le.preferredHeight = 300;

                // Add text
                GameObject textObj = CreateUIObject("Text", item.transform);
                Text text = textObj.AddComponent<Text>();
                text.text = $"Page {i + 1}";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
            }
        }

        // 4. Create Buttons
        if (prevButton == null)
        {
            GameObject btnObj = CreateUIObject("Btn_Prev", transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0.5f);
            btnRect.anchorMax = new Vector2(0, 0.5f);
            btnRect.anchoredPosition = new Vector2(25, 0); // Position inside left edge
            btnRect.sizeDelta = new Vector2(40, 80);

            Image img = btnObj.AddComponent<Image>();
            img.color = Color.gray;
            prevButton = btnObj.AddComponent<Button>();

            // Text <
            GameObject textObj = CreateUIObject("Text", btnObj.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = "<";
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        if (nextButton == null)
        {
            GameObject btnObj = CreateUIObject("Btn_Next", transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-25, 0); // Position inside right edge
            btnRect.sizeDelta = new Vector2(40, 80);

            Image img = btnObj.AddComponent<Image>();
            img.color = Color.gray;
            nextButton = btnObj.AddComponent<Button>();

            // Text >
            GameObject textObj = CreateUIObject("Text", btnObj.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = ">";
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        // 5. Ensure GraphicRaycaster
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // 6. Ensure EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
#endif
        }

#if UNITY_EDITOR
        // Mark scene dirty
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();

#if UNITY_EDITOR
        // Register Undo for better editor experience
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Create UI Object");
        }
#endif

        return obj;
    }


    private void GenerateUIStructure_Runtime()
    {
        // 1. Setup Root RectTransform
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(800, 400);

        // 2. Create Viewport (Mask)
        if (contentContainer == null)
        {
            GameObject viewportObj = CreateUIObject("Viewport", transform);
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(50, 0); // Left padding for button
            viewportRect.offsetMax = new Vector2(-50, 0); // Right padding for button

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f); // Transparent but raycast target
            viewportObj.AddComponent<RectMask2D>();

            // 3. Create Content
            GameObject contentObj = CreateUIObject("Content", viewportObj.transform);
            contentContainer = contentObj.GetComponent<RectTransform>();
            contentContainer.anchorMin = new Vector2(0, 0.5f);
            contentContainer.anchorMax = new Vector2(0, 0.5f);
            contentContainer.pivot = new Vector2(0, 0.5f); // Pivot left-center
            contentContainer.sizeDelta = new Vector2(0, 400); // Height matches root

            HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true; // Enable control width to respect LayoutElement
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 0; // No spacing for single page view
            hlg.padding = new RectOffset(0, 0, 0, 0); // No padding

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add dummy items
            float itemWidth = 700f; // Matches Viewport width (800 - 50 - 50)
            for (int i = 0; i < 5; i++)
            {
                GameObject item = CreateUIObject($"Item {i + 1}", contentObj.transform);
                Image img = item.AddComponent<Image>();
                img.color = Color.HSVToRGB(i * 0.15f, 0.7f, 0.9f);
                LayoutElement le = item.AddComponent<LayoutElement>();
                le.preferredWidth = itemWidth;
                le.preferredHeight = 300;

                // Add text
                GameObject textObj = CreateUIObject("Text", item.transform);
                Text text = textObj.AddComponent<Text>();
                text.text = $"Page {i + 1}";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
#if UNITY_EDITOR
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
            }
        }

        // 4. Create Buttons
        if (prevButton == null)
        {
            GameObject btnObj = CreateUIObject("Btn_Prev", transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0.5f);
            btnRect.anchorMax = new Vector2(0, 0.5f);
            btnRect.anchoredPosition = new Vector2(25, 0); // Position inside left edge
            btnRect.sizeDelta = new Vector2(40, 80);

            Image img = btnObj.AddComponent<Image>();
            img.color = Color.gray;
            prevButton = btnObj.AddComponent<Button>();

            // Text <
            GameObject textObj = CreateUIObject("Text", btnObj.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = "<";
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.color = Color.white;
#if UNITY_EDITOR
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        if (nextButton == null)
        {
            GameObject btnObj = CreateUIObject("Btn_Next", transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-25, 0); // Position inside right edge
            btnRect.sizeDelta = new Vector2(40, 80);

            Image img = btnObj.AddComponent<Image>();
            img.color = Color.gray;
            nextButton = btnObj.AddComponent<Button>();

            // Text >
            GameObject textObj = CreateUIObject("Text", btnObj.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = ">";
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.color = Color.white;
#if UNITY_EDITOR
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        // 5. Ensure GraphicRaycaster
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
#endif
