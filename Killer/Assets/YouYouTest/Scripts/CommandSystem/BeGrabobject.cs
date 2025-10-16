using UnityEngine;

public class BeGrabobject  : MonoBehaviour
{
    private bool isGrabbed = false;
    private Transform grabHand; // 抓取它的手部transform
    private Vector3 offsetFromHand; // 相对于手的偏移
    private Quaternion initialRotationOffset; // 初始旋转偏移
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 如果被抓取，跟随手部移动
        if (isGrabbed && grabHand != null)
        {
            transform.position = grabHand.position + grabHand.rotation * offsetFromHand;
            transform.rotation = grabHand.rotation * initialRotationOffset;
        }
    }
    
    /// <summary>
    /// 被抓住时调用
    /// </summary>
    /// <param name="handTransform">抓住它的手部transform</param>
    public void OnGrabbed(Transform handTransform)
    {
        if (isGrabbed) return; // 已经被抓取了，不再处理
        
        isGrabbed = true;
        grabHand = handTransform;
        
        // 计算相对于手的偏移（保持当前位置）
        offsetFromHand = Quaternion.Inverse(handTransform.rotation) * (transform.position - handTransform.position);
        
        // 计算初始旋转偏移（保持当前旋转）
        initialRotationOffset = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
        
        Debug.Log($"{gameObject.name} 被抓住了");
    }
    
    /// <summary>
    /// 松开时调用
    /// </summary>
    public void OnReleased()
    {
        if (!isGrabbed) return; // 没有被抓取，不处理
        
        isGrabbed = false;
        grabHand = null;
        
        Debug.Log($"{gameObject.name} 被松开了");
    }
}
