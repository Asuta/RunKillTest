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
        
        // 注册左手交互回调
        xrInputActions.XRILeftInteraction.Select.performed += OnLeftSelectPerformed;
        xrInputActions.XRILeftInteraction.Activate.performed += OnLeftActivatePerformed;
        xrInputActions.XRILeftInteraction.PrimaryButton.performed += OnLeftPrimaryButtonPerformed;
        xrInputActions.XRILeftInteraction.SecondaryButton.performed += OnLeftSecondaryButtonPerformed;
        
        // 注册右手交互回调
        xrInputActions.XRIRightInteraction.Select.performed += OnRightSelectPerformed;
        xrInputActions.XRIRightInteraction.Activate.performed += OnRightActivatePerformed;
        xrInputActions.XRIRightInteraction.PrimaryButton.performed += OnRightPrimaryButtonPerformed;
        xrInputActions.XRIRightInteraction.SecondaryButton.performed += OnRightSecondaryButtonPerformed;
        
        // 注册摇杆输入回调
        xrInputActions.XRILeft.Thumbstick.performed += OnLeftThumbstickPerformed;
        xrInputActions.XRIRight.Thumbstick.performed += OnRightThumbstickPerformed;
    }

    void OnDestroy()
    {
        // 取消注册回调
        xrInputActions.XRILeftInteraction.Select.performed -= OnLeftSelectPerformed;
        xrInputActions.XRILeftInteraction.Activate.performed -= OnLeftActivatePerformed;
        xrInputActions.XRILeftInteraction.PrimaryButton.performed -= OnLeftPrimaryButtonPerformed;
        xrInputActions.XRILeftInteraction.SecondaryButton.performed -= OnLeftSecondaryButtonPerformed;
        
        xrInputActions.XRIRightInteraction.Select.performed -= OnRightSelectPerformed;
        xrInputActions.XRIRightInteraction.Activate.performed -= OnRightActivatePerformed;
        xrInputActions.XRIRightInteraction.PrimaryButton.performed -= OnRightPrimaryButtonPerformed;
        xrInputActions.XRIRightInteraction.SecondaryButton.performed -= OnRightSecondaryButtonPerformed;
        
        // 取消注册摇杆回调
        xrInputActions.XRILeft.Thumbstick.performed -= OnLeftThumbstickPerformed;
        xrInputActions.XRIRight.Thumbstick.performed -= OnRightThumbstickPerformed;
        
        xrInputActions.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
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
    
    // 左手回调方法
    private void OnLeftSelectPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Select button pressed");
    }
    
    private void OnLeftActivatePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Activate button pressed");
    }
    
    private void OnLeftPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Primary button pressed");
    }
    
    private void OnLeftSecondaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Left Secondary button pressed");
    }
    
    // 右手回调方法
    private void OnRightSelectPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Select button pressed");
    }
    
    private void OnRightActivatePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Activate button pressed");
    }
    
    private void OnRightPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Primary button pressed");
    }
    
    private void OnRightSecondaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Right Secondary button pressed");
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
