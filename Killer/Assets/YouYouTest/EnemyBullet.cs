using UnityEditor;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public Transform target;
    public Enemy createEnemy;
    [SerializeField]
    private bool isBack;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // move towards target
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
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
            // Debug.Log("Enemy Bullet OnTriggerEnter:" + other.gameObject.name);
            if (other.gameObject.name == "PlayerHit")
            {
                // hit the player
                Debug.Log("Enemy Bullet plaer hit");
                isBack = true;
                target = createEnemy.EnemyBody;
            }

            if (other.gameObject.name == "PlayerDefense")
            {
                // hit the player
                Debug.Log("Enemy Bullet Hit Defense");
                Destroy(gameObject);
                // createEnemy.OnHitDefense();
            }

            if (other.gameObject.name == "CapsuleBody")
            {
                // hit the player
                Debug.Log("Enemy Bullet Player");
                other.attachedRigidbody.GetComponent<ICanBeHit>().TakeDamage(20);

                // destroy this bullet
                Destroy(gameObject);
            }

        }


    }
}
