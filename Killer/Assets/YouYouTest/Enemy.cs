using UnityEngine;

public class Enemy : MonoBehaviour, ICanBeHit
{
    #region 变量声明
    public int health = 100;
    public Transform target;
    public Transform EnemyBody;

    // 用于绘制的材质和网格
    public Material lineMaterial;
    public Material sphereMaterial;
    private Mesh sphereMesh;
    #endregion

    #region 伤害处理
    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Enemy took {damage} damage, remaining health: {health}");
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died!");
        // 在这里添加敌人死亡的逻辑，比如播放动画、掉落物品等
        Destroy(gameObject);
    }
    #endregion

    #region 目标设置
    public void SetTarget(Transform PlayerHead)
    {
        target = PlayerHead;
    }
    #endregion

    #region Unity生命周期方法
    private void Start()
    {
        // 创建球体网格
        sphereMesh = CreateSphereMesh(0.1f);
    }

    private void Update()
    {
        // 如果target不为null，绘制线和球
        if (target != null && EnemyBody != null)
        {
            DrawTargetVisualization();
        }
    }
    #endregion

    #region 自有方法
    public void OnHitByBullet()
    {
        TakeDamage(20);
    }

    #endregion

    #region 可视化绘制
    private void DrawTargetVisualization()
    {
        // 计算终点位置（target位置减1）
        Vector3 targetPosition = target.position;
        targetPosition = targetPosition + Vector3.up * -0.5f; // 降低一些高度，避免与视线重合
        Vector3 direction = (targetPosition - EnemyBody.position).normalized;
        Vector3 endPoint = targetPosition - direction * 1f; // 减1个单位

        // 绘制线
        DrawLine(EnemyBody.position, endPoint);

        // 绘制球
        DrawSphere(endPoint);
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        if (lineMaterial == null) return;

        // 使用工具类绘制线
        MeshDrawUtility.DrawLine(start, end, lineMaterial, 0.02f);
    }

    private void DrawSphere(Vector3 position)
    {
        if (sphereMaterial == null || sphereMesh == null) return;

        // 使用工具类绘制球体
        MeshDrawUtility.DrawSphere(position, sphereMaterial, sphereMesh);
    }
    #endregion

    #region 网格创建
    private Mesh CreateSphereMesh(float radius)
    {
        // 使用工具类创建球体网格
        return MeshDrawUtility.CreateSphereMesh(radius);
    }
    #endregion
}