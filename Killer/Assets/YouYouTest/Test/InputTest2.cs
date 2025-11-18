using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest2 : MonoBehaviour
{
    void Start()
    {
        // 启用所有 actions
        InputActionsManager.EnableAll();
    }

    // Update is called once per frame
    void Update()
    {
        // 直接使用强类型属性访问 actions
        Vector2 moveInput = InputActionsManager.Actions.XRILeftLocomotion.Move.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            Debug.Log("Moving: " + moveInput);
        }

        // 检查右手交互的 PrimaryButton 是否被释放
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasReleasedThisFrame())
        {
            Debug.Log("Primary Button Released!");
        }
        
        // 检查左手交互的 Select 是否被按下
        if (InputActionsManager.Actions.XRILeftInteraction.Select.WasPressedThisFrame())
        {
            Debug.Log("Select Pressed!");
        }
    }
    
    void OnDestroy()
    {
        // 禁用所有 actions
        InputActionsManager.DisableAll();
    }
}
