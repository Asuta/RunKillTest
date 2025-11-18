using VInspector;
using UnityEngine;

public class NewGrabTest : MonoBehaviour
{
    [Header("抓取目标设置")]
    [Tooltip("默认抓取目标（Group）")]
    public Transform groupTarget;
    
    [Header("抓取状态")]
    private bool isGrabbed = false;
    public Transform grabTarget;

    public Vector3 middlePosition;
    public Quaternion middleRotation;

    
    
    [Header("相对变换")]
    private Vector3 relativePosition;
    private Quaternion relativeRotation;
    
    [Header("跟随设置")]
    [Tooltip("跟随速度，越大越快跟上目标")]
    public float followSpeed = 5f;
    [Tooltip("旋转跟随速度")]
    public float rotationSpeed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrabbed && grabTarget != null)
        {
            FollowTarget();
        }
    }
    
    /// <summary>
    /// 抓取这个物体（默认抓取groupTarget）
    /// </summary>
    [Button("抓取物体")]
    public void Grab()
    {
        if (groupTarget == null)
        {
            Debug.LogWarning($"物体 {gameObject.name} 没有设置groupTarget！");
            return;
        }
        
        // 设置抓取目标为groupTarget
        grabTarget = groupTarget;
        isGrabbed = true;
        
        // 记录相对位置和相对旋转
        relativePosition = transform.position - grabTarget.position;
        relativeRotation = Quaternion.Inverse(grabTarget.rotation) * transform.rotation;
        
        Debug.Log($"物体 {gameObject.name} 被 {groupTarget.name} 抓取了");
    }
    
    /// <summary>
    /// 指定目标抓取（可选方法）
    /// </summary>
    /// <param name="target">抓取者（目标物体）</param>
    [Button("指定目标抓取")]
    public void Grab(Transform target)
    {
        if (target == null) return;
        
        // 设置抓取目标
        grabTarget = target;
        isGrabbed = true;
        
        // 记录相对位置和相对旋转
        relativePosition = transform.position - grabTarget.position;
        relativeRotation = Quaternion.Inverse(grabTarget.rotation) * transform.rotation;
        
        Debug.Log($"物体 {gameObject.name} 被 {target.name} 抓取了");
    }
    
    /// <summary>
    /// 释放抓取
    /// </summary>
    [Button("释放物体")]
    public void Release()
    {
        isGrabbed = false;
        grabTarget = null;
        Debug.Log($"物体 {gameObject.name} 释放了抓取");
    }
    
    /// <summary>
    /// 跟随目标物体（立即跟随，无插值）
    /// </summary>
    private void FollowTarget()
    {
        if (grabTarget == null) return;
        
        // 计算目标位置（保持相对位置）
        Vector3 targetPosition = grabTarget.position + grabTarget.rotation * relativePosition;
        
        // 计算目标旋转（保持相对角度）
        Quaternion targetRotation = grabTarget.rotation * relativeRotation;
        
        // 立即跟随目标位置（无插值）
        transform.position = targetPosition;
        
        // 立即跟随目标角度（无插值）
        transform.rotation = targetRotation;
    }
    
    /// <summary>
    /// 立即跟随（不进行平滑过渡）
    /// </summary>
    public void FollowImmediately()
    {
        if (grabTarget == null) return;
        
        // 立即设置位置
        Vector3 targetPosition = grabTarget.position + grabTarget.rotation * relativePosition;
        transform.position = targetPosition;
        
        // 立即设置旋转
        Quaternion targetRotation = grabTarget.rotation * relativeRotation;
        transform.rotation = targetRotation;
    }
    
    /// <summary>
    /// 是否正在被抓取
    /// </summary>
    public bool IsGrabbed => isGrabbed;
    
    /// <summary>
    /// 获取抓取目标
    /// </summary>
    public Transform GetGrabTarget() => grabTarget;
    
    /// <summary>
    /// 测试方法：使用groupTarget进行抓取测试
    /// </summary>
    [ContextMenu("测试抓取")]
    private void TestGrab()
    {
        if (groupTarget != null)
        {
            Grab(); // 使用默认的groupTarget
        }
        else
        {
            Debug.LogWarning($"物体 {gameObject.name} 没有设置groupTarget，请在Inspector中设置groupTarget！");
        }
    }
    
    /// <summary>
    /// 测试方法：通过Find方式找目标抓取
    /// </summary>
    [ContextMenu("Find测试抓取")]
    private void TestGrabByFind()
    {
        // 这里需要您自己指定一个测试目标
        GameObject testTarget = GameObject.Find("TestTarget");
        if (testTarget != null)
        {
            Grab(testTarget.transform);
        }
        else
        {
            Debug.LogWarning("请创建一个名为 'TestTarget' 的游戏物体来测试抓取功能");
        }
    }
}
