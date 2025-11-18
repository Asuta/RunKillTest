using UnityEngine;

public class BladeMesh : MonoBehaviour
{
    public Transform targetT;
    public float lerpSpeed = 10f; // 插值速度，可以调整这个值来控制插值的快慢
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (targetT != null)
        {
            // 使用插值平滑地移动到目标位置
            transform.position = Vector3.Lerp(transform.position, targetT.position, lerpSpeed * Time.deltaTime);
            // 使用插值平滑地旋转到目标旋转
            transform.rotation = Quaternion.Lerp(transform.rotation, targetT.rotation, lerpSpeed * Time.deltaTime);
        }
    }
}
