using UnityEngine;

public class HeadDebug : MonoBehaviour
{
    public float sphereRadius = 5f;
    public Color sphereColor = Color.red;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 在Scene视图中绘制Gizmos
    void OnDrawGizmos()
    {
        // 设置Gizmos颜色
        Gizmos.color = sphereColor;
        
        // 在对象位置绘制球体
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }
}
