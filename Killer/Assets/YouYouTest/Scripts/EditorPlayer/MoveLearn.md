以某个东西（某个子物体？）为中心缩放大小


using UnityEngine;
public class ScaleTestttt : MonoBehaviour
{
    public Transform centerPoint; // 指定的中心点(先不用这个了)
    public Vector3 centerPosition;//用这个作为中心点
    public Transform TrackingSpace; // 摄像机的父物体
    public Transform leftHand; // 左手
    public Transform rightHand; // 右手
    public float recordScale; // 记录的初始比例
    public float recordDistance; // 记录的初始距离
    private void Update()
    {
        centerPosition = (leftHand.position + rightHand.position) / 2;
        //如果按下了手柄的A键并且按下了手柄的X键 或者按下了手柄的X键并且按下了手柄的A键
        if (OVRInput.GetDown(OVRInput.Button.One) && OVRInput.Get(OVRInput.Button.Three) || OVRInput.GetDown(OVRInput.Button.Three) && OVRInput.Get(OVRInput.Button.One))
        {
            //记录当前的相机的缩放倍数
            recordScale = TrackingSpace.lossyScale.x;
            //记录当前两个手柄的距离
            recordDistance = Vector3.Distance(leftHand.localPosition, rightHand.localPosition);
        }
        if (OVRInput.Get(OVRInput.Button.One) && OVRInput.Get(OVRInput.Button.Three))
        {
            // 获取现在两个手的距离
            var nowDistance = Vector3.Distance(leftHand.localPosition, rightHand.localPosition);
            // 计算现在的缩放比例
            var targetScale = recordDistance / nowDistance * recordScale;
            // 计算缩放后的大小
            var newSize = targetScale;

            // 计算要缩放的距离
            Vector3 zoomVector = (centerPosition - TrackingSpace.position) * (newSize / TrackingSpace.localScale.x - 1.0f);
            // 缩放物体并移动位置，以使中心点保持不变
            TrackingSpace.localScale = new Vector3(newSize, newSize, newSize);
            TrackingSpace.position -= zoomVector;
        }
    }
}

