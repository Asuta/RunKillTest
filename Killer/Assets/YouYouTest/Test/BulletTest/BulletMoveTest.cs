using UnityEngine;

public class BulletMoveTest : MonoBehaviour
{
    [Header("移动设置")]
    public float speed = 10f; // 子弹移动速度（世界单位 / 秒） 
    public float nowSpeed = 10f; // 当前速度
    public float nowTime = 10f; // 当前用时
    public bool useDynamicTracking = true; // 是否启用动态追踪

    [Header("目标点设置")]
    public Transform pointA; // 起始点A
    public Transform pointB; // 目标点B

    [Header("调试选项")]
    public bool showDebugLine = true; // 是否显示调试线
    public Color debugLineColor = Color.yellow; // 调试线颜色

    // 内部状态：使用进度t（0..1）保证子弹始终在直线上
    private float progressT = 0f;
    private bool isMoving = true;
    private Vector3 lastPosition; // 用于观测实际速度
    private float timer; // 内部计时器

    void Start()
    {
        InitializeBullet();
    }

    /// <summary>
    /// 初始化子弹
    /// </summary>
    private void InitializeBullet()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("请设置pointA和pointB的Transform引用！");
            enabled = false;
            return;
        }

        // 将子弹放置在A点并重置进度
        progressT = 0f;
        transform.position = pointA.position;
        // 初始化速度观测基准
        lastPosition = transform.position;
        timer = 0f;
        nowTime = 0f;
        isMoving = true;
    }

    void Update()
    {
        // 按空格从A点重新发射一次（无论当前是否在移动）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetBullet();
            return;
        }

        if (!isMoving || pointA == null || pointB == null)
            return;

        if (useDynamicTracking)
        {
            UpdateDynamicMovement();
        }
        else
        {
            UpdateStaticMovement();
        }

        // 更新调试信息
        if (showDebugLine)
        {
            Debug.DrawLine(pointA.position, pointB.position, debugLineColor);
        }

        // 计算并记录实际速度（世界单位/秒）
        if (Time.deltaTime > 0f)
        {
            nowSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        }
        lastPosition = transform.position;

        // 更新计时器
        if (isMoving)
        {
            timer += Time.deltaTime;
            nowTime = timer;
        }
    }

    /// <summary>
    /// 动态追踪移动实现（保证每帧将子弹位置设置到当前直线上）
    /// 思路：维护进度 t（0..1），每帧根据当前A-B长度计算 deltaT，使得子弹在世界空间速度近似为 speed，
    /// 然后直接将 transform.position 置为 Lerp(A,B,t)，这样子弹始终在直线上。
    /// </summary>
    private void UpdateDynamicMovement()
    {
        Vector3 A = pointA.position;
        Vector3 B = pointB.position;
        Vector3 AB = B - A;
        float currentDistance = AB.magnitude;
        if (currentDistance <= Mathf.Epsilon)
        {
            transform.position = A;
            return;
        }

        Vector3 dir = AB / currentDistance; // 归一化方向

        // 1) 将当前位置投影到 AB 线段上（并夹制到 [A,B]）
        float tProj = Vector3.Dot(transform.position - A, AB) / (currentDistance * currentDistance);
        tProj = Mathf.Clamp01(tProj);
        Vector3 projPoint = A + AB * tProj;

        // 2) 以投影点为基准沿直线移动固定的世界距离
        float moveDistance = speed * Time.deltaTime;
        float remaining = (B - projPoint).magnitude;
        if (moveDistance >= remaining)
        {
            // 到达或超过目标
            transform.position = B;
            progressT = 1f;
            OnReachTarget();
            return;
        }

        Vector3 movedPos = projPoint + dir * moveDistance;

        // 3) 将新位置投影回线段并设置位置，更新 progressT
        float tNew = Vector3.Dot(movedPos - A, AB) / (currentDistance * currentDistance);
        tNew = Mathf.Clamp01(tNew);
        transform.position = A + AB * tNew;
        progressT = tNew;
    }


    /// <summary>
    /// 静态移动：A点和B点位置固定（兼容老逻辑）
    /// </summary>
    private void UpdateStaticMovement()
    {
        Vector3 dir = (pointB.position - transform.position);
        float dist = dir.magnitude;
        if (dist <= 0.001f)
        {
            OnReachTarget();
            return;
        }

        Vector3 move = dir.normalized * speed * Time.deltaTime;
        if (move.magnitude >= dist)
        {
            transform.position = pointB.position;
            OnReachTarget();
        }
        else
        {
            transform.position += move;
        }
    }

    /// <summary>
    /// 到达目标时的处理
    /// </summary>
    private void OnReachTarget()
    {
        isMoving = false;
        transform.position = pointB.position;
        nowTime = timer; // 记录最终用时
        Debug.Log($"子弹已到达目标点B，用时: {nowTime:F2}秒");
        // Destroy(gameObject);
    }

    /// <summary>
    /// 重置子弹状态
    /// </summary>
    public void ResetBullet()
    {
        InitializeBullet();
    }

    /// <summary>
    /// 设置新的目标点
    /// </summary>
    public void SetTargets(Transform newPointA, Transform newPointB)
    {
        pointA = newPointA;
        pointB = newPointB;
        ResetBullet();
    }

    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            // 绘制A点到B点的线
            Gizmos.color = debugLineColor;
            Gizmos.DrawLine(pointA.position, pointB.position);

            // 绘制A点和B点的球体
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointA.position, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointB.position, 0.2f);

            // 绘制子弹当前位置
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
    }
}
