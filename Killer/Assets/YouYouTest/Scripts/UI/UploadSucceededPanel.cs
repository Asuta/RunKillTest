using System.Collections;
using UnityEngine;
using DG.Tweening;

public class UploadSucceededPanel : MonoBehaviour
{
    private void OnEnable()
    {
        // 确保显示时缩放正常
        transform.localScale = Vector3.one;
        
        // 开启协程，3秒后执行消失动画
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        // 等待 3 秒
        yield return new WaitForSeconds(2f);

        // 执行 DOTween 动画，缩放至 0 后隐藏
        transform.DOScale(0, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
