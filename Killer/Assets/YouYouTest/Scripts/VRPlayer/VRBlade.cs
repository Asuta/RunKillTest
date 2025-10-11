using UnityEngine;

public class VRBlade : MonoBehaviour
{
    public MeshRenderer bladeMeshRenderer;
    public float speedThreshold = 2.0f; // 速度阈值，大于此值时变红
    
    private Vector3 previousPosition;
    private bool isRed = false;
    public GameObject PlayerHit;
    public GameObject PlayerDefense;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bladeMeshRenderer = GetComponent<MeshRenderer>();
        previousPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // 计算当前速度
        Vector3 currentPosition = transform.position;
        float distance = Vector3.Distance(currentPosition, previousPosition);
        float currentSpeed = distance / Time.deltaTime;
        
        // 根据速度切换颜色
        if (currentSpeed > speedThreshold && !isRed)
        {
            SetBladeRed();
            isRed = true;
        }
        else if (currentSpeed <= speedThreshold && isRed)
        {
            SetBladeBlack();
            isRed = false;
        }
        
        // 更新上一帧位置
        previousPosition = currentPosition;
    }

    public void SetBladeRed()
    {
        bladeMeshRenderer.material.color = Color.red;
    }
    
    public void SetBladeBlack()
    {
        bladeMeshRenderer.material.color = Color.black;
    }
}
