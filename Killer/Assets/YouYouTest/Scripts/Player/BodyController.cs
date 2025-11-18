using UnityEngine;

public class BodyController : MonoBehaviour
{
    public Transform rotateTarget;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // LateUpdate is called once per frame after all Update calls
    void LateUpdate()
    {
        if (rotateTarget != null)
        {
            // 获取当前物体的当前旋转
            Vector3 currentRotation = transform.eulerAngles;
            // 获取rotateTarget的世界Y轴旋转角度
            float targetYRotation = rotateTarget.eulerAngles.y;
            // 只修改Y轴，保持X和Z轴不变
            transform.eulerAngles = new Vector3(currentRotation.x, targetYRotation, currentRotation.z);
        }
    }




}
