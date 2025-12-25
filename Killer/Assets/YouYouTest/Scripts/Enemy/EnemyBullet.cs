using System.Threading;
using UnityEditor;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public float travelTime = 3f; // 子弹飞行到目标所需的时间（秒）
    public Transform target;
    public Enemy createEnemy;
    public Vector3 explosionPosition; // 指定的爆炸位置
    public bool useExplosionPosition = false; // 是否使用指定位置爆炸
    public GameObject greenEffect;
    public GameObject greenExplosion;
    public GameObject redEffect;
    public GameObject redExplosion;
    public GameObject changeEffect;
    [SerializeField]
    private bool isBack;
    private bool currentEffectState; // 跟踪当前效果状态

    // 起始点（用于基于线段的移动算法）
    private Vector3 originPos;
    // 子弹生成时间（用于基于时间的移动算法）
    private float spawnTime;
    // Start在MonoBehaviour创建后，在第一次执行Update之前被调用一次
    void Start()
    {
        // 初始化效果状态
        currentEffectState = isBack;
        UpdateEffectState();

        // 初始化起点为子弹生成时的位置
        originPos = transform.position;
        // 记录生成时间
        spawnTime = Time.time;

        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
    }

    void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }



    // Update每帧调用一次
    void Update()
    {
        // 向目标移动（使用基于时间的百分比移动算法）
        if (target != null)
        {
            // 如果 originPos 未初始化（Vector3.zero），则用当前位置作为起点
            if (originPos == Vector3.zero)
                originPos = transform.position;

            Vector3 A = originPos;
            Vector3 B = target.position;
            Vector3 AB = B - A;
            float currentDistance = AB.magnitude;
            if (currentDistance <= Mathf.Epsilon)
            {
                transform.position = B;
                // 如果已经到达目标且是反弹状态，直接销毁
                if (isBack)
                {
                    ExplodeAndDestroy();
                    return;
                }
            }
            else
            {
                // 计算经过的时间比例（0到1之间）
                float elapsedTime = Time.time - spawnTime;
                float t = elapsedTime / travelTime;
                t = Mathf.Clamp01(t);

                // 使用线性插值计算当前位置
                Vector3 nextPos = Vector3.Lerp(A, B, t);

                // 检查这一帧的移动是否经过了指定爆炸位置（防止速度过快穿透）
                if (useExplosionPosition)
                {
                    if (CheckPassThrough(transform.position, nextPos, explosionPosition))
                    {
                        transform.position = explosionPosition;
                        ExplodeAndDestroy();
                        return;
                    }
                }

                transform.position = nextPos;

                // 如果到达目标（t >= 1）
                if (t >= 1f)
                {
                    // 如果是反弹状态，到达目标（敌人身体）时直接销毁
                    if (isBack)
                    {
                        ExplodeAndDestroy();
                        return;
                    }
                }
            }
        }
        else if (isBack || target == null)
        {
            // 没有目标，销毁自身
            // 根据当前状态生成对应的特效
            if (isBack && greenExplosion != null)
            {
                Instantiate(greenExplosion, transform.position, transform.rotation);
            }
            else if (!isBack && redExplosion != null)
            {
                Instantiate(redExplosion, transform.position, transform.rotation);
            }
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {

        if (isBack)
        {
            if (other.gameObject.tag == "Enemy")
            {
                other.attachedRigidbody.GetComponent<Enemy>().OnHitByBullet();
                // 根据当前状态生成对应的特效
                if (isBack && greenExplosion != null)
                {
                    Instantiate(greenExplosion, transform.position, transform.rotation);
                }
                Destroy(gameObject);

            }
        }
        else
        {
            // Debug.Log("敌人子弹触发进入:" + other.gameObject.name);
            if (other.gameObject.name == "PlayerHit")
            {
                // 击中玩家的反击
                Debug.Log("敌人子弹击中玩家的反击");
                isBack = true;
                // 只在状态改变时更新效果
                if (currentEffectState != isBack)
                {
                    UpdateEffectState();
                }
                //让自己的mesh 变成绿色
                GetComponent<MeshRenderer>().material.color = Color.green;
                if (createEnemy.EnemyBody != null)
                {
                    target = createEnemy.EnemyBody;
                    // 反弹后以当前点作为新的起点
                    originPos = transform.position;
                    // 重置生成时间
                    spawnTime = Time.time;
                }
                else
                {
                    // 根据当前状态生成对应的特效
                    if (isBack && greenExplosion != null)
                    {
                        Instantiate(greenExplosion, transform.position, transform.rotation);
                    }
                    Destroy(gameObject);
                }
            }

            if (other.gameObject.name == "PlayerDefense")
            {
                // 击中玩家
                Debug.Log("敌人子弹击中防御");
                // 根据当前状态生成对应的特效
                if (isBack && greenExplosion != null)
                {
                    Instantiate(greenExplosion, transform.position, transform.rotation);
                }
                else if (!isBack && redExplosion != null)
                {
                    Instantiate(redExplosion, transform.position, transform.rotation);
                }
                Destroy(gameObject);
                // createEnemy.OnHitDefense();
            }

            if (other.gameObject.tag == "Player")
            {
                // 击中玩家
                Debug.Log("敌人子弹击中玩家身体");
                other.attachedRigidbody.GetComponent<ICanBeHit>().TakeDamage(20);

                // 根据当前状态生成对应的特效
                if (isBack && greenExplosion != null)
                {
                    Instantiate(greenExplosion, transform.position, transform.rotation);
                }
                else if (!isBack && redExplosion != null)
                {
                    Instantiate(redExplosion, transform.position, transform.rotation);
                }
                // 销毁这颗子弹
                Destroy(gameObject);
            }

        }


    }

    // 根据isBack状态更新效果显示
    private void UpdateEffectState()
    {
        if (greenEffect != null && redEffect != null)
        {
            if (isBack)
            {
                // isBack为true时：关闭redEffect，打开greenEffect
                redEffect.SetActive(false);
                greenEffect.SetActive(true);

                // 当从false切换到true时，生成changeEffect特效
                if (!currentEffectState && changeEffect != null)
                {
                    Instantiate(changeEffect, transform.position, transform.rotation);
                }
            }
            else
            {
                // isBack为false时：关闭greenEffect，打开redEffect
                greenEffect.SetActive(false);
                redEffect.SetActive(true);
            }
            // 更新当前状态跟踪
            currentEffectState = isBack;
        }
    }

    private void OnCheckPointReset()
    {
        // 检查点重置时销毁子弹
        Destroy(gameObject);
    }


    // 爆炸并销毁子弹的方法
    private void ExplodeAndDestroy()
    {
        // 根据当前状态生成对应的特效
        if (isBack && greenExplosion != null)
        {
            Instantiate(greenExplosion, transform.position, transform.rotation);
        }
        else if (!isBack && redExplosion != null)
        {
            Instantiate(redExplosion, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }

    // 检测线段 start->end 是否经过 point（带有一定的宽容度）
    private bool CheckPassThrough(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 segment = end - start;
        float segmentLenSqr = segment.sqrMagnitude;
        
        // 如果移动距离极小，直接比较距离
        if (segmentLenSqr < Mathf.Epsilon)
            return Vector3.Distance(start, point) < 0.2f;

        Vector3 pointToStart = point - start;
        // 计算点在直线上的投影比例 t
        float t = Vector3.Dot(pointToStart, segment) / segmentLenSqr;

        // 如果 t 在 [0, 1] 之间，说明投影点在线段上
        if (t >= 0f && t <= 1f)
        {
            Vector3 projection = start + segment * t;
            // 检查点到线段的垂直距离是否足够近
            if (Vector3.Distance(projection, point) < 0.3f) // 0.3f 为检测半径
            {
                return true;
            }
        }
        return false;
    }
}
