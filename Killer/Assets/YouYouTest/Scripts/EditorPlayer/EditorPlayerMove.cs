using UnityEngine;

public class EditorPlayerMove : MonoBehaviour
{
    #region 字段和属性
    
    public Transform rigT; // tracking space
    public Transform headT; // head(center相机位置)
    private Vector3 rigRecordPos; //rigT记录位置(用于拖拽移动，按下时记录位置)
    public float moveSpeed; //移动速度
    public Transform leftHand; //左手
    private Vector3 leftHandRecordPos; //左手记录位置
    public Transform rightHand; //右手
    private Vector3 rightHandRecordPos; //右手记录位置
    
    // 拖拽移动状态
    private bool leftHandDragging = false;
    private bool rightHandDragging = false;
    
    // 旋转状态控制
    private bool wasRotatingLeft = false;
    private bool wasRotatingRight = false;
    
    // 缩放相关变量
    private float recordScale; // 记录的初始比例
    private float recordDistance; // 记录的初始距离
    private bool isScaling = false; // 是否正在缩放
    private Vector3 centerPosition; // 缩放中心点
    
    #endregion
    
    #region Unity生命周期
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 启用输入动作
        InputActionsManager.EnableAll();
    }

    // Update is called once per frame
    void Update()
    {
        // moveSpeed保持为1，因为我们在移动计算中已经考虑了rigT的缩放
        moveSpeed = 1f;
        StickRotate();
        //拖拽移动
        DragMove();
        //左摇杆移动
        StickMove();
        //双手缩放
        HandleScale();
    }
    
    #endregion
    
    #region 移动功能
    
    /// <summary>
    /// 摇杆旋转功能 - 使用右手摇杆左右方向控制旋转
    /// </summary>
    private void StickRotate()
    {
        //右手摇杆进行转向，每次旋转45度(以头部（headT）为旋转中心)
        Vector2 rightThumbstick = InputActionsManager.Actions.XRIRightLocomotion.Move.ReadValue<Vector2>();
        
        // 右旋转 - 摇杆推到最右边时才旋转一次
        if (rightThumbstick.x > 0.8f && !wasRotatingRight)
        {
            rigT.RotateAround(headT.position, Vector3.up, 45);
            wasRotatingRight = true;
        }
        else if (rightThumbstick.x <= 0.8f)
        {
            wasRotatingRight = false;
        }
        
        // 左旋转 - 摇杆推到最左边时才旋转一次
        if (rightThumbstick.x < -0.8f && !wasRotatingLeft)
        {
            rigT.RotateAround(headT.position, Vector3.up, -45);
            wasRotatingLeft = true;
        }
        else if (rightThumbstick.x >= -0.8f)
        {
            wasRotatingLeft = false;
        }
    }
    
    /// <summary>
    /// 摇杆移动功能 - 使用左手摇杆控制移动
    /// </summary>
    private void StickMove()
    {
        //左摇杆移动
        Vector2 move = InputActionsManager.Actions.XRILeftLocomotion.Move.ReadValue<Vector2>();
        if (move.magnitude > 0.1f)
        {
            // 创建一个只有Y轴旋转的四元数
            Quaternion rotationY = Quaternion.Euler(0, leftHand.rotation.eulerAngles.y, 0);
            // 使用新的四元数旋转移动向量
            Vector3 dir = rotationY * new Vector3(move.x, 0, move.y);
            dir = new Vector3(dir.x, 0, dir.z);
            // 考虑rigT的缩放，移动距离乘以rigT.localScale.x
            rigT.position += dir * moveSpeed * Time.deltaTime * rigT.localScale.x;
        }
    }
    
    /// <summary>
    /// 拖拽移动功能 - 同时按住扳机和握键时进行拖拽移动
    /// </summary>
    private void DragMove()
    {
        // 右手拖拽移动
        HandleDragMove(true);
        
        // 左手拖拽移动
        HandleDragMove(false);
    }
    
    /// <summary>
    /// 处理单手拖拽移动
    /// </summary>
    /// <param name="isRightHand">是否为右手</param>
    private void HandleDragMove(bool isRightHand)
    {
        // 获取对应手的扳机和握键值和状态
        float triggerValue = isRightHand ?
            InputActionsManager.Actions.XRIRightInteraction.ActivateValue.ReadValue<float>() :
            InputActionsManager.Actions.XRILeftInteraction.ActivateValue.ReadValue<float>();
            
        float gripValue = isRightHand ?
            InputActionsManager.Actions.XRIRightInteraction.SelectValue.ReadValue<float>() :
            InputActionsManager.Actions.XRILeftInteraction.SelectValue.ReadValue<float>();
        
        bool triggerPressed = triggerValue > 0.1f;
        bool gripPressed = gripValue > 0.1f;
        
        // 获取按键按下事件
        bool triggerDown = isRightHand ?
            InputActionsManager.Actions.XRIRightInteraction.Activate.WasPressedThisFrame() :
            InputActionsManager.Actions.XRILeftInteraction.Activate.WasPressedThisFrame();
            
        bool gripDown = isRightHand ?
            InputActionsManager.Actions.XRIRightInteraction.Select.WasPressedThisFrame() :
            InputActionsManager.Actions.XRILeftInteraction.Select.WasPressedThisFrame();
        
        Transform handTransform = isRightHand ? rightHand : leftHand;
        
        if (isRightHand)
        {
            // 右手拖拽处理 - 记录位置
            // 握键按下且扳机已按下
            if (gripDown && triggerPressed)
            {
                rightHandRecordPos = handTransform.localPosition;
                rigRecordPos = rigT.position;
                rightHandDragging = true;
            }
            // 扳机按下且握键已按下
            else if (triggerDown && gripPressed)
            {
                rightHandRecordPos = handTransform.localPosition;
                rigRecordPos = rigT.position;
                rightHandDragging = true;
            }
            
            // 拖拽移动
            if (triggerPressed && gripPressed && rightHandDragging)
            {
                Vector3 offset = handTransform.localPosition - rightHandRecordPos;
                offset = Quaternion.Euler(0, rigT.eulerAngles.y, 0) * offset;
                // 考虑rigT的缩放，移动距离乘以rigT.localScale.x
                rigT.position = rigRecordPos - offset * moveSpeed * rigT.localScale.x;
            }
            
            // 释放拖拽状态
            if (!triggerPressed || !gripPressed)
            {
                rightHandDragging = false;
            }
        }
        else
        {
            // 左手拖拽处理 - 记录位置
            // 握键按下且扳机已按下
            if (gripDown && triggerPressed)
            {
                leftHandRecordPos = handTransform.localPosition;
                rigRecordPos = rigT.position;
                leftHandDragging = true;
            }
            // 扳机按下且握键已按下
            else if (triggerDown && gripPressed)
            {
                leftHandRecordPos = handTransform.localPosition;
                rigRecordPos = rigT.position;
                leftHandDragging = true;
            }
            
            // 拖拽移动
            if (triggerPressed && gripPressed && leftHandDragging)
            {
                Vector3 offset = handTransform.localPosition - leftHandRecordPos;
                offset = Quaternion.Euler(0, rigT.eulerAngles.y, 0) * offset;
                // 考虑rigT的缩放，移动距离乘以rigT.localScale.x
                rigT.position = rigRecordPos - offset * moveSpeed * rigT.localScale.x;
            }
            
            // 释放拖拽状态
            if (!triggerPressed || !gripPressed)
            {
                leftHandDragging = false;
            }
        }
    }
    
    /// <summary>
    /// 双手缩放功能 - 同时按住双手A键进行缩放
    /// </summary>
    private void HandleScale()
    {
        // 检测双手A键状态
        bool leftAPressed = InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.IsPressed();
        bool rightAPressed = InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.IsPressed();
        bool leftADown = InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.WasPressedThisFrame();
        bool rightADown = InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasPressedThisFrame();
        
        // 计算缩放中心点（两手之间的中点）
        centerPosition = (leftHand.position + rightHand.position) / 2;
        
        // 检测是否开始缩放（任意一个A键按下且另一个已经按下）
        if ((leftADown && rightAPressed) || (rightADown && leftAPressed))
        {
            // 记录当前的缩放倍数
            recordScale = rigT.localScale.x;
            // 记录当前两个手柄的距离
            recordDistance = Vector3.Distance(leftHand.localPosition, rightHand.localPosition);
            isScaling = true;
        }
        
        // 如果双手都按住A键，进行缩放
        if (leftAPressed && rightAPressed && isScaling)
        {
            // 获取现在两个手的距离
            var nowDistance = Vector3.Distance(leftHand.localPosition, rightHand.localPosition);
            // 计算现在的缩放比例
            var targetScale = recordDistance / nowDistance * recordScale;
            // 计算缩放后的大小
            var newSize = targetScale;

            // 计算要缩放的距离
            Vector3 zoomVector = (centerPosition - rigT.position) * (newSize / rigT.localScale.x - 1.0f);
            // 缩放物体并移动位置，以使中心点保持不变
            rigT.localScale = new Vector3(newSize, newSize, newSize);
            rigT.position -= zoomVector;
            
            // 触发缩放变化事件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerScaleChanged(recordScale, newSize);
            }
        }
        
        // 如果任意一个A键释放，停止缩放
        if (!leftAPressed || !rightAPressed)
        {
            isScaling = false;
        }
    }
    
    #endregion
}
