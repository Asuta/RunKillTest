using UnityEngine;
using UnityEngine.InputSystem;
using YouYouTest.CommandFramework;
using System.Collections.Generic;

public class CreateTest : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform handPosition;
    public Transform handCheckTrigger;
    
    private BeGrabobject grabbedObject = null; // 当前抓取的物体
    private GrabCommand currentGrabCommand = null; // 当前抓取命令

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
            if (grabbedObject == null)
            {
                // 没有抓取物体时，尝试抓取
                TryGrabObject();
            }
        }
        
        // 检查右手侧握键（Grip）是否松开
        if (InputActionsManager.Actions.XRIRightInteraction.Select.WasReleasedThisFrame())
        {
            if (grabbedObject != null)
            {
                // 有抓取物体时，松开它
                grabbedObject.OnReleased();
                
                // 完成抓取命令并添加到历史
                if (currentGrabCommand != null)
                {
                    // 设置最终位置和旋转
                    currentGrabCommand.SetEndTransform(grabbedObject.transform.position, grabbedObject.transform.rotation);
                    
                    // 执行命令并添加到历史
                    CommandHistory.Instance.ExecuteCommand(currentGrabCommand);
                    Debug.Log($"抓取命令已执行: {grabbedObject.gameObject.name} 从 {currentGrabCommand}");
                }
                
                grabbedObject = null;
                currentGrabCommand = null;
                Debug.Log("松开了抓取的物体");
            }
        }
        
        // 添加撤销和重做快捷键支持
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                CommandHistory.Instance.Undo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                CommandHistory.Instance.Redo();
            }
        }
        
        // 右手柄A键撤销，B键重做
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Undo();
            Debug.Log("右手柄A键按下：执行撤销操作");
        }
        
        if (InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Redo();
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
            CommandHistory.Instance.ExecuteCommand(createCommand);
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
    /// 尝试抓取物体
    /// </summary>
    private void TryGrabObject()
    {
        if (handCheckTrigger == null)
        {
            Debug.LogWarning("handCheckTrigger 未赋值，无法执行重叠检查");
            return;
        }
        
        if (handPosition == null)
        {
            Debug.LogWarning("handPosition 未赋值，无法确定手的位置");
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
        
        // 寻找第一个有CubeMoveTest组件的物体
        foreach (Collider collider in hitColliders)
        {
            BeGrabobject cubeMove = collider.GetComponent<BeGrabobject>();
            if (cubeMove != null)
            {
                // 创建抓取命令，记录抓取前的状态
                currentGrabCommand = new GrabCommand(cubeMove.transform, cubeMove.transform.position, cubeMove.transform.rotation);
                
                // 抓取这个物体
                grabbedObject = cubeMove;
                cubeMove.OnGrabbed(handPosition);
                Debug.Log($"抓取了物体: {collider.gameObject.name}");
                return; // 只抓取第一个找到的物体
            }
            
            Debug.Log($"- 检测到物体: {collider.gameObject.name}, 标签: {collider.gameObject.tag}, 层级: {LayerMask.LayerToName(collider.gameObject.layer)}");
        }
        
        // 如果没有检测到任何可抓取的物体
        Debug.Log("没有检测到可抓取的物体");
    }
}
