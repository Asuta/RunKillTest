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
    // Start在MonoBehaviour创建后，在第一次执行Update之前被调用一次
    void Start()
    {
        // 初始化效果状态
        currentEffectState = isBack;
        UpdateEffectState();
    }

    // Update每帧调用一次
    void Update()
    {
        // 向目标移动
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
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
}
