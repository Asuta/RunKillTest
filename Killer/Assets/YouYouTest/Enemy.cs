using UnityEngine;



public class Enemy : MonoBehaviour, ICanBeHit
{
    public int health = 100;
    public Transform target;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Enemy took {damage} damage, remaining health: {health}");
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died!");
        // 在这里添加敌人死亡的逻辑，比如播放动画、掉落物品等
        Destroy(gameObject);
    }

    public void SetTarget( Transform PlayerHead)
    {
        target = PlayerHead;   
    }   
}