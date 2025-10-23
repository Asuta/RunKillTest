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
    }

    void OnDestroy()
    {
        // 取消订阅检查点重置事件
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);

        // 取消订阅游戏模式变化事件
        GlobalEvent.IsPlayChange.RemoveListener(OnGameModeChange);
    }

    private void OnCheckPointReset()
    {
        // 当检查点重置事件触发时，执行复活逻辑
        Respawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GlobalEvent.CheckPointReset.Invoke();
        }

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
}
