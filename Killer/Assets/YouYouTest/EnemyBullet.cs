using UnityEditor;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public Transform target;
    public Enemy createEnemy;
    [SerializeField]
    private bool isBack;
    // Start在MonoBehaviour创建后，在第一次执行Update之前被调用一次
    void Start()
    {

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
                Destroy(gameObject);

            }
        }
        else
        {
            // Debug.Log("敌人子弹触发进入:" + other.gameObject.name);
            if (other.gameObject.name == "PlayerHit")
            {
                // 击中玩家
                Debug.Log("敌人子弹击中玩家");
                isBack = true;
                //让自己的mesh 变成绿色
                GetComponent<MeshRenderer>().material.color = Color.green;
                if (createEnemy.EnemyBody != null)
                {
                    target = createEnemy.EnemyBody;
                }
                else
                {
                    Destroy(gameObject);
                }
            }

            if (other.gameObject.name == "PlayerDefense")
            {
                // 击中玩家
                Debug.Log("敌人子弹击中防御");
                Destroy(gameObject);
                // createEnemy.OnHitDefense();
            }

            if (other.gameObject.name == "CapsuleBody")
            {
                // 击中玩家
                Debug.Log("敌人子弹击中玩家身体");
                other.attachedRigidbody.GetComponent<ICanBeHit>().TakeDamage(20);

                // 销毁这颗子弹
                Destroy(gameObject);
            }

        }


    }
}
