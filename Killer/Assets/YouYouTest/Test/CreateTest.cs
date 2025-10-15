using UnityEngine;
using UnityEngine.InputSystem;
using YouYouTest.CommandFramework;

public class CreateTest : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform handPosition;
    
    private CommandHistory _commandHistory = new CommandHistory();

    void Start()
    {
        // 确保 InputActionsManager 已启用
        InputActionsManager.EnableAll();
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
}
