using UnityEngine;

public class Hook : MonoBehaviour
{
    private Transform playerCameraT; // 缓存相机引用

    public float lightDistance = 5f;
    
    //test
    public GameObject testSign;


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
        if (playerCameraT != null && testSign != null)
        {
            // 计算与相机的平方距离（避免开平方根运算）
            float sqrDistance = (transform.position - playerCameraT.position).sqrMagnitude;
            float sqrLightDistance = lightDistance * lightDistance;
            
            // 根据平方距离显示或隐藏sign物体
            if (sqrDistance < sqrLightDistance)
            {
                testSign.SetActive(true);
            }
            else
            {
                testSign.SetActive(false);
            }
        }
    }
}
