using UnityEngine;

public class Hook : MonoBehaviour
{
    private Transform playerCameraT; // 缓存相机引用
    private bool _isGreen = false;   // 当前是否为绿色状态

    public float lightDistance = 5f;

    //test
    public MeshRenderer shortSign;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 在Start中缓存引用
        playerCameraT = GameManager.Instance.PlayerCameraT;

        // 可选：检查引用是否有效
        if (playerCameraT == null)
        {
            Debug.LogError("Player camera reference is null in Hook.Start()");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 使用缓存的引用
        if (playerCameraT != null && shortSign != null)
        {
            // 计算与相机的平方距离（避免开平方根运算）
            float sqrDistance = (transform.position - playerCameraT.position).sqrMagnitude;
            float sqrLightDistance = lightDistance * lightDistance;

            // Check if this hook is the closest one
            if (GameManager.Instance.ClosestAngleHook == transform)
            {
                // If it's the closest, GameManager will handle the blue color.
                // We just need to ensure it's registered as green.
                if (!_isGreen)
                {
                    _isGreen = true;
                    GameManager.Instance.RegisterGreenHook(transform);
                }
                return; // Skip the rest of the color logic
            }

            // 根据平方距离改变MeshRenderer的颜色
            if (sqrDistance < sqrLightDistance)
            {
                // 距离范围内：设置为绿色
                shortSign.material.color = Color.green;
                
                // 如果之前不是绿色状态，注册到GameManager
                if (!_isGreen)
                {
                    _isGreen = true;
                    GameManager.Instance.RegisterGreenHook(transform);
                }
            }
            else
            {
                // 距离范围外：设置为红色
                shortSign.material.color = Color.red;
                
                // 如果之前是绿色状态，从GameManager注销
                if (_isGreen)
                {
                    _isGreen = false;
                    GameManager.Instance.UnregisterGreenHook(transform);
                }
            }
        }


    }

    // 当对象被禁用或销毁时，确保从列表中移除
    void OnDisable()
    {
        if (_isGreen)
        {
            _isGreen = false;
            GameManager.Instance.UnregisterGreenHook(transform);
        }
    }

    void OnDestroy()
    {
        if (_isGreen)
        {
            _isGreen = false;
            GameManager.Instance.UnregisterGreenHook(transform);
        }
    }

    // 在Scene视图中绘制Gizmos
    void OnDrawGizmos()
    {
        // 设置Gizmos颜色为半透明绿色
        Gizmos.color = new Color(0, 1, 0, 0.3f);

        // 在对象位置绘制球体，使用lightDistance作为半径
        Gizmos.DrawWireSphere(transform.position, lightDistance);
    }
}
