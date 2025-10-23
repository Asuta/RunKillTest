using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;


public class VRPlayerSetting : MonoBehaviour
{
    public Transform playerCameraT;

    public XROrigin xrOrigin;
    public Transform VrPlayerRig;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GameManager.Instance.PlayerCameraT = playerCameraT;
        
        // 订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.AddListener(OnGameModeChange);
        
        // 订阅GameManager就绪事件
        GameManager.OnGameManagerReady += OnGameManagerReady;
    }

    void OnDestroy()
    {
        // 取消订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.RemoveListener(OnGameModeChange);
        
        // 取消订阅GameManager就绪事件
        GameManager.OnGameManagerReady -= OnGameManagerReady;
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private void OnGameModeChange(bool isPlayMode)
    {
        // 当游戏模式变化时，根据状态设置VR组件
        if (xrOrigin != null && VrPlayerRig != null)
        {
            if (isPlayMode)
            {
                // 游戏模式：激活VR Player
                xrOrigin.gameObject.SetActive(false);
                xrOrigin.gameObject.SetActive(true);
                VrPlayerRig.gameObject.SetActive(true);
                Debug.Log("VR Player Rig 已激活");
            }
            else
            {
                // 编辑模式：禁用VR Player
                xrOrigin.enabled = false;
                VrPlayerRig.gameObject.SetActive(false);
                Debug.Log("VR Player Rig 已禁用");
            }
        }
    }
    
    private void OnGameManagerReady(bool initialMode)
    {
        // 当GameManager就绪时，应用初始游戏模式
        OnGameModeChange(initialMode);
        Debug.Log($"VRPlayerSetting通过GameManager就绪事件初始化，当前模式: {(initialMode ? "游戏模式" : "编辑模式")}");
    }
}
