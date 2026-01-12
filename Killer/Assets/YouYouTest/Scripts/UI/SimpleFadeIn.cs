using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class SimpleFadeIn : MonoBehaviour
{
    [Header("动画设置")]
    public float duration = 0.2f;       // 动画持续时间
    public float moveOffset = 100f;    // 向上弹出的偏移量
    public Ease fadeEase = Ease.OutQuad;
    public Ease moveEase = Ease.OutBack;

    private RectTransform rectTransform;
    private CanvasGroup cg;
    private Vector3 originalPosition;
    private bool isInitialized = false;

    void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;
        rectTransform = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        
        // 强制刷新 UI 布局，确保获取到正确的原始位置
        Canvas.ForceUpdateCanvases();
        originalPosition = rectTransform.anchoredPosition;
        
        isInitialized = true;
    }

    void OnEnable()
    {
        EnsureInitialized();

        // 1. 停止之前的动画，并强制回归初始状态
        cg.DOKill();
        rectTransform.DOKill();
        
        // 重置状态
        cg.alpha = 0;
        rectTransform.anchoredPosition = originalPosition;

        // 2. 执行动画
        // 淡入效果
        cg.DOFade(1, duration).SetEase(fadeEase);

        // 位移效果：使用 From() 让它从“偏移位置”运动回“当前位置(originalPosition)”
        rectTransform.DOAnchorPos(originalPosition, duration)
            .From(originalPosition + Vector3.down * moveOffset)
            .SetEase(moveEase);
    }
}
