using UnityEngine;
using VInspector;

public class DeathTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            //find the rigdidbody assed to the collider, 找到碰撞体所附加的刚体组件。
            Rigidbody playerRigidbody = other.attachedRigidbody;

            //find the IPlayerHeadProvider interface implementation assed to the playerRigidbody, 找到实现了ICanBeHit接口的组件。
            ICanBeHit playerHeadProvider = playerRigidbody.GetComponent<ICanBeHit>();
            if (playerHeadProvider != null)
            {
                playerHeadProvider.OnDeath();; //调用接口方法OnDeath()，实现死亡逻辑。
            }
        }
    }
}
