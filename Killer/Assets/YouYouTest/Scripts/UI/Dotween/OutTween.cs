using UnityEngine;
using DG.Tweening;

public class OutTween : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutQuad;
    [SerializeField] private float delay = 0f;

    private Vector3 initialScale;

    private void Awake()
    {
        // 在 Awake 中记录初始缩放，防止在 OnEnable 中被重置为 zero
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        // 确保从 0 开始
        transform.localScale = Vector3.zero;

        // 动画到初始缩放值
        transform.DOScale(initialScale, duration)
            .SetEase(easeType)
            .SetDelay(delay)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        // 当禁用时，可以考虑杀死正在进行的动画，防止冲突
        transform.DOKill();
    }
}
