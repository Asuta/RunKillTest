using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class RayLineWidthControl : MonoBehaviour
{
    // 存储原始宽度的变量
    private float originalLeftWidth = 0.005f; // 默认线条宽度
    private float originalRightWidth = 0.005f; // 默认线条宽度
    
    public CurveVisualController leftCurveVisualController;
    public CurveVisualController rightCurveVisualController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 保存原始宽度
        if (leftCurveVisualController != null && leftCurveVisualController.noValidHitProperties != null)
        {
            originalLeftWidth = leftCurveVisualController.noValidHitProperties.starWidth;
        }
        
        if (rightCurveVisualController != null && rightCurveVisualController.noValidHitProperties != null)
        {
            originalRightWidth = rightCurveVisualController.noValidHitProperties.starWidth;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (leftCurveVisualController != null && GameManager.Instance != null)
        {
            UpdateCurveVisualControllerWidth(leftCurveVisualController, originalLeftWidth, GameManager.Instance.VrEditorScale);
        }

        if (rightCurveVisualController != null && GameManager.Instance != null)
        {
            UpdateCurveVisualControllerWidth(rightCurveVisualController, originalRightWidth, GameManager.Instance.VrEditorScale);
        }
    }

    private void UpdateCurveVisualControllerWidth(CurveVisualController controller, float originalWidth, float scale)
    {
        float scaledWidth = originalWidth * scale;
        
        // 更新所有状态的线条宽度
        if (controller.noValidHitProperties != null)
        {
            controller.noValidHitProperties.starWidth = scaledWidth;
        }
        
        if (controller.uiHitProperties != null)
        {
            controller.uiHitProperties.starWidth = scaledWidth;
        }
        
        if (controller.uiPressHitProperties != null)
        {
            controller.uiPressHitProperties.starWidth = scaledWidth;
        }
        
        if (controller.selectHitProperties != null)
        {
            controller.selectHitProperties.starWidth = scaledWidth;
        }
        
        if (controller.hoverHitProperties != null)
        {
            controller.hoverHitProperties.starWidth = scaledWidth;
        }
    }
    
    
    
}
