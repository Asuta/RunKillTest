using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    private XRIDefaultInputActions xrInputActions;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        xrInputActions = new XRIDefaultInputActions();
        xrInputActions.Enable();
        
        // 注册左手交互回调（按下、抬起、按住）
        xrInputActions.XRILeftInteraction.Select.started += OnLeftSelectStarted;
        xrInputActions.XRILeftInteraction.Select.performed += OnLeftSelectPerformed;
        xrInputActions.XRILeftInteraction.Select.canceled += OnLeftSelectCanceled;
        
        xrInputActions.XRILeftInteraction.Activate.started += OnLeftActivateStarted;
        xrInputActions.XRILeftInteraction.Activate.performed += OnLeftActivatePerformed;
        xrInputActions.XRILeftInteraction.Activate.canceled += OnLeftActivateCanceled;
        
        xrInputActions.XRILeftInteraction.PrimaryButton.started += OnLeftPrimaryButtonStarted;
        xrInputActions.XRILeftInteraction.PrimaryButton.performed += OnLeftPrimaryButtonPerformed;
        xrInputActions.XRILeftInteraction.PrimaryButton.canceled += OnLeftPrimaryButtonCanceled;
        
        xrInputActions.XRILeftInteraction.SecondaryButton.started += OnLeftSecondaryButtonStarted;
        xrInputActions.XRILeftInteraction.SecondaryButton.performed += OnLeftSecondaryButtonPerformed;
        xrInputActions.XRILeftInteraction.SecondaryButton.canceled += OnLeftSecondaryButtonCanceled;
        
        // 注册右手交互回调（按下、抬起、按住）
        xrInputActions.XRIRightInteraction.Select.started += OnRightSelectStarted;
        xrInputActions.XRIRightInteraction.Select.performed += OnRightSelectPerformed;
        xrInputActions.XRIRightInteraction.Select.canceled += OnRightSelectCanceled;
        
        xrInputActions.XRIRightInteraction.Activate.started += OnRightActivateStarted;
        xrInputActions.XRIRightInteraction.Activate.performed += OnRightActivatePerformed;
        xrInputActions.XRIRightInteraction.Activate.canceled += OnRightActivateCanceled;
        
        xrInputActions.XRIRightInteraction.PrimaryButton.started += OnRightPrimaryButtonStarted;
        xrInputActions.XRIRightInteraction.PrimaryButton.performed += OnRightPrimaryButtonPerformed;
        xrInputActions.XRIRightInteraction.PrimaryButton.canceled += OnRightPrimaryButtonCanceled;
        
        xrInputActions.XRIRightInteraction.SecondaryButton.started += OnRightSecondaryButtonStarted;
        xrInputActions.XRIRightInteraction.SecondaryButton.performed += OnRightSecondaryButtonPerformed;
        xrInputActions.XRIRightInteraction.SecondaryButton.canceled += OnRightSecondaryButtonCanceled;
        
        // 注册摇杆输入回调
        xrInputActions.XRILeft.Thumbstick.performed += OnLeftThumbstickPerformed;
        xrInputActions.XRIRight.Thumbstick.performed += OnRightThumbstickPerformed;
    }

    void OnDestroy()
    {
        // 取消注册回调
        xrInputActions.XRILeftInteraction.Select.started -= OnLeftSelectStarted;
        xrInputActions.XRILeftInteraction.Select.performed -= OnLeftSelectPerformed;
        xrInputActions.XRILeftInteraction.Select.canceled -= OnLeftSelectCanceled;
        
        xrInputActions.XRILeftInteraction.Activate.started -= OnLeftActivateStarted;
        xrInputActions.XRILeftInteraction.Activate.performed -= OnLeftActivatePerformed;
        xrInputActions.XRILeftInteraction.Activate.canceled -= OnLeftActivateCanceled;
        
        xrInputActions.XRILeftInteraction.PrimaryButton.started -= OnLeftPrimaryButtonStarted;
        xrInputActions.XRILeftInteraction.PrimaryButton.performed -= OnLeftPrimaryButtonPerformed;
        xrInputActions.XRILeftInteraction.PrimaryButton.canceled -= OnLeftPrimaryButtonCanceled;
        
        xrInputActions.XRILeftInteraction.SecondaryButton.started -= OnLeftSecondaryButtonStarted;
        xrInputActions.XRILeftInteraction.SecondaryButton.performed -= OnLeftSecondaryButtonPerformed;
        xrInputActions.XRILeftInteraction.SecondaryButton.canceled -= OnLeftSecondaryButtonCanceled;
        
        xrInputActions.XRIRightInteraction.Select.started -= OnRightSelectStarted;
        xrInputActions.XRIRightInteraction.Select.performed -= OnRightSelectPerformed;
        xrInputActions.XRIRightInteraction.Select.canceled -= OnRightSelectCanceled;
        
        xrInputActions.XRIRightInteraction.Activate.started -= OnRightActivateStarted;
        xrInputActions.XRIRightInteraction.Activate.performed -= OnRightActivatePerformed;
        xrInputActions.XRIRightInteraction.Activate.canceled -= OnRightActivateCanceled;
        
        xrInputActions.XRIRightInteraction.PrimaryButton.started -= OnRightPrimaryButtonStarted;
        xrInputActions.XRIRightInteraction.PrimaryButton.performed -= OnRightPrimaryButtonPerformed;
        xrInputActions.XRIRightInteraction.PrimaryButton.canceled -= OnRightPrimaryButtonCanceled;
        
        xrInputActions.XRIRightInteraction.SecondaryButton.started -= OnRightSecondaryButtonStarted;
        xrInputActions.XRIRightInteraction.SecondaryButton.performed -= OnRightSecondaryButtonPerformed;
        xrInputActions.XRIRightInteraction.SecondaryButton.canceled -= OnRightSecondaryButtonCanceled;
        
        // 取消注册摇杆回调
        xrInputActions.XRILeft.Thumbstick.performed -= OnLeftThumbstickPerformed;
        xrInputActions.XRIRight.Thumbstick.performed -= OnRightThumbstickPerformed;
        
        xrInputActions.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        // 检测按钮按下和释放的瞬间
        if (xrInputActions.XRILeftInteraction.PrimaryButton.WasPressedThisFrame())
        {
            Debug.Log("Left Primary button was pressed this frame (按下瞬间)");
        }
        
        if (xrInputActions.XRILeftInteraction.PrimaryButton.WasReleasedThisFrame())
        {
            Debug.Log("Left Primary button was released this frame (释放瞬间)");
        }

        // 实时检测左手按钮状态
        if (xrInputActions.XRILeftInteraction.Select.IsPressed())
        {
            Debug.Log("Left Select button is being pressed");
        }
        
        if (xrInputActions.XRILeftInteraction.Activate.IsPressed())
        {
            Debug.Log("Left Activate button is being pressed");
        }
        
        if (xrInputActions.XRILeftInteraction.PrimaryButton.IsPressed())
        {
            Debug.Log("Left Primary button is being pressed");
        }
        
        if (xrInputActions.XRILeftInteraction.SecondaryButton.IsPressed())
        {
            Debug.Log("Left Secondary button is being pressed");
        }

        // 实时检测右手按钮状态
        if (xrInputActions.XRIRightInteraction.Select.IsPressed())
        {
            Debug.Log("Right Select button is being pressed");
        }
        
        if (xrInputActions.XRIRightInteraction.Activate.IsPressed())
        {
            Debug.Log("Right Activate button is being pressed");
        }
        
        if (xrInputActions.XRIRightInteraction.PrimaryButton.IsPressed())
        {
            Debug.Log("Right Primary button is being pressed");
        }
        
        if (xrInputActions.XRIRightInteraction.SecondaryButton.IsPressed())
        {
            Debug.Log("Right Secondary button is being pressed");
        }

        // 实时检测摇杆输入
        var leftThumbstickValue = xrInputActions.XRILeft.Thumbstick.ReadValue<Vector2>();
        if (leftThumbstickValue.magnitude > 0.1f)
        {
            Debug.Log($"Left Thumbstick: {leftThumbstickValue}");
        }
        
        var rightThumbstickValue = xrInputActions.XRIRight.Thumbstick.ReadValue<Vector2>();
        if (rightThumbstickValue.magnitude > 0.1f)
        {
            Debug.Log($"Right Thumbstick: {rightThumbstickValue}");
        }
    }
    
    // 左手按钮回调方法（按下、抬起、按住）
    private void OnLeftSelectStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Left Select button started (按下)");
    }
    
    private void OnLeftSelectPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Select button performed (按住)");
    }
    
    private void OnLeftSelectCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Left Select button canceled (抬起)");
    }
    
    private void OnLeftActivateStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Left Activate button started (按下)");
    }
    
    private void OnLeftActivatePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Activate button performed (按住)");
    }
    
    private void OnLeftActivateCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Left Activate button canceled (抬起)");
    }
    
    private void OnLeftPrimaryButtonStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Left Primary button started (按下)");
    }
    
    private void OnLeftPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Primary button performed (按住)");
    }
    
    private void OnLeftPrimaryButtonCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Left Primary button canceled (抬起)");
    }
    
    private void OnLeftSecondaryButtonStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Left Secondary button started (按下)");
    }
    
    private void OnLeftSecondaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Secondary button performed (按住)");
    }
    
    private void OnLeftSecondaryButtonCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Left Secondary button canceled (抬起)");
    }
    
    // 右手按钮回调方法（按下、抬起、按住）
    private void OnRightSelectStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Right Select button started (按下)");
    }
    
    private void OnRightSelectPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Select button performed (按住)");
    }
    
    private void OnRightSelectCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Right Select button canceled (抬起)");
    }
    
    private void OnRightActivateStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Right Activate button started (按下)");
    }
    
    private void OnRightActivatePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Activate button performed (按住)");
    }
    
    private void OnRightActivateCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Right Activate button canceled (抬起)");
    }
    
    private void OnRightPrimaryButtonStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Right Primary button started (按下)");
    }
    
    private void OnRightPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Primary button performed (按住)");
    }
    
    private void OnRightPrimaryButtonCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Right Primary button canceled (抬起)");
    }
    
    private void OnRightSecondaryButtonStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Right Secondary button started (按下)");
    }
    
    private void OnRightSecondaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Secondary button performed (按住)");
    }
    
    private void OnRightSecondaryButtonCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Right Secondary button canceled (抬起)");
    }
    
    // 摇杆回调方法
    private void OnLeftThumbstickPerformed(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        Debug.Log($"Left Thumbstick moved: {value}");
    }
    
    private void OnRightThumbstickPerformed(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        Debug.Log($"Right Thumbstick moved: {value}");
    }
}
