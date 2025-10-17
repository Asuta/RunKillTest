using UnityEngine;

public class EditorPlayerMove : MonoBehaviour
{
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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 启用输入动作
        InputActionsManager.EnableAll();
    }

    // Update is called once per frame
    void Update()
    {
        moveSpeed = rigT.localScale.x;
        StickRotate();
        //拖拽移动
        DragMove();
        //左摇杆移动
        StickMove();
    }
    
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
            rigT.position += dir * moveSpeed * Time.deltaTime;
        }
    }
    
    private void DragMove()
    {
        // 右手拖拽移动
        HandleDragMove(true);
        
        // 左手拖拽移动
        HandleDragMove(false);
    }
    
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
                rigT.position = rigRecordPos - offset * moveSpeed;
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
                rigT.position = rigRecordPos - offset * moveSpeed;
            }
            
            // 释放拖拽状态
            if (!triggerPressed || !gripPressed)
            {
                leftHandDragging = false;
            }
        }
    }
}
