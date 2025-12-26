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


        // 配置 firstLateTime
        items.Add(new ConfigItem(
            "lateTime",
            firstLateTime,
            ConfigType.Float,
            (newValue) =>
            {
                firstLateTime = Convert.ToSingle(newValue);
                Debug.Log($"[Enemy Config] lateTime updated to: {firstLateTime}");
            }
        ));

        // 配置 IntervalTime
        items.Add(new ConfigItem(
            "IntervalTime",
            IntervalTime,
            ConfigType.Float,
            (newValue) =>
            {
                IntervalTime = Convert.ToSingle(newValue);
                Debug.Log($"[Enemy Config] IntervalTime updated to: {IntervalTime}");
            }
        ));

        // 配置 RespawnTime
        items.Add(new ConfigItem(
            "RespawnTime",
            respawnTime,
            ConfigType.Float,
            (newValue) =>
            {
                respawnTime = Convert.ToSingle(newValue);
                Debug.Log($"[Enemy Config] RespawnTime updated to: {respawnTime}");
            }
        ));



        return items;
    }
    #endregion

    #region 变量声明
    public int health = 100;
    private int initialHealth; // 保存初始血量
    public float firstLateTime;
    public float IntervalTime;
    public float respawnTime = 5f; // 复活间隔时间（秒）

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

    // 球形射线检测相关
    public float sphereCastRadius = 0.5f;
    public LayerMask detectionLayer;

    //死亡状态
    private bool isDead = false;

    //特效
    public GameObject deathEffect;
    public GameObject hitEffect;

    //声音
    public AudioSource audioSource;
    public AudioClip aimSound;
    public AudioClip fireSound;


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
        // 保存初始血量
        initialHealth = health;

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
            if (aimingTime >= firstLateTime)
            {
                Fire();
                hasFiredFirstShot = true;
                lastFireTime = Time.time;
            }
        }
        else
        {
            // 后续发射：每隔5秒发射一次
            if (Time.time - lastFireTime >= IntervalTime)
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

        // 播放瞄准声音
        if (audioSource != null && aimSound != null)
        {
            audioSource.PlayOneShot(aimSound);
        }

        // // 创建子弹实例
        // GameObject bullet = Instantiate(bulletPrefab, EnemyBody.position, Quaternion.identity);
        // EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();

        // if (enemyBullet != null)
        // {
        //     enemyBullet.target = target;
        //     enemyBullet.createEnemy = this;
        // }

        // 启动发射 3 秒后的独立检测协程（与子弹存活无关）
        StartCoroutine(DelayedSphereCastCheck(EnemyBody.position, target.position));

        Debug.Log("Enemy fired a bullet!");
    }

    private System.Collections.IEnumerator DelayedSphereCastCheck(Vector3 startPos, Vector3 targetPos)
    {
        // 等待 3 秒
        yield return new WaitForSeconds(3f);

        // 计算方向和距离
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        Debug.Log($"[Enemy] Performing independent SphereCast check. Start: {startPos}, Target: {targetPos}, Distance: {distance}, Radius: {sphereCastRadius}");

        // 使用 SphereCastAll 检测路径上所有对象 (暂时不使用 detectionLayer 以便排查)
        RaycastHit[] hits = Physics.SphereCastAll(startPos, sphereCastRadius, direction, distance);

        if (hits.Length == 0)
        {
            Debug.Log("[Enemy] SphereCast hit nothing at all.");
        }

        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"[Enemy] SphereCast detected: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            if (hit.collider.gameObject.name == "PlayerHit")
            {
                Debug.LogError("Enemy SphereCast hit PlayerHit!");
                // 播放开火声音
                if (audioSource != null && fireSound != null)
                {
                    audioSource.PlayOneShot(fireSound);
                }
                // 播放命中特效
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, hit.point, Quaternion.identity);
                }

            }
            if (hit.collider.gameObject.name == "PlayerDefense")
            {
                Debug.LogError("Enemy SphereCast hit PlayerDefense!");

                // 播放开火声音
                if (audioSource != null && fireSound != null)
                {
                    audioSource.PlayOneShot(fireSound);
                }
                // 播放命中特效
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, hit.point, Quaternion.identity);
                }

            }
        }
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
        // 等待复活间隔时间
        yield return new WaitForSeconds(respawnTime);

        // 重新激活并重置状态
        ReactivateAndReset();
    }

    private void ResetEnemyState()
    {
        // 重置生命值为初始血量
        health = initialHealth;

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

        // 绘制球形射线 - 使用原始的 target.position 作为终点（bullet的目标点）
        DrawSphereCast(EnemyBody.position, target.position);
    }

    private void DrawSphereCast(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        direction.Normalize();

        // 执行球形射线检测
        RaycastHit hit;
        bool hasHit = Physics.SphereCast(start, sphereCastRadius, direction, out hit, distance, detectionLayer);

        // 确定绘制的终点和颜色
        Vector3 actualEnd = hasHit ? hit.point : end;
        Color drawColor = hasHit ? Color.red : Color.green;

        // 参考 NewVRMove2.cs 中的画法，使用 CapsuleWireframeDrawer 绘制球形射线可视化
        // 球形射线在空间中扫过的区域实际上是一个胶囊体
        // 起点球心为 start，终点球心为 actualEnd

        // 计算胶囊体的两个端点
        // 注意：CapsuleWireframeDrawer.DrawCapsuleCastGizmo 内部会处理起点和方向
        // 这里我们直接使用 DrawWireCapsule 来表示当前检测到的范围
        CapsuleWireframeDrawer.DrawWireCapsule(start, actualEnd, sphereCastRadius, drawColor);

        // 如果需要，也可以绘制投射过程的连线
        // CapsuleWireframeDrawer.DrawCapsuleCastGizmo(start, start, sphereCastRadius, actualEnd - start, drawColor);
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