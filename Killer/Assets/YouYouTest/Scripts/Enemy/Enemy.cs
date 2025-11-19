using UnityEngine;
using System;
using System.Collections.Generic;

public class Enemy : MonoBehaviour, ICanBeHit, IConfigurable
{
    #region IConfigurable 实现
    public string GetConfigTitle()
    {
        return $"Enemy ({gameObject.name})";
    }

    public List<ConfigItem> GetConfigItems()
    {
        var items = new List<ConfigItem>();

        // 配置 Health
        items.Add(new ConfigItem(
            "health",
            health,
            ConfigType.Int,
            (newValue) =>
            {
                health = Convert.ToInt32(newValue);
                Debug.Log($"[Enemy Config] Health updated to: {health}");
            }
        ));

        // 配置 Health2
        items.Add(new ConfigItem(
            "health2",
            health2,
            ConfigType.Int,
            (newValue) =>
            {
                health2 = Convert.ToInt32(newValue);
                Debug.Log($"[Enemy Config] Health2 updated to: {health2}");
            }
        ));

        return items;
    }
    #endregion

    #region 变量声明
    public int health = 100;
    public int health2 = 100;
    public Transform target;
    public Transform EnemyBody;
    public Transform checkBox;
    // public GameObject EnemyCheckerBox;

    // 用于绘制的材质和网格
    public Material lineMaterial;
    public Material sphereMaterial;
    private Mesh sphereMesh;
    public GameObject bulletPrefab;

    // 射击计时器
    private float aimingTime = 0f;
    private float lastFireTime = 0f;
    private bool hasFiredFirstShot = false;

    //死亡状态
    private bool isDead = false;

    //特效
    public GameObject deathEffect;
    #endregion

    #region 伤害处理
    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Enemy took {damage} damage, remaining health: {health}");
        if (health <= 0)
        {
            OnDeath();
        }
    }



    public void OnDeath()
    {
        Debug.Log("Enemy died!");
        //生成并 播放死亡特效
        if (deathEffect != null)
        {
            // 在enemy body位置生成死亡特效
            Vector3 spawnPosition = EnemyBody != null ? EnemyBody.position : transform.position;
            GameObject effect = Instantiate(deathEffect, spawnPosition, Quaternion.identity);
            // 可选：设置特效的旋转与敌人一致
            effect.transform.rotation = transform.rotation;
            effect.transform.localScale *= 2;
            // 播放特效（如果特效有自带的粒子系统或动画，会自动播放）
        }
        else
        {
            Debug.LogWarning("Death effect prefab is not assigned!");
        }

        // 停用所有子物体而不是销毁自己
        DeactivateAllChildren();
        
        // 5秒后重新激活并刷新状态
        StartCoroutine(ReactivateAfterDelay());
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
        // EnemyCheckerBox.GetComponent<MeshRenderer>().enabled = false;

        // 注册检查点重置事件
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
    }

    private void OnDestroy()
    {
        // 注销检查点重置事件
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }



    private void Update()
    {
        // 如果敌人处于死亡状态，不执行任何逻辑
        if (isDead) return;

        // 如果target不为null，绘制线和球
        if (target != null && EnemyBody != null)
        {
            DrawTargetVisualization();

            // 瞄准计时和射击逻辑
            HandleShooting();
        }
        else
        {
            // 重置瞄准计时器
            aimingTime = 0f;
            hasFiredFirstShot = false;
        }
    }
    #endregion

    #region 自有方法
    public void OnHitByBullet()
    {
        TakeDamage(20);
    }

    private void HandleShooting()
    {
        // 累计瞄准时间
        aimingTime += Time.deltaTime;

        if (!hasFiredFirstShot)
        {
            // 第一次发射：瞄准2秒后
            if (aimingTime >= 2f)
            {
                Fire();
                hasFiredFirstShot = true;
                lastFireTime = Time.time;
            }
        }
        else
        {
            // 后续发射：每隔5秒发射一次
            if (Time.time - lastFireTime >= 5f)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }
    }

    private void Fire()
    {
        if (bulletPrefab == null || target == null || EnemyBody == null)
        {
            Debug.LogWarning("Cannot fire: missing bulletPrefab, target, or EnemyBody");
            return;
        }

        // 创建子弹实例
        GameObject bullet = Instantiate(bulletPrefab, EnemyBody.position, Quaternion.identity);
        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();

        if (enemyBullet != null)
        {
            enemyBullet.target = target;
            enemyBullet.createEnemy = this;
        }

        Debug.Log("Enemy fired a bullet!");
    }

    private void OnCheckPointReset()
    {
        // 检查点重置时停止攻击，并清空目标
        target = null;
        aimingTime = 0f;
        hasFiredFirstShot = false;
        lastFireTime = 0f;

        Debug.Log("Enemy: Checkpoint reset - stopped attacking and cleared target");
    }

    private void DeactivateAllChildren()
    {
        // 设置死亡状态为true
        isDead = true;
        
        // 遍历所有子物体并停用它们
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(false);
        }
    }

    private void ReactivateAndReset()
    {
        // 重新激活所有子物体
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(true);
        }

        // 刷新敌人状态
        ResetEnemyState();
        
        // 设置死亡状态为false
        isDead = false;
        
        Debug.Log("Enemy reactivated and state reset!");
    }

    private System.Collections.IEnumerator ReactivateAfterDelay()
    {
        // 等待5秒
        yield return new WaitForSeconds(5f);
        
        // 重新激活并重置状态
        ReactivateAndReset();
    }

    private void ResetEnemyState()
    {
        // 重置生命值
        health = 100;
        
        // 重置射击相关变量
        aimingTime = 0f;
        lastFireTime = 0f;
        hasFiredFirstShot = false;
        
        // 重置目标（如果需要）
        target = null; // 根据游戏需求决定是否重置目标
        
        // 重置EnemyCheckBox的检测状态
        ResetCheckBoxState();
    }
    
    private void ResetCheckBoxState()
    {
        // 获取EnemyCheckBox组件并重置其状态
        EnemyCheckBox checkBox = GetComponentInChildren<EnemyCheckBox>();
        if (checkBox != null)
        {
            checkBox.ResetDetection();
            Debug.Log("EnemyCheckBox state reset!");
        }
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