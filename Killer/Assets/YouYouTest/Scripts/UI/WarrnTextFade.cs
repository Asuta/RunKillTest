using UnityEngine;
using TMPro;
using DG.Tweening;

public class WarrnTextFade : MonoBehaviour
{
    [Header("动画配置")]
    public float moveDistance = 50f; // 向上移动的距离
    public float duration = 1.5f;    // 动画持续时间
    public Ease moveEase = Ease.OutQuad; // 移动的缓动类型

    [Header("组件引用 (可选)")]
    public TMP_Text text;
    public CanvasGroup canvasGroup;

    void Start()
    {
        // 如果没有手动分配，则尝试自动获取
        if (text == null) text = GetComponent<TMP_Text>();
        if (text == null) text = GetComponentInChildren<TMP_Text>();

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();

        // 如果是 3D 对象（没有 CanvasGroup 且不是 UI），CanvasGroup.DOFade 是无效的
        // 但 text.DOFade 对 TMP_Text (无论是 3D 还是 UI) 都是有效的

        PlayAnimation();
    }

    private void PlayAnimation()
    {
        // 获取缩放后的移动距离
        float scaledMoveDistance = moveDistance * GameManager.Instance.VrEditorScale;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            // 如果是 UI 元素，使用 DOAnchorPosY 相对坐标移动
            rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + scaledMoveDistance, duration).SetEase(moveEase);
        }
        else
        {
            // 否则使用世界坐标移动
            transform.DOMoveY(transform.position.y + scaledMoveDistance, duration).SetEase(moveEase);
        }

        // 透明度渐隐：使用 alpha 属性，这在 TMP 中是最有效的
        if (text != null)
        {
            // 确保初始 alpha 是 1
            text.alpha = 1f;

            // 渐变到 0
            DOTween.To(() => text.alpha, x => text.alpha = x, 0f, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    Destroy(gameObject);
                });
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.DOFade(0, duration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            // 如果两个组件都没有，直接延迟销毁
            Destroy(gameObject, duration);
        }
    }
}
