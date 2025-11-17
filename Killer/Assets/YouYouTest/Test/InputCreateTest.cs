using UnityEngine;
using VInspector;

public class InputCreateTest : MonoBehaviour
{
    public Transform target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    [Button("Create InputField")]
    public void CreateInputField()
    {
        if (target != null)
        {
            // 计算forward方向向下偏移45度的方向
            Vector3 direction = Quaternion.Euler(-80, 0, 0) * -transform.forward;
            // 设置target位置为当前位置加上方向向量乘以距离3米
            target.position = transform.position + direction * 3f;
        }
    }
}
