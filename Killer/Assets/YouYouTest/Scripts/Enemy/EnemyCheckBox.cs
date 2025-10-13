using UnityEngine;
using VInspector;

public class EnemyCheckBox : MonoBehaviour
{
    public Enemy enemy;
    private bool isFinded   = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
    }

    void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }

    private void OnCheckPointReset()
    {
        isFinded = false;
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

            //find the IPlayerHeadProvider interface implementation assed to the playerRigidbody
            IPlayerHeadProvider playerHeadProvider = playerRigidbody.GetComponent<IPlayerHeadProvider>();
            if (playerHeadProvider != null)
            {
                enemy.SetTarget(playerHeadProvider.GetPlayerHead());
                isFinded = true;
            }
            
        }

    }
}
