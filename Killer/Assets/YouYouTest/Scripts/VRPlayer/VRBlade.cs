using UnityEngine;
using Drakkar;

public class VRBlade : MonoBehaviour
{
    #region 公共变量
    public MeshRenderer bladeMeshRenderer;
    public float speedThreshold = 2.0f; // 速度阈值，大于此值时变红
    public float minTrailDuration = 1.0f; // 拖尾最小显示时间
    public GameObject playerHit;
    public GameObject playerDefense;
    public GameObject trailObject;
    #endregion

    #region 私有变量
    private Vector3 previousPosition;
    private bool isRed = false;
    private float trailTimer = 0f;
    #endregion

    #region Unity生命周期方法
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bladeMeshRenderer = GetComponent<MeshRenderer>();
        // 使用父物体的本地坐标初始化，如果没有父物体则使用自身坐标
        previousPosition = transform.parent != null ? transform.parent.localPosition : transform.localPosition;
        if (trailObject != null) trailObject.SetActive(false);
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
        // Debug.Log($"刀片碰到了: {other.gameObject.name}");
        
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
        // 计算当前速度 (使用父物体的本地坐标)
        Vector3 currentPosition = transform.parent != null ? transform.parent.localPosition : transform.localPosition;
        float distance = Vector3.Distance(currentPosition, previousPosition);
        float currentSpeed = distance / Time.deltaTime;
        
        // 更新上一帧位置
        previousPosition = currentPosition;

        Debug.Log($"当前速度: {currentSpeed:F4}");

        // 如果速度超过阈值，刷新计时器并设为红色状态
        if (currentSpeed > speedThreshold)
        {
            trailTimer = minTrailDuration;
            if (!isRed)
            {
                SetBladeRed();
                isRed = true;
            }
        }
        else
        {
            // 速度不够时，减少计时器
            if (trailTimer > 0)
            {
                trailTimer -= Time.deltaTime;
            }

            // 只有当计时器归零且当前是红色状态时，才恢复黑色
            if (trailTimer <= 0 && isRed)
            {
                SetBladeBlack();
                isRed = false;
            }
        }
    }

    public void SetBladeRed()
    {
        bladeMeshRenderer.material.color = Color.red;
        playerHit.SetActive(true);
        playerDefense.SetActive(false);
        if (trailObject != null) trailObject.SetActive(true);
    }
    
    public void SetBladeBlack()
    {
        bladeMeshRenderer.material.color = Color.black;
        playerHit.SetActive(false);
        playerDefense.SetActive(true);
        if (trailObject != null) trailObject.SetActive(false);
    }
    #endregion
}
