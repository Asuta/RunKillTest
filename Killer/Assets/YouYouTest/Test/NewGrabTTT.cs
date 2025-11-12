using UnityEngine;

public class NewGrabTTT : MonoBehaviour
{
    public Transform target;
    public Transform controlObject;
    public Transform grabObject;
    public Vector3 middlePosition;
    public Quaternion middleRotation;
    public float positionLerpSpeed = 5f;
    public float rotationLerpSpeed = 5f;
    
    private Vector3 grabOffset;
    private Quaternion grabRotationOffset;
    private bool isGrabbing = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null && controlObject != null)
        {
            // 使用 Lerp 平滑插值位置
            Vector3 newPosition = Vector3.Lerp(controlObject.position, target.position, positionLerpSpeed * Time.deltaTime);
            
            // 使用 Slerp 平滑插值旋转
            Quaternion newRotation = Quaternion.Slerp(controlObject.rotation, target.rotation, rotationLerpSpeed * Time.deltaTime);
            
            // 将计算出的值赋值给 controlObject
            controlObject.position = newPosition;
            controlObject.rotation = newRotation;
        }
        
        // 如果正在抓取，让 grabObject 立即跟随 controlObject
        if (isGrabbing && grabObject != null && controlObject != null)
        {
            grabObject.position = controlObject.position + controlObject.rotation * grabOffset;
            grabObject.rotation = controlObject.rotation * grabRotationOffset;
        }
    }
    
    [ContextMenu("Start Grab")]
    public void StartGrab()
    {
        if (grabObject != null && controlObject != null)
        {
            // 计算相对位置偏移
            grabOffset = Quaternion.Inverse(controlObject.rotation) * (grabObject.position - controlObject.position);
            
            // 计算相对旋转偏移
            grabRotationOffset = Quaternion.Inverse(controlObject.rotation) * grabObject.rotation;
            
            isGrabbing = true;
        }
    }
    
    public void StopGrab()
    {
        isGrabbing = false;
    }
}
