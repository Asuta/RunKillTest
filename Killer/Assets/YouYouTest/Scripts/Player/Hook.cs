using UnityEngine;

public class Hook : MonoBehaviour
{
    private Transform playerCameraT; // 缓存相机引用
    private bool _isGreen = false;   // 当前是否为绿色状态

    public float lightDistance = 5f;

    //test
    public MeshRenderer shortSign;
    public GameObject scopeMesh;
    
    private float _radius = 0.5f;
    public float radius
    {
        get => _radius;
        set => SetRadius(value);
    }
    
    private float _lastRadiusValue; // 用于检测radius变化的临时变量



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

        // 记录初始radius值
        _lastRadiusValue = radius;

        // 主动获取当前播放状态并设置scopeMesh
        bool currentPlayState = GameManager.Instance.IsPlayMode;
        OnPlayStateChange(currentPlayState);

        // 监听播放状态改变事件
        GlobalEvent.IsPlayChange.AddListener(OnPlayStateChange);
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

            // 如果这个hook是最小夹角的，设置为蓝色（覆盖之前的颜色）
            if (GameManager.Instance.ClosestAngleHook == transform)
            {
                shortSign.material.color = Color.blue;
            }
        }
    }

    /// <summary>
    /// 设置radius值的事件驱动方法
    /// </summary>
    /// <param name="newRadius">新的radius值</param>
    public void SetRadius(float newRadius)
    {
        if (!Mathf.Approximately(newRadius, _radius))
        {
            Debug.Log("hahaha");
            _radius = newRadius;
            _lastRadiusValue = newRadius;
        }
    }

    /// <summary>
    /// 在编辑器中值改变时调用（用于监听Inspector中的变化）
    /// </summary>
    void OnValidate()
    {
        if (!Mathf.Approximately(_radius, _lastRadiusValue))
        {
            Debug.Log("hahaha");
            _lastRadiusValue = _radius;
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

        // 取消监听播放状态改变事件
        GlobalEvent.IsPlayChange.RemoveListener(OnPlayStateChange);
    }

    /// <summary>
    /// 播放状态改变事件处理方法
    /// </summary>
    /// <param name="isPlaying">播放状态，true为播放中，false为暂停</param>
    private void OnPlayStateChange(bool isPlaying)
    {
        if (scopeMesh != null)
        {
            // 当播放状态为true时，禁用scopeMesh；为false时，启用scopeMesh
            scopeMesh.SetActive(!isPlaying);
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
