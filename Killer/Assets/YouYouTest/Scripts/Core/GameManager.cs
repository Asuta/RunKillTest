using UnityEngine;
using System.Collections.Generic;
using System;


public class GameManager : MonoBehaviour
{
    public bool needLog;
    // 单例实例
    private static GameManager _instance;

    // 公开的实例访问器
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();

                if (_instance == null)
                {
                    GameObject gameManagerObject = new GameObject("GameManager");
                    _instance = gameManagerObject.AddComponent<GameManager>();
                }
            }

            return _instance;
        }
    }

    // 确保只有一个实例存在
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }


    [SerializeField]
    private Transform _playerCameraT;
    public Transform PlayerCameraT
    {
        get
        {
            if (_playerCameraT == null)
            {
                CustomLog.LogError(needLog, "玩家相机未分配！请确保PlayerSetting组件设置了相机引用。");
            }
            return _playerCameraT;
        }
        set
        {
            _playerCameraT = value;
            if (_playerCameraT != null)
            {
                CustomLog.Log(needLog, "玩家相机已在GameManager中设置");
            }
        }
    }

    // 存储绿色状态的Hook Transform列表
    [SerializeField]
    private List<Transform> _greenHooks = new List<Transform>();

    // 公开的绿色Hook列表访问器（只读）
    public IReadOnlyList<Transform> GreenHooks => _greenHooks;

    // 存储夹角最小的Hook Transform
    [SerializeField]
    private Transform _closestAngleHook;
    public Transform ClosestAngleHook => _closestAngleHook;

    // 注册绿色Hook
    public void RegisterGreenHook(Transform hookTransform)
    {
        if (hookTransform != null && !_greenHooks.Contains(hookTransform))
        {
            _greenHooks.Add(hookTransform);
            CustomLog.Log(needLog, $"绿色钩子已注册: {hookTransform.name}");
        }
    }

    // 注销绿色Hook
    public void UnregisterGreenHook(Transform hookTransform)
    {
        if (hookTransform != null && _greenHooks.Contains(hookTransform))
        {
            _greenHooks.Remove(hookTransform);
            CustomLog.Log(needLog, $"绿色钩子已注销: {hookTransform.name}");
            
            // 如果移除的是当前最小夹角的Hook，需要重新计算
            if (hookTransform == _closestAngleHook)
            {
                _closestAngleHook = null;
            }
        }
    }

    // 计算与相机forward方向的夹角（度）
    private float CalculateAngleToCamera(Transform hookTransform)
    {
        if (_playerCameraT == null || hookTransform == null)
            return float.MaxValue;

        Vector3 toHook = (hookTransform.position - _playerCameraT.position).normalized;
        float angle = Vector3.Angle(_playerCameraT.forward, toHook);
        return angle;
    }

    // 找到与相机forward方向夹角最小的Hook
    private void FindClosestAngleHook()
    {
        if (_playerCameraT == null || _greenHooks.Count == 0)
        {
            _closestAngleHook = null;
            return;
        }

        Transform closestHook = null;
        float minAngle = float.MaxValue;

        foreach (var hook in _greenHooks)
        {
            if (hook == null) continue;
            
            float angle = CalculateAngleToCamera(hook);
            if (angle < minAngle)
            {
                minAngle = angle;
                closestHook = hook;
            }
        }

        // 检查最小夹角是否大于45度，如果大于45度则不作为最近钩子
        if (minAngle > 35f)
        {
            closestHook = null;
            minAngle = float.MaxValue;
        }

        // Check if the closest hook has changed
        if (_closestAngleHook != closestHook)
        {
            // Revert the old closest hook back to green
            if (_closestAngleHook != null)
            {
                var oldHook = _closestAngleHook.GetComponent<Hook>();
                if (oldHook != null && oldHook.shortSign != null)
                {
                    oldHook.shortSign.material.color = Color.green;
                }
            }

            // Set the new closest hook to blue
            if (closestHook != null)
            {
                var newHook = closestHook.GetComponent<Hook>();
                if (newHook != null && newHook.shortSign != null)
                {
                    newHook.shortSign.material.color = Color.blue;
                }
                CustomLog.Log(needLog, $"新的最近角度钩子: {closestHook.name}, 角度: {minAngle:F1} 度");
            }

            _closestAngleHook = closestHook;
        }
    }

    public CheckPoint nowActivateCheckPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 不再进行严格的null检查，因为相机可能在之后设置
        if (_playerCameraT == null)
        {
            CustomLog.LogWarning(needLog, "玩家相机尚未分配，可能会在稍后设置。");
        }

        // 注册检查点激活事件
        GlobalEvent.CheckPointActivate.AddListener(OnCheckPointActivate);
    }

    private void OnDestroy() {
        GlobalEvent.CheckPointActivate.RemoveListener(OnCheckPointActivate);
    }

    private void OnCheckPointActivate(CheckPoint checkPoint) {
        CustomLog.Log(needLog, $"检查点激活: {checkPoint.name}");
        
        // 如果已经有激活的检查点，将其状态设置为已激活
        if (nowActivateCheckPoint != null)
        {
            nowActivateCheckPoint.SetState(CheckPoint.CheckPointState.Activated);
        }
        
        nowActivateCheckPoint = checkPoint; // 存储激活的检查点引用
        
        // 检查点已经在CheckPoint.cs中设置为Activating状态
        // 这里不需要再次设置状态，保持Activating状态让其他逻辑处理
    }

    // Update is called once per frame
    void Update()
    {
        // 每帧更新最小夹角的Hook
        FindClosestAngleHook();
    }
}
