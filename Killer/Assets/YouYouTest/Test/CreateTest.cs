using UnityEngine;
using UnityEngine.InputSystem;
using YouYouTest.CommandFramework;
using System.Collections.Generic;

public class CreateTest : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform handPosition;
    public Transform handCheckTrigger; 
    
    private CommandHistory _commandHistory = new CommandHistory();

    void Start()
    {

    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update()
    {
        // 检查左手选择按钮是否按下
        if (InputActionsManager.Actions.XRIRightInteraction.Activate.WasPressedThisFrame())
        {
            CreateCubeAtHandPosition();
        }
        
        // 检查右手侧握键（Grip）是否按下
        if (InputActionsManager.Actions.XRIRightInteraction.Select.WasPressedThisFrame())
        {
            CheckHandTriggerOverlap();
        }
        
        // 添加撤销和重做快捷键支持
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                _commandHistory.Undo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                _commandHistory.Redo();
            }
        }
        
        // 右手柄A键撤销，B键重做
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasPressedThisFrame())
        {
            _commandHistory.Undo();
            Debug.Log("右手柄A键按下：执行撤销操作");
        }
        
        if (InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.WasPressedThisFrame())
        {
            _commandHistory.Redo();
            Debug.Log("右手柄B键按下：执行重做操作");
        }
    }

    /// <summary>
    /// 在手的位置创建立方体（使用命令模式）
    /// </summary>
    private void CreateCubeAtHandPosition()
    {
        if (cubePrefab != null && handPosition != null)
        {
            // 使用命令模式创建立方体
            ICommand createCommand = new CreateObjectCommand(cubePrefab, handPosition.position);
            _commandHistory.ExecuteCommand(createCommand);
            Debug.Log($"使用命令模式在手的位置创建立方体: {handPosition.position}");
        }
        else
        {
            if (cubePrefab == null)
                Debug.LogWarning("cubePrefab 未赋值，无法创建立方体");
            if (handPosition == null)
                Debug.LogWarning("handPosition 未赋值，无法确定手的位置");
        }
    }
    
    /// <summary>
    /// 检查手部触发器位置的box overlap
    /// </summary>
    private void CheckHandTriggerOverlap()
    {
        if (handCheckTrigger == null)
        {
            Debug.LogWarning("handCheckTrigger 未赋值，无法执行重叠检查");
            return;
        }
        
        // 获取handCheckTrigger的缩放作为box的大小
        Vector3 boxSize = handCheckTrigger.localScale;
        
        // 获取handCheckTrigger的位置和旋转
        Vector3 center = handCheckTrigger.position;
        Quaternion rotation = handCheckTrigger.rotation;
        
        // 执行box overlap检查
        Collider[] hitColliders = Physics.OverlapBox(center, boxSize / 2, rotation);
        
        // 记录检查到的物体信息
        Debug.Log($"在位置 {center} 执行box overlap检查，范围大小: {boxSize}");
        Debug.Log($"检查到 {hitColliders.Length} 个物体:");
        
        foreach (Collider collider in hitColliders)
        {
            Debug.Log($"- 检测到物体: {collider.gameObject.name}, 标签: {collider.gameObject.tag}, 层级: {LayerMask.LayerToName(collider.gameObject.layer)}");
        }
        
        // 如果没有检测到任何物体
        if (hitColliders.Length == 0)
        {
            Debug.Log("没有检测到任何物体");
        }
    }
}
