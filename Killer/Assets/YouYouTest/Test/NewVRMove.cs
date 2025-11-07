using UnityEngine;

public class NewVRMove : MonoBehaviour
{
    public Transform leftSphere;
    public Transform leftSphereTarget;

    public Vector3 leftDirection;

    public Transform rightSphere;
    public Transform rightSphereTarget;

    public Vector3 rightDirection;


    public Vector3 residualVelocity;
    public float speedDecay;




    //test
    public Transform linePosition;




    [Tooltip("Lerp跟随速度，值越大跟随越快")]
    public float lerpSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初始化grip状态
        lastLeftGrip = 0f;
        lastRightGrip = 0f;
    }

    // Update is called once per frame
    void Update()
    {

    }

    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        // 先读取当前grip值（但不要在读取后马上覆盖球体位置）
        float leftGrip = InputActionsManager.Actions.XRILeftInteraction.SelectValue.ReadValue<float>();
        float rightGrip = InputActionsManager.Actions.XRIRightInteraction.SelectValue.ReadValue<float>();

        // 1) 先基于当前sphere与target的"真实位置"计算方向向量（**必须在修改球体位置之前计算**）
        if (leftSphere != null && leftSphereTarget != null)
        {
            leftDirection = leftSphere.position - leftSphereTarget.position;
            leftDirection = new Vector3(leftDirection.x, 0, leftDirection.z); // 只保留水平分量
        }
        else
        {
            leftDirection = Vector3.zero;
        }

        if (rightSphere != null && rightSphereTarget != null)
        {
            rightDirection = rightSphere.position - rightSphereTarget.position;
            rightDirection = new Vector3(rightDirection.x, 0, rightDirection.z); // 只保留水平分量
        }
        else
        {
            rightDirection = Vector3.zero;
        }

        // 2) 在把球体位置写回target之前，检测"松手瞬间"，把当时捕获到的方向累加到residualVelocity
        if (lastLeftGrip > 0.1f && leftGrip <= 0.1f)
        {
            residualVelocity += leftDirection;
            Debug.Log("左手松开（捕获前一帧位置），累加leftDirection到residualVelocity: " + leftDirection);
        }

        if (lastRightGrip > 0.1f && rightGrip <= 0.1f)
        {
            residualVelocity += rightDirection;
            Debug.Log("右手松开（捕获前一帧位置），累加rightDirection到residualVelocity: " + rightDirection);
        }

        // 3) 更新grip历史状态（用于下一帧检测）
        lastLeftGrip = leftGrip;
        lastRightGrip = rightGrip;

        // 4) 现在执行跟随逻辑（Lerp 或 直接设置）
        if (leftGrip > 0.1f && leftSphere != null && leftSphereTarget != null)
        {
            leftSphere.position = Vector3.Lerp(leftSphere.position, leftSphereTarget.position, lerpSpeed * Time.deltaTime);
            leftSphere.rotation = Quaternion.Slerp(leftSphere.rotation, leftSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
        else if (leftSphere != null && leftSphereTarget != null)
        {
            // 松开时直接同步位置（这一步在计算并累加方向之后执行）
            leftSphere.position = leftSphereTarget.position;
            leftSphere.rotation = leftSphereTarget.rotation;
        }

        if (rightGrip > 0.1f && rightSphere != null && rightSphereTarget != null)
        {
            rightSphere.position = Vector3.Lerp(rightSphere.position, rightSphereTarget.position, lerpSpeed * Time.deltaTime);
            rightSphere.rotation = Quaternion.Slerp(rightSphere.rotation, rightSphereTarget.rotation, lerpSpeed * Time.deltaTime);
        }
        else if (rightSphere != null && rightSphereTarget != null)
        {
            rightSphere.position = rightSphereTarget.position;
            rightSphere.rotation = rightSphereTarget.rotation;
        }

        // 5) 每帧都让residualVelocity以speedDecay衰减（使用MoveTowards避免反向）
        residualVelocity = Vector3.MoveTowards(residualVelocity, Vector3.zero, speedDecay * Time.deltaTime);

        // 日志与可视化（只显示residualVelocity）
        Debug.Log("当前residualVelocity: " + residualVelocity);
        if (linePosition != null)
        {
            Debug.DrawLine(linePosition.position, linePosition.position + residualVelocity * 11f, Color.red, 0.1f);
        }
    }

    // 用于检测grip键状态变化
    private float lastLeftGrip;
    private float lastRightGrip;
}
