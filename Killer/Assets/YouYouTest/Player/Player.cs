using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour, ICanBeHit
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
        // 重置玩家到初始位置
        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(initialRotation);
        this.GetComponent<Rigidbody>().linearVelocity = Vector3.zero; // 重置速度

        // 隐藏红色面板
        deathRed.SetActive(false);
        // 恢复生命值
        health = 1;
        Debug.Log("Player respawned at initial position!");

        //重新加载当前场景
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
