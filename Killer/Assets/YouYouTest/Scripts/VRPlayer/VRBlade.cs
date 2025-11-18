using UnityEngine;

public class VRBlade : MonoBehaviour
{
    #region 公共变量
    public MeshRenderer bladeMeshRenderer;
    public float speedThreshold = 2.0f; // 速度阈值，大于此值时变红
    public GameObject playerHit;
    public GameObject playerDefense;
    #endregion

    #region 私有变量
    private Vector3 previousPosition;
    private bool isRed = false;
    #endregion

    #region Unity生命周期方法
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bladeMeshRenderer = GetComponent<MeshRenderer>();
        previousPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBladeColorBasedOnSpeed();
    }

    // 当刀片碰撞到其他物体时触发
    private void OnTriggerEnter(Collider other)
    {
        // 记录碰撞物体的名称
        Debug.Log($"刀片碰到了: {other.gameObject.name}");
        
        // 检查碰撞物体是否为敌人并且有ICanBeHit接口
        if (other.CompareTag("Enemy"))
        {
            ICanBeHit canBeHit = other.attachedRigidbody.GetComponent<ICanBeHit>();
            if (canBeHit != null)
            {
                // 调用伤害方法，这里假设造成50点伤害
                canBeHit.TakeDamage(50);
                Debug.Log($"对敌人造成50点伤害");
            }
        }
    }
    #endregion

    #region 刀片颜色管理
    
    private void UpdateBladeColorBasedOnSpeed()
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
        playerHit.SetActive(true);
        playerDefense.SetActive(false);
    }
    
    public void SetBladeBlack()
    {
        bladeMeshRenderer.material.color = Color.black;
        playerHit.SetActive(false);
        playerDefense.SetActive(true);
    }
    #endregion
}
