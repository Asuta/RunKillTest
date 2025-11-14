using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class SimpleFadeIn : MonoBehaviour
{
    void Start()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 0; // 设置初始透明度为0
        
        // 保存初始位置
        Vector3 originalPosition = transform.position;
        
        // 设置初始位置在下方（向下偏移100单位）
        transform.position = originalPosition - Vector3.up * 2f;
        
        // // 在0.5秒内淡入到完全显示
        // cg.DOFade(1, 0.5f).SetDelay(0.2f); // 延迟0.2秒后开始播放

        // 在0.2秒内淡入到完全显示，使用OutQuad缓动
        cg.DOFade(1, 0.2f).SetEase(Ease.OutQuad);
        
        // 在0.2秒内从下到上移动到原始位置，使用OutBack缓动（会有轻微的回弹效果）
        transform.DOMove(originalPosition, 0.2f).SetEase(Ease.OutBack);
    }
}
