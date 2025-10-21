using UnityEngine;
using VInspector;

public class EnemyCheckBox : MonoBehaviour
{
    public Enemy enemy;
    private bool isFinded   = false;
    private MeshRenderer meshRenderer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
        GlobalEvent.IsPlayChange.AddListener(OnIsPlayChange);
        
        // 获取MeshRenderer组件
        meshRenderer = GetComponent<MeshRenderer>();
        
        // 初始化时根据当前PlayMode状态设置Mesh Renderer
        if (meshRenderer != null)
        {
            meshRenderer.enabled = !GameManager.Instance.IsPlayMode;
        }
    }

    void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
        GlobalEvent.IsPlayChange.RemoveListener(OnIsPlayChange);
    }

    private void OnCheckPointReset()
    {
        isFinded = false;
    }
    
    private void OnIsPlayChange(bool isPlayMode)
    {
        // 根据PlayMode状态启用或禁用Mesh Renderer
        if (meshRenderer != null)
        {
            meshRenderer.enabled = !isPlayMode;
        }
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
