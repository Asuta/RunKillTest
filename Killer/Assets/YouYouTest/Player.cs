using UnityEngine;

public class Player : MonoBehaviour,ICanBeHit
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

    public int health = 100;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // // 在这里添加玩家死亡的逻辑，比如播放动画、掉落物品等
        // Destroy(gameObject);
    }
}
