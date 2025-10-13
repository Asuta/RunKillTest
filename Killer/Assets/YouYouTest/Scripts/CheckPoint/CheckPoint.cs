using System;
using UnityEngine;
using VInspector;

public class CheckPoint : MonoBehaviour
{
    // 定义三种状态
    public enum CheckPointState
    {
        Inactive,      // 未激活
        Activating,    // 正在激活
        Activated      // 激活过了
    }

    // 状态对应的颜色
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activatingColor = Color.yellow;
    [SerializeField] private Color activatedColor = Color.green;

    // 当前状态
    private CheckPointState currentState = CheckPointState.Inactive;
    
    // 材质引用
    private Material material;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取材质组件
        material = GetComponent<Renderer>().material;
        // 初始化颜色
        UpdateMaterialColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 触发器检测 - 当玩家进入触发器时
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家
        if (other.CompareTag("Player") && currentState == CheckPointState.Inactive)
        {
            // 开始激活过程
            SetState(CheckPointState.Activating);
        }
    }

    // 更新材质颜色
    private void UpdateMaterialColor()
    {
        switch (currentState)
        {
            case CheckPointState.Inactive:
                material.color = inactiveColor;
                break;
            case CheckPointState.Activating:
                material.color = activatingColor;
                break;
            case CheckPointState.Activated:
                material.color = activatedColor;
                break;
        }
    }

    // 设置状态
    public void SetState(CheckPointState newState)
    {
        currentState = newState;
        UpdateMaterialColor();
    }

    // 获取当前状态
    public CheckPointState GetCurrentState()
    {
        return currentState;
    }

    [Button("Set Activating")]
    public void TestSetState()
    {
        SetState(CheckPointState.Activating);
    }

    [Button("Set Activated")]
    public void TestSetState2()
    {
        SetState(CheckPointState.Activated);
    }

    [Button("Set Inactive")]
    public void TestSetState3()
    {
        SetState(CheckPointState.Inactive);
    }
}
