using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class SimpleFadeIn : MonoBehaviour
{
    public Vector3 originalPosition;
    private RectTransform rectTransform;
    private CanvasGroup cg;
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
        originalPosition = rectTransform.anchoredPosition;
        isInitialized = true;
    }

    void OnEnable()
    {
        EnsureInitialized();

        // 停止之前的动画，防止冲突
        cg.DOKill();
        rectTransform.DOKill();

        cg.alpha = 0; // 设置初始透明度为0

        // 设置初始位置在下方（向下偏移100单位）
        // 在 Unity UI 中，Vector3.down (0, -1, 0) 是向下
        rectTransform.anchoredPosition = originalPosition + Vector3.down * 100f;

        // 在0.2秒内淡入到完全显示，使用OutQuad缓动
        cg.DOFade(1, 0.2f).SetEase(Ease.OutQuad);

        // 在0.2秒内从下到上移动到原始位置，使用OutBack缓动
        rectTransform.DOAnchorPos(originalPosition, 0.2f).SetEase(Ease.OutBack);
    }
}
