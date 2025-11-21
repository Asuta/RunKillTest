using UnityEngine;
using DG.Tweening;
[RequireComponent(typeof(CanvasGroup))]
public class DotweenUIBigOut : MonoBehaviour
{
    private Vector3 originalScale;
    
    void Awake()
    {
        // 保存初始缩放
        originalScale = transform.localScale;
    }
    
    void OnEnable()
    {
        PlayAnimation();
    }
    
    void Start()
    {
        PlayAnimation();
    }
    
    void PlayAnimation()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 0; // 设置初始透明度为0
        
        // 设置初始缩放为0（从小开始）
        transform.localScale = Vector3.zero;
        
        // 在0.2秒内淡入到完全显示，使用OutQuad缓动
        cg.DOFade(1, 0.2f).SetEase(Ease.OutQuad);
        
        // 在0.2秒内从小到大缩放到原始大小，使用OutBack缓动（会有轻微的回弹效果）
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBack);
    }
    
}