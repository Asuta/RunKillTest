using UnityEngine;

public class TurretCheckBox : MonoBehaviour
{
    public AutoTurret turret;
    private bool isFound = false;
    private MeshRenderer meshRenderer;

    void Start()
    {
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
        GlobalEvent.IsPlayChange.AddListener(OnIsPlayChange);
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.enabled = !GameManager.Instance.IsPlayMode;
    }

    void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
        GlobalEvent.IsPlayChange.RemoveListener(OnIsPlayChange);
    }

    private void OnCheckPointReset()
    {
        ResetDetection();
    }

    public void ResetDetection()
    {
        isFound = false;
    }

    private void OnIsPlayChange(bool isPlayMode)
    {
        if (meshRenderer != null) meshRenderer.enabled = !isPlayMode;
    }

    void OnTriggerEnter(Collider other)
    {
        TryDetectPlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDetectPlayer(other);
    }

    private void TryDetectPlayer(Collider other)
    {
        if (!GameManager.Instance.IsPlayMode) return;
        if (isFound) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Rigidbody playerRigidbody = other.attachedRigidbody;
            if (playerRigidbody != null)
            {
                IPlayerHeadProvider playerHeadProvider = playerRigidbody.GetComponent<IPlayerHeadProvider>();
                if (playerHeadProvider != null)
                {
                    turret.SetTarget(playerHeadProvider.GetPlayerHead());
                    isFound = true;
                }
            }
        }
    }
}
