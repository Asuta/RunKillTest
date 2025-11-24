
using UnityEngine;
using DG.Tweening;
[RequireComponent(typeof(CanvasGroup))]
public class SelectSimpleFadeIn : MonoBehaviour
{
    void Start()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 0; // 设置初始透明度为0
        
        // 保存初始位置
        Vector3 originalPosition = transform.localPosition;
        
        // 设置初始位置在下方（向下偏移2个单位）
        transform.localPosition = originalPosition - Vector3.up * 0.1f;
        
        // // 在0.5秒内淡入到完全显示
        // cg.DOFade(1, 0.5f).SetDelay(0.2f); // 延迟0.2秒后开始播放
        // 在0.2秒内淡入到完全显示，使用OutQuad缓动
        cg.DOFade(1, 0.2f).SetEase(Ease.OutQuad);
        
        // 在0.2秒内从下到上移动到原始位置，使用OutQuad缓动（平滑无回弹）
        transform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
    }
}
