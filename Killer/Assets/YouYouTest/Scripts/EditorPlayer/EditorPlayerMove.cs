using UnityEngine;

public class EditorPlayerMove : MonoBehaviour
{
    #region 字段和属性

    private const float InputThreshold = 0.1f;

    public Transform rigT; // tracking space
    public Transform headT; // head(center相机位置)
    public float moveSpeed; //移动速度
    public Transform leftHand; //左手
    public Transform rightHand; //右手

    private readonly HandState leftHandState = new HandState();
    private readonly HandState rightHandState = new HandState();

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

        CacheHandState(leftHandState, false);
        CacheHandState(rightHandState, true);

        StickRotate();

        bool scalingActive = HandleScaleGesture();

        if (!scalingActive)
        {
            HandleDragGesture(leftHandState);
            HandleDragGesture(rightHandState);
            StickMove();
        }

        leftHandState.TryUnlock();
        rightHandState.TryUnlock();
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
    /// 拖拽移动功能 - 在当前输入状态下处理单手拖拽
    /// </summary>
    private void HandleDragGesture(HandState handState)
    {
        if (handState.HandTransform == null || rigT == null)
        {
            return;
        }

        if (handState.LockedUntilRelease)
        {
            handState.ResetDrag();
            return;
        }

        if (handState.ComboStartedThisFrame)
        {
            handState.DragHandLocalPosition = handState.HandTransform.localPosition;
            handState.DragRigWorldPosition = rigT.position;
            handState.Dragging = true;
        }

        if (handState.ComboPressed && handState.Dragging)
        {
            Vector3 offset = handState.HandTransform.localPosition - handState.DragHandLocalPosition;
            offset = Quaternion.Euler(0, rigT.eulerAngles.y, 0) * offset;
            rigT.position = handState.DragRigWorldPosition - offset * moveSpeed * rigT.localScale.x;
        }

        if (!handState.ComboPressed)
        {
            handState.ResetDrag();
        }
    }

    /// <summary>
    /// 双手缩放功能 - 同时按住双手的扳机键和握键进行缩放
    /// </summary>
    private bool HandleScaleGesture()
    {
        if (leftHandState.HandTransform == null || rightHandState.HandTransform == null || rigT == null)
        {
            return false;
        }

        bool leftReady = leftHandState.ComboPressed;
        bool rightReady = rightHandState.ComboPressed;

        if (leftReady && rightReady)
        {
            if (!isScaling)
            {
                BeginScaleGesture();
            }

            UpdateScaleGesture();
            return true;
        }

        if (isScaling)
        {
            EndScaleGesture();
        }

        return false;
    }

    private void BeginScaleGesture()
    {
        recordScale = rigT.localScale.x;
        recordDistance = Vector3.Distance(leftHandState.HandTransform.localPosition, rightHandState.HandTransform.localPosition);
        centerPosition = (leftHandState.HandTransform.position + rightHandState.HandTransform.position) * 0.5f;
        isScaling = true;

        leftHandState.LockUntilRelease();
        rightHandState.LockUntilRelease();
        leftHandState.ResetDrag();
        rightHandState.ResetDrag();
    }

    private void UpdateScaleGesture()
    {
        float currentDistance = Vector3.Distance(leftHandState.HandTransform.localPosition, rightHandState.HandTransform.localPosition);
        if (currentDistance <= Mathf.Epsilon)
        {
            return;
        }

        float targetScale = recordDistance / currentDistance * recordScale;
        float currentScale = rigT.localScale.x;

        if (Mathf.Approximately(currentScale, targetScale))
        {
            return;
        }

        centerPosition = (leftHandState.HandTransform.position + rightHandState.HandTransform.position) * 0.5f;
        Vector3 zoomVector = (centerPosition - rigT.position) * (targetScale / currentScale - 1.0f);
        rigT.localScale = new Vector3(targetScale, targetScale, targetScale);
        rigT.position -= zoomVector;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerScaleChanged(recordScale, targetScale);
        }
    }

    private void EndScaleGesture()
    {
        isScaling = false;
        leftHandState.LockUntilRelease();
        rightHandState.LockUntilRelease();
        leftHandState.ResetDrag();
        rightHandState.ResetDrag();
    }

    private void CacheHandState(HandState handState, bool isRightHand)
    {
        handState.HandTransform = isRightHand ? rightHand : leftHand;

        if (handState.HandTransform == null)
        {
            handState.ResetDrag();
            handState.TriggerValue = 0f;
            handState.GripValue = 0f;
            handState.TriggerPressed = false;
            handState.GripPressed = false;
            handState.TriggerDown = false;
            handState.GripDown = false;
            handState.LockedUntilRelease = false;
            return;
        }

        if (isRightHand)
        {
            handState.TriggerValue = InputActionsManager.Actions.XRIRightInteraction.ActivateValue.ReadValue<float>();
            handState.GripValue = InputActionsManager.Actions.XRIRightInteraction.SelectValue.ReadValue<float>();
            handState.TriggerDown = InputActionsManager.Actions.XRIRightInteraction.Activate.WasPressedThisFrame();
            handState.GripDown = InputActionsManager.Actions.XRIRightInteraction.Select.WasPressedThisFrame();
        }
        else
        {
            handState.TriggerValue = InputActionsManager.Actions.XRILeftInteraction.ActivateValue.ReadValue<float>();
            handState.GripValue = InputActionsManager.Actions.XRILeftInteraction.SelectValue.ReadValue<float>();
            handState.TriggerDown = InputActionsManager.Actions.XRILeftInteraction.Activate.WasPressedThisFrame();
            handState.GripDown = InputActionsManager.Actions.XRILeftInteraction.Select.WasPressedThisFrame();
        }

        handState.TriggerPressed = handState.TriggerValue > InputThreshold;
        handState.GripPressed = handState.GripValue > InputThreshold;
    }
    
    #endregion
    #region 内部类型

    private class HandState
    {
        public Transform HandTransform;
        public float TriggerValue;
        public float GripValue;
        public bool TriggerPressed;
        public bool GripPressed;
        public bool TriggerDown;
        public bool GripDown;
        public bool Dragging;
        public bool LockedUntilRelease;
        public Vector3 DragHandLocalPosition;
        public Vector3 DragRigWorldPosition;

        public bool ComboPressed => TriggerPressed && GripPressed;
        public bool ComboStartedThisFrame => (GripDown && TriggerPressed) || (TriggerDown && GripPressed);

        public void ResetDrag()
        {
            Dragging = false;
        }

        public void LockUntilRelease()
        {
            LockedUntilRelease = true;
        }

        public void TryUnlock()
        {
            if (!LockedUntilRelease)
            {
                return;
            }

            if (!TriggerPressed && !GripPressed)
            {
                LockedUntilRelease = false;
            }
        }
    }

    #endregion
}
