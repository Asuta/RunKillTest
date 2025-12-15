
using UnityEngine;

public class PlayerUIMove : MonoBehaviour
{
    public Transform moveTarget;
    public Vector3 offset;
    public Vector3 offset2;
    public Transform lookTarget;
    public Transform UIRoot;

    public float maxDistance = 10f;
     
    
    public float lerpSpeed = 10f; // 插值速度，可以调整这个值来控制插值的快慢
    
    private float menuKeyPressTime = -1f; // 记录菜单键按下的时间
    private const float CLICK_TIME_THRESHOLD = 0.2f; // 单击时间阈值
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (moveTarget != null )
        {
            Vector3 targetPosition = moveTarget.position + offset + offset2;
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
            menuKeyPressTime = Time.time; // 记录按下时间
        }
        
        // 检测左手柄菜单键释放
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasReleasedThisFrame())
        {
            // 检查是否在0.1秒内释放（单击）
            if (menuKeyPressTime > 0 && (Time.time - menuKeyPressTime) <= CLICK_TIME_THRESHOLD)
            {
                if (UIRoot != null)
                {
                    UIRoot.gameObject.SetActive(!UIRoot.gameObject.activeSelf);
                }
            }
            menuKeyPressTime = -1f; // 重置按下时间
        }
    }
}
