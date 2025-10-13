using UnityEngine;
using UnityEngine.SceneManagement;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 记录游戏开始时的初始位置
        initialPosition = transform.position;
        initialRotation = transform.eulerAngles;

        // 订阅检查点重置事件
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
    }

    void OnDestroy()
    {
        // 取消订阅检查点重置事件
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }

    private void OnCheckPointReset()
    {
        // 当检查点重置事件触发时，执行复活逻辑
        Respawn();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R)) 
        {
            Respawn();
        }

    }

    private void Die()
    {
        Debug.Log("Player died!");
        // 显示红色面板
        deathRed.SetActive(true);
        // 5秒后执行复活
        Invoke(nameof(Respawn), 2.5f); 
    }

    private void Respawn()
    {
        // 获取Rigidbody组件
        Rigidbody rb = this.GetComponent<Rigidbody>();
        
        // 重置玩家到初始位置使用Rigidbody的MovePosition和MoveRotation
        rb.MovePosition(initialPosition);
        rb.MoveRotation(Quaternion.Euler(initialRotation));
        rb.linearVelocity = Vector3.zero; // 重置速度

        // 隐藏红色面板
        deathRed.SetActive(false);
        // 恢复生命值
        health = 1;
        Debug.Log("Player respawned at initial position!");

        //重新加载当前场景
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
