using System;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class CheckPoint : MonoBehaviour
{
    // 定义两种状态
    public enum CheckPointState
    {
        Inactive,      // 未激活
        Activated      // 已激活
    }

    // 状态对应的颜色
    [SerializeField] private Color inactiveColor = Color.yellow;
    [SerializeField] private Color activatedColor = Color.green;

    // 当前状态
    private CheckPointState currentState = CheckPointState.Inactive;

    private MeshRenderer meshRenderer;

    // 检查点激活音效
    public AudioClip activateClip;

    // 在MonoBehaviour创建后，Update第一次执行前调用一次
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // 初始化颜色为未激活状态（黄色）
        UpdateParticleColors();


        // 订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.AddListener(OnGameModeChange);

        // 订阅GameManager就绪事件
        GameManager.OnGameManagerReady += OnGameManagerReady;

        // 主动获取GameManager的playmode并设置一次OnGameModeChange
        if (GameManager.Instance != null)
        {
            OnGameModeChange(GameManager.Instance.IsPlayMode);
        }
    }

    void OnDestroy()
    {
        // 取消订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.RemoveListener(OnGameModeChange);

        // 取消订阅GameManager就绪事件
        GameManager.OnGameManagerReady -= OnGameManagerReady;
    }

    private void OnGameModeChange(bool isPlayMode)
    {
        // 切换成player模式的时候，隐藏自己的meshrender，editor模式再显示出来
        if (meshRenderer != null)
        {
            // meshRenderer.enabled = !isPlayMode;
            meshRenderer.enabled = false;
        }
    }

    private void OnGameManagerReady(bool initialMode)
    {
        OnGameModeChange(initialMode);
    }

    // 每帧调用一次
    void Update()
    {

    }

    // 触发器检测 - 当玩家进入触发器时
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            // 直接激活检查点
            SetState(CheckPointState.Activated);
            GlobalEvent.CheckPointActivate.Invoke(this);
        }
    }

    /// <summary>
    /// 统一设置所有粒子系统的颜色
    /// </summary>
    private void SetAllParticleColors(Color color)
    {
        // 获取自身及所有子物体中的 ParticleSystem 组件（包括未激活的，支持多级嵌套）
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }

    // 更新粒子系统颜色
    private void UpdateParticleColors()
    {
        Color targetColor = currentState == CheckPointState.Inactive ? inactiveColor : activatedColor;
        SetAllParticleColors(targetColor);
    }

    // 设置状态
    public void SetState(CheckPointState newState)
    {
        // 如果是从未激活切换到已激活状态，播放音效
        if (currentState == CheckPointState.Inactive && newState == CheckPointState.Activated)
        {
            if (activateClip != null)
            {
                AudioSource.PlayClipAtPoint(activateClip, transform.position);
            }
        }

        currentState = newState;
        UpdateParticleColors();
    }

    // 获取当前状态
    public CheckPointState GetCurrentState()
    {
        return currentState;
    }

    [Button("Set Activated")]
    public void TestSetState()
    {
        SetState(CheckPointState.Activated);
    }

    [Button("Set Inactive")]
    public void TestSetState2()
    {
        SetState(CheckPointState.Inactive);
    }
}
