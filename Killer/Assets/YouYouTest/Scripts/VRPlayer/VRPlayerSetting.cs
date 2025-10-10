using UnityEngine;

public class VRPlayerSetting : MonoBehaviour
{
    public Transform playerCameraT;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GameManager.Instance.PlayerCameraT = playerCameraT;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
