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
    }

    private void Start()
    {
        TryAutoWireReferences();
        Initialize();

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(OnPrevClick);
            prevButton.onClick.AddListener(OnPrevClick);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClick);
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

        // Trigger tutorial for the new item
        TriggerCurrentTutorial();

        // Calculate target position
        // Moving content to the LEFT (negative x) reveals items on the RIGHT.
        float targetX = -1 * (currentIndex * itemWidth);

        // Kill any existing tweens on this transform to avoid conflicts
        contentContainer.DOKill();
        contentContainer.DOAnchorPosX(targetX, scrollDuration).SetEase(scrollEase);

        UpdateButtons();
    }

    private void TriggerCurrentTutorial()
    {
        if (contentContainer == null || currentIndex < 0 || currentIndex >= contentContainer.childCount)
            return;

        Transform child = contentContainer.GetChild(currentIndex);
        if (child != null)
        {
            EditorTutorialTrigger trigger = child.GetComponent<EditorTutorialTrigger>();
            if (trigger != null)
            {
                trigger.ShowTutorial();
            }
        }
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
}
