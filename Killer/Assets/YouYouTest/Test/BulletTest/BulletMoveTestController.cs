using UnityEngine;

public class BulletMoveTestController : MonoBehaviour
{
    [Header("测试设置")]
    public BulletMoveTest bulletTest;
    public Transform pointA;
    public Transform pointB;
    
    [Header("移动设置")]
    public float moveSpeed = 2f;
    public float moveRadius = 5f;
    public bool enableMovement = true;
    
    [Header("控制选项")]
    public bool autoFire = true;
    public float fireInterval = 3f;
    
    private float fireTimer;
    private float angleA;
    private float angleB;
    private Vector3 centerPoint;
    
    void Start()
    {
        // 设置中心点
        centerPoint = transform.position;
        
        // 初始化角度
        angleA = 0f;
        angleB = Mathf.PI; // B点初始位置与A点相对
        
        // 如果没有设置bulletTest，尝试查找
        if (bulletTest == null)
        {
            bulletTest = FindObjectOfType<BulletMoveTest>();
        }
        
        // 初始化子弹
        if (bulletTest != null)
        {
            bulletTest.SetTargets(pointA, pointB);
        }
        
        fireTimer = 0f;
    }
    
    void Update()
    {
        if (enableMovement)
        {
            MovePoints();
        }
        
        if (autoFire)
        {
            HandleAutoFire();
        }
        
        HandleInput();
    }
    
    /// <summary>
    /// 移动A点和B点
    /// </summary>
    private void MovePoints()
    {
        // A点做圆周运动
        angleA += moveSpeed * Time.deltaTime * 0.5f;
        Vector3 newPosA = centerPoint + new Vector3(
            Mathf.Cos(angleA) * moveRadius,
            0,
            Mathf.Sin(angleA) * moveRadius
        );
        pointA.position = newPosA;
        
        // B点做反向圆周运动
        angleB -= moveSpeed * Time.deltaTime * 0.3f;
        Vector3 newPosB = centerPoint + new Vector3(
            Mathf.Cos(angleB) * moveRadius * 0.8f,
            Mathf.Sin(angleB * 2) * 2f, // 添加垂直运动
            Mathf.Sin(angleB) * moveRadius * 0.8f
        );
        pointB.position = newPosB;
    }
    
    /// <summary>
    /// 处理自动发射
    /// </summary>
    private void HandleAutoFire()
    {
        fireTimer += Time.deltaTime;
        
        if (fireTimer >= fireInterval)
        {
            FireBullet();
            fireTimer = 0f;
        }
    }
    
    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireBullet()
    {
        if (bulletTest != null)
        {
            bulletTest.ResetBullet();
            Debug.Log("发射新子弹！");
        }
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 空格键手动发射
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireBullet();
        }
        
        // M键切换移动
        if (Input.GetKeyDown(KeyCode.M))
        {
            enableMovement = !enableMovement;
            Debug.Log($"移动状态: {(enableMovement ? "开启" : "关闭")}");
        }
        
        // F键切换自动发射
        if (Input.GetKeyDown(KeyCode.F))
        {
            autoFire = !autoFire;
            Debug.Log($"自动发射: {(autoFire ? "开启" : "关闭")}");
        }
        
        // R键重置位置
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPositions();
        }
    }
    
    /// <summary>
    /// 重置位置
    /// </summary>
    private void ResetPositions()
    {
        angleA = 0f;
        angleB = Mathf.PI;
        
        pointA.position = centerPoint + new Vector3(moveRadius, 0, 0);
        pointB.position = centerPoint + new Vector3(-moveRadius, 0, 0);
        
        if (bulletTest != null)
        {
            bulletTest.ResetBullet();
        }
        
        Debug.Log("位置已重置");
    }
    
    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // 绘制运动范围
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(centerPoint, moveRadius);
            Gizmos.DrawWireSphere(centerPoint, moveRadius * 0.8f);
        }
    }
    
    /// <summary>
    /// 在GUI中显示控制说明
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("子弹移动测试控制器", GUI.skin.box);
        GUILayout.Space(10);
        
        GUILayout.Label($"移动状态: {(enableMovement ? "开启" : "关闭")} (按M切换)");
        GUILayout.Label($"自动发射: {(autoFire ? "开启" : "关闭")} (按F切换)");
        GUILayout.Space(10);
        
        GUILayout.Label("控制说明:");
        GUILayout.Label("空格键 - 手动发射子弹");
        GUILayout.Label("M键 - 切换移动状态");
        GUILayout.Label("F键 - 切换自动发射");
        GUILayout.Label("R键 - 重置位置");
        
        GUILayout.EndArea();
    }
}