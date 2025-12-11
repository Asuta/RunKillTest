using System.Threading;
using UnityEditor;
using UnityEngine;

public class TurretBullet : MonoBehaviour
{
    public float speed = 10f;
    public Transform target;
    public AutoTurret createTurret;
    public GameObject greenEffect;
    public GameObject greenExplosion;
    public GameObject redEffect;
    public GameObject redExplosion;
    public GameObject changeEffect;
    [SerializeField]
    private bool isBack;
    private bool currentEffectState;

    private Vector3 originPos;

    void Start()
    {
        currentEffectState = isBack;
        UpdateEffectState();
        originPos = transform.position;
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
    }

    void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }

    void Update()
    {
        if (target != null)
        {
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
                float tProj = Vector3.Dot(transform.position - A, AB) / (currentDistance * currentDistance);
                tProj = Mathf.Clamp01(tProj);
                Vector3 projPoint = A + AB * tProj;

                float moveDistance = speed * Time.deltaTime;
                float remaining = (B - projPoint).magnitude;
                if (moveDistance >= remaining)
                {
                    transform.position = B;
                }
                else
                {
                    Vector3 movedPos = projPoint + dir * moveDistance;
                    float tNew = Vector3.Dot(movedPos - A, AB) / (currentDistance * currentDistance);
                    tNew = Mathf.Clamp01(tNew);
                    transform.position = A + AB * tNew;
                }
            }
        }
        else if (isBack || target == null)
        {
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
            // Hit enemy or turret?
            // For now, let's assume it can hit enemies too if we want, but mainly it should hit the turret back?
            // The original code hit "Enemy". Let's check if it hits a Turret.
            if (other.gameObject.GetComponent<AutoTurret>() != null)
            {
                other.gameObject.GetComponent<AutoTurret>().TakeDamage(20);
                if (greenExplosion != null) Instantiate(greenExplosion, transform.position, transform.rotation);
                Destroy(gameObject);
            }
            else if (other.gameObject.tag == "Enemy")
            {
                other.attachedRigidbody.GetComponent<ICanBeHit>()?.TakeDamage(20);
                if (greenExplosion != null) Instantiate(greenExplosion, transform.position, transform.rotation);
                Destroy(gameObject);
            }
        }
        else
        {
            if (other.gameObject.name == "PlayerHit")
            {
                Debug.Log("Turret bullet hit player counter");
                isBack = true;
                if (currentEffectState != isBack) UpdateEffectState();
                GetComponent<MeshRenderer>().material.color = Color.green;
                
                if (createTurret != null && createTurret.body != null)
                {
                    target = createTurret.body;
                    originPos = transform.position;
                }
                else
                {
                    if (isBack && greenExplosion != null) Instantiate(greenExplosion, transform.position, transform.rotation);
                    Destroy(gameObject);
                }
            }

            if (other.gameObject.name == "PlayerDefense")
            {
                Debug.Log("Turret bullet hit defense");
                if (isBack && greenExplosion != null) Instantiate(greenExplosion, transform.position, transform.rotation);
                else if (!isBack && redExplosion != null) Instantiate(redExplosion, transform.position, transform.rotation);
                Destroy(gameObject);
            }

            if (other.gameObject.tag == "Player")
            {
                Debug.Log("Turret bullet hit player");
                other.attachedRigidbody.GetComponent<ICanBeHit>()?.TakeDamage(20);
                if (isBack && greenExplosion != null) Instantiate(greenExplosion, transform.position, transform.rotation);
                else if (!isBack && redExplosion != null) Instantiate(redExplosion, transform.position, transform.rotation);
                Destroy(gameObject);
            }
        }
    }

    private void UpdateEffectState()
    {
        if (greenEffect != null && redEffect != null)
        {
            if (isBack)
            {
                redEffect.SetActive(false);
                greenEffect.SetActive(true);
                if (!currentEffectState && changeEffect != null)
                {
                    Instantiate(changeEffect, transform.position, transform.rotation);
                }
            }
            else
            {
                greenEffect.SetActive(false);
                redEffect.SetActive(true);
            }
            currentEffectState = isBack;
        }
    }

    private void OnCheckPointReset()
    {
        Destroy(gameObject);
    }
}
