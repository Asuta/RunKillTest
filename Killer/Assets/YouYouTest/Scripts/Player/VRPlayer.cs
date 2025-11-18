using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;

public class VRPlayer : MonoBehaviour, ICanBeHit
{
    public void TakeDamage(int damage)
    {
        Debug.Log($"Player took {damage} damage");
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }


    public int health = 1;
    public GameObject deathRed;
    private Vector3 initialPosition; // 存储初始位置
    private Vector3 initialRotation; // 存储初始旋转
    private bool isDead = false; // 标记玩家是否处于死亡状态
    public GameObject grabBody;
    
    // 菜单键和左手扳机键组合检测
    private bool menuButtonPressed = false;
    private bool leftTriggerPressed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 记录游戏开始时的初始位置
        initialPosition = transform.position;
        initialRotation = transform.eulerAngles;

        // 订阅检查点重置事件
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);

        // 订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.AddListener(OnGameModeChange);

        // 订阅GameManager就绪事件
        GameManager.OnGameManagerReady += OnGameManagerReady;
        
        // 主动获取GameManager的playmode并设置一次OnGameModeChange
        if (GameManager.Instance != null)
        {
            OnGameModeChange(GameManager.Instance.IsPlayMode);
            Debug.Log($"VRPlayer主动获取GameManager模式，当前模式: {(GameManager.Instance.IsPlayMode ? "游戏模式" : "编辑模式")}");
        }
    }

    void OnDestroy()
    {
        // 取消订阅检查点重置事件
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);

        // 取消订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.RemoveListener(OnGameModeChange);

        // 取消订阅GameManager就绪事件
        GameManager.OnGameManagerReady -= OnGameManagerReady;
    }

    private void OnCheckPointReset()
    {
        // 当检查点重置事件触发时，执行复活逻辑
        Respawn();
    }

    // Update is called once per frame
    void Update()
    {
        // R键保持原有行为（按下即触发）
        if (Input.GetKeyDown(KeyCode.R))
        {
            GlobalEvent.CheckPointReset.Invoke();
        }
        
        // 检测菜单键和左手扳机键的组合按下
        bool menuCurrentlyPressed = InputActionsManager.Actions.XRILeftInteraction.Menu.IsPressed();
        float leftTriggerValue = InputActionsManager.Actions.XRILeftInteraction.ActivateValue.ReadValue<float>();
        bool leftTriggerCurrentlyPressed = leftTriggerValue > 0.1f;
        
        // 检测菜单键短按（用于开启UI）
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasPressedThisFrame())
        {
            // 这里可以添加开启UI的逻辑
            Debug.Log("菜单键短按 - 开启UI");
        }
        
        // 检测菜单键和左手扳机键同时按下（用于reset功能）
        if (menuCurrentlyPressed && leftTriggerCurrentlyPressed)
        {
            if (!menuButtonPressed || !leftTriggerPressed)
            {
                // 两个键刚刚同时按下
                GlobalEvent.CheckPointReset.Invoke();
                Debug.Log("菜单键+左手扳机键同时按下 - 触发Reset");
            }
        }
        
        // 更新按键状态
        menuButtonPressed = menuCurrentlyPressed;
        leftTriggerPressed = leftTriggerCurrentlyPressed;
    }

    private void Die()
    {
        if (isDead) return; // 如果已经死亡，则不再执行死亡逻辑

        Debug.Log("Player died!");
        isDead = true; // 设置死亡标志
        // 显示红色面板
        deathRed.SetActive(true);
        // 2.5秒后触发检查点重置事件
        Invoke(nameof(TriggerCheckPointReset), 2.5f);
    }

    private void TriggerCheckPointReset()
    {
        GlobalEvent.CheckPointReset.Invoke();
    }

    private void Respawn()
    {
        // 获取Rigidbody组件
        Rigidbody rb = this.GetComponent<Rigidbody>();

        Vector3 respawnPosition = initialPosition;
        Vector3 respawnRotation = initialRotation;

        // 如果有激活的检查点，使用检查点的位置和旋转
        if (GameManager.Instance != null && GameManager.Instance.nowActivateCheckPoint != null)
        {
            respawnPosition = GameManager.Instance.nowActivateCheckPoint.transform.position;
            respawnRotation = GameManager.Instance.nowActivateCheckPoint.transform.eulerAngles;
            Debug.Log("Player respawned at checkpoint position!");
        }
        else
        {
            Debug.Log("Player respawned at initial position!");
        }

        // 重置玩家到检查点位置或初始位置
        rb.MovePosition(respawnPosition);
        rb.MoveRotation(Quaternion.Euler(respawnRotation));
        rb.linearVelocity = Vector3.zero; // 重置速度

        // 隐藏红色面板
        deathRed.SetActive(false);
        // 恢复生命值
        health = 1;
        // 重置死亡标志
        isDead = false;

        //重新加载当前场景
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //test
    [Button]
    private void TestDie()
    {
        GlobalEvent.CheckPointReset.Invoke();
    }

    public void OnDeath()
    {
        if (isDead) return; // 如果已经死亡，则不再触发死亡事件
        Die();
    }

    private void OnGameModeChange(bool isPlayMode)
    {
        // 当游戏模式变化时，根据状态设置grabBody的激活状态
        if (grabBody != null)
        {
            grabBody.SetActive(!isPlayMode);
        }

        this.GetComponent<Rigidbody>().isKinematic = !isPlayMode;
    }

    private void OnGameManagerReady(bool initialMode)
    {
        // 当GameManager就绪时，应用初始游戏模式
        OnGameModeChange(initialMode);
        Debug.Log($"VRPlayer通过GameManager就绪事件初始化，当前模式: {(initialMode ? "游戏模式" : "编辑模式")}");
    }
}
