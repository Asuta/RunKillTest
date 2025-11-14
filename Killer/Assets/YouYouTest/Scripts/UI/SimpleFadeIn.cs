using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class SimpleFadeIn : MonoBehaviour
{

    public Vector3 originalPosition;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        // 保存初始位置
        RectTransform rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }
    void OnEnable()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        RectTransform rectTransform = GetComponent<RectTransform>();
        cg.alpha = 0; // 设置初始透明度为0



        // 设置初始位置在下方（向下偏移100单位）
        // 对于RectTransform，向上移动是减少y值，向下移动是增加y值
        rectTransform.anchoredPosition = originalPosition + Vector3.up * 100f;

        // // 在0.5秒内淡入到完全显示
        // cg.DOFade(1, 0.5f).SetDelay(0.2f); // 延迟0.2秒后开始播放

        // 在0.2秒内淡入到完全显示，使用OutQuad缓动
        cg.DOFade(1, 0.2f).SetEase(Ease.OutQuad);

        // 在0.2秒内从下到上移动到原始位置，使用OutBack缓动（会有轻微的回弹效果）
        rectTransform.DOAnchorPos(originalPosition, 0.2f).SetEase(Ease.OutBack);
    }
}
