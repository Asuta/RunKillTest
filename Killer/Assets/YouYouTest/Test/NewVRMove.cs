using UnityEngine;

public class NewVRMove : MonoBehaviour
{
    public Transform leftSphere;
    public Transform leftSphereTarget;

    public Transform rightSphere;
    public Transform rightSphereTarget;
    
    [Tooltip("Lerp跟随速度，值越大跟随越快")]
    public float lerpSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 检测左手grip键
        float leftGrip = InputActionsManager.Actions.XRILeftInteraction.SelectValue.ReadValue<float>();
        if (leftGrip <= 0.1f && leftSphere != null && leftSphereTarget != null)
        {
            // 松开grip时，直接设置位置等于target位置
            leftSphere.position = leftSphereTarget.position;
            leftSphere.rotation = leftSphereTarget.rotation;
        }

        // 检测右手grip键
        float rightGrip = InputActionsManager.Actions.XRIRightInteraction.SelectValue.ReadValue<float>();
        if (rightGrip <= 0.1f && rightSphere != null && rightSphereTarget != null)
        {
            // 松开grip时，直接设置位置等于target位置
            rightSphere.position = rightSphereTarget.position;
            rightSphere.rotation = rightSphereTarget.rotation;
        }
    }

    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        // 检测左手grip键
        float leftGrip = InputActionsManager.Actions.XRILeftInteraction.SelectValue.ReadValue<float>();
        if (leftGrip > 0.1f && leftSphere != null && leftSphereTarget != null)
        {
            // 使用Lerp让leftSphere平滑跟随leftSphereTarget移动
            leftSphere.position = Vector3.Lerp(leftSphere.position, leftSphereTarget.position, lerpSpeed * Time.deltaTime);
            leftSphere.rotation = Quaternion.Slerp(leftSphere.rotation, leftSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }

        // 检测右手grip键
        float rightGrip = InputActionsManager.Actions.XRIRightInteraction.SelectValue.ReadValue<float>();
        if (rightGrip > 0.1f && rightSphere != null && rightSphereTarget != null)
        {
            // 使用Lerp让rightSphere平滑跟随rightSphereTarget移动
            rightSphere.position = Vector3.Lerp(rightSphere.position, rightSphereTarget.position, lerpSpeed * Time.deltaTime);
            rightSphere.rotation = Quaternion.Slerp(rightSphere.rotation, rightSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
    }
}
