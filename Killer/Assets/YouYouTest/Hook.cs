using UnityEngine;

public class Hook : MonoBehaviour
{
    private Transform playerCameraT; // 缓存相机引用

    public float lightDistance = 5f;

    //test
    public MeshRenderer shortSign;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 在Start中缓存引用
        playerCameraT = GameManager.Instance.PlayerCameraT;

        // 可选：检查引用是否有效
        if (playerCameraT == null)
        {
            Debug.LogError("Player camera reference is null in Hook.Start()");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 使用缓存的引用
        if (playerCameraT != null && shortSign != null)
        {
            // 计算与相机的平方距离（避免开平方根运算）
            float sqrDistance = (transform.position - playerCameraT.position).sqrMagnitude;
            float sqrLightDistance = lightDistance * lightDistance;

            // 根据平方距离改变MeshRenderer的颜色
            if (sqrDistance < sqrLightDistance)
            {
                // 距离范围内：设置为绿色
                shortSign.material.color = Color.green;
            }
            else
            {
                // 距离范围外：设置为红色
                shortSign.material.color = Color.red;
            }
        }


    }

    // 在Scene视图中绘制Gizmos
    void OnDrawGizmos()
    {
        // 设置Gizmos颜色为半透明绿色
        Gizmos.color = new Color(0, 1, 0, 0.3f);

        // 在对象位置绘制球体，使用lightDistance作为半径
        Gizmos.DrawWireSphere(transform.position, lightDistance);
    }
}
