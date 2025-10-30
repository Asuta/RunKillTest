using System.Threading;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public Transform target;
    public Enemy createEnemy;
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
    // Start在MonoBehaviour创建后，在第一次执行Update之前被调用一次
    void Start()
    {
        // 初始化效果状态
        currentEffectState = isBack;
        UpdateEffectState();

        // 初始化起点为子弹生成时的位置
        originPos = transform.position;

        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
    }

    void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }



    // Update每帧调用一次
    void Update()
    {
        // 向目标移动（使用投影基准 + 固定世界位移算法）
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
            }
            else
            {
                Vector3 dir = AB / currentDistance;
                // 将当前位置投影到 AB 线段上（夹制到 [A,B]）
                float tProj = Vector3.Dot(transform.position - A, AB) / (currentDistance * currentDistance);
                tProj = Mathf.Clamp01(tProj);
                Vector3 projPoint = A + AB * tProj;

                // 以投影点为基准沿直线移动固定的世界距离
                float moveDistance = speed * Time.deltaTime;
                float remaining = (B - projPoint).magnitude;
                if (moveDistance >= remaining)
                {
                    // 到达目标
                    transform.position = B;
                }
                else
                {
                    Vector3 movedPos = projPoint + dir * moveDistance;
                    // 投影回线段并设置位置
                    float tNew = Vector3.Dot(movedPos - A, AB) / (currentDistance * currentDistance);
                    tNew = Mathf.Clamp01(tNew);
                    transform.position = A + AB * tNew;
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
}
