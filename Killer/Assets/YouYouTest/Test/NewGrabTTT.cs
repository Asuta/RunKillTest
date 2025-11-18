using UnityEngine;

public class NewGrabTTT : MonoBehaviour
{
    public Transform target;
    public Transform grabObject;
    public Vector3 middlePosition;
    public Quaternion middleRotation = Quaternion.identity;
    public float positionLerpSpeed = 5f;
    public float rotationLerpSpeed = 5f;
    
    private Vector3 grabOffset;
    private Quaternion grabRotationOffset;
    private bool isGrabbing = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初始化中间追踪数据，避免未赋值时产生偏移
        middlePosition = transform.position;
        middleRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            // 中间数据以差值方式追踪 target（保存为 middlePosition / middleRotation）
            middlePosition = Vector3.Lerp(middlePosition, target.position, positionLerpSpeed * Time.deltaTime);
            middleRotation = Quaternion.Slerp(middleRotation, target.rotation, rotationLerpSpeed * Time.deltaTime);
        }
        
        // 如果正在抓取，让 grabObject 立即跟随中间数据（无差值）
        if (isGrabbing && grabObject != null)
        {
            grabObject.position = middlePosition + middleRotation * grabOffset;
            grabObject.rotation = middleRotation * grabRotationOffset;
        }
    }
    
    [ContextMenu("Start Grab")]
    public void StartGrab()
    {
        if (grabObject != null)
        {
            // 记录 grabObject 与中间数据之间的相对位置和旋转
            grabOffset = Quaternion.Inverse(middleRotation) * (grabObject.position - middlePosition);
            grabRotationOffset = Quaternion.Inverse(middleRotation) * grabObject.rotation;
            
            isGrabbing = true;
        }
    }
    
    public void StopGrab()
    {
        isGrabbing = false;
    }
}
