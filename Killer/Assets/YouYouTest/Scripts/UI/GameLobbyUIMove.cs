
using UnityEngine;

public class GameLobbyUIMove : MonoBehaviour
{
    public GameObject subUI;
    public Transform moveTarget;
    public Vector3 offset;
    public Vector3 offset2;
    public Transform lookTarget;

    public float maxDistance = 10f;
     
    
    public float lerpSpeed = 10f; // 插值速度，可以调整这个值来控制插值的快慢
    
    // 存储原始值的变量
    private Vector3 originalOffset;
    
    // 跟踪菜单键状态的变量
    private bool menuKeyPressed = false;
    private float menuKeyPressTime = 0f;
    private const float QUICK_PRESS_THRESHOLD = 0.2f; // 快速按下的时间阈值（秒）
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 保存原始值
        originalOffset = offset;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (moveTarget != null && GameManager.Instance != null)
        {
            Vector3 targetPosition = moveTarget.position + originalOffset + offset2;
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // 如果距离大于maxDistance，直接移动到目标位置
            if (distance > maxDistance)
            {
                transform.position = targetPosition;
            }
            else
            {
                // 使用插值平滑地移动到目标位置
                transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.deltaTime);
            }
        }

        if (lookTarget != null)
        {
            // 使用反向lookat，使得物体背离lookTarget的方向
            Vector3 directionFromTarget = transform.position - lookTarget.position;
            transform.rotation = Quaternion.LookRotation(directionFromTarget);
        }
        
        // 检测左手柄菜单键按下
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasPressedThisFrame())
        {
            menuKeyPressed = true;
            menuKeyPressTime = Time.time;
            Debug.Log("左手柄菜单键按下");
        }
        
        // 检测左手柄菜单键抬起（只有快速按下后抬起的情况）
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasReleasedThisFrame() && menuKeyPressed)
        {
            float pressDuration = Time.time - menuKeyPressTime;
            menuKeyPressed = false;
            
            // 只有按下时间小于阈值才认为是快速按下
            if (pressDuration < QUICK_PRESS_THRESHOLD)
            {
                Debug.Log($"左手柄菜单键快速抬起，按下时长: {pressDuration:F2}秒");
                
                // 切换subUI的active状态
                if (subUI != null)
                {
                    bool newState = !subUI.activeSelf;
                    subUI.SetActive(newState);
                    Debug.Log("subUI状态切换为: " + (newState ? "激活" : "禁用"));
                }
                else
                {
                    Debug.LogWarning("subUI为空");
                }
            }
            else
            {
                Debug.Log($"左手柄菜单键长按后抬起，按下时长: {pressDuration:F2}秒，不触发UI切换");
            }
        }
    }
}
