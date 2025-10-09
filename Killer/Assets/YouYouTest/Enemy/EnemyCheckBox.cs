using UnityEngine;

public class EnemyCheckBox : MonoBehaviour
{
    public Enemy enemy;
    private bool isFinded   = false;
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
        if (isFinded) return;
        // if the layer name is"Player"
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            //find the rigdidbody assed to the collider
            Rigidbody playerRigidbody = other.attachedRigidbody;

            //find the PlayerMove script assed to the playerRigidbody
            PlayerMove playerMove = playerRigidbody.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                enemy.SetTarget(playerMove.playerHead);   
                isFinded = true;
            }
            
        }

    }
}
