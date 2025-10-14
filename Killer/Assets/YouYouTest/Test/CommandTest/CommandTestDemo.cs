using UnityEngine;

namespace YouYouTest.CommandTest
{
    /// <summary>
    /// 命令模式测试演示脚本
    /// </summary>
    public class CommandTestDemo : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private GameObject _testCubePrefab;
        [SerializeField] private GameObject _testSpherePrefab;
        
        private CommandHistory _commandHistory = new CommandHistory();
        private GameObject _testObject;
        
        void Start()
        {
            Debug.Log("=== 命令模式测试演示开始 ===");
            
            // 创建一个测试物体
            CreateTestObject();
            
            // 延迟执行一系列测试
            Invoke(nameof(TestMoveCommand), 1f);
            Invoke(nameof(TestCreateCommand), 2f);
            Invoke(nameof(TestDeleteCommand), 3f);
            Invoke(nameof(TestUndoRedo), 4f);
        }
        
        void CreateTestObject()
        {
            if (_testCubePrefab != null)
            {
                Vector3 position = new Vector3(0, 1, 0);
                CreateObjectCommand createCommand = new CreateObjectCommand(_testCubePrefab, position);
                _commandHistory.ExecuteCommand(createCommand);
                
                // 获取创建的物体引用（这里简化处理）
                _testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _testObject.name = "测试立方体";
                _testObject.transform.position = position;
            }
        }
        
        void TestMoveCommand()
        {
            if (_testObject != null)
            {
                Vector3 startPos = _testObject.transform.position;
                Vector3 endPos = startPos + Vector3.right * 2;
                
                MoveCommand moveCommand = new MoveCommand(_testObject.transform, startPos, endPos);
                _commandHistory.ExecuteCommand(moveCommand);
                
                Debug.Log($"测试移动命令: 从 {startPos} 到 {endPos}");
            }
        }
        
        void TestCreateCommand()
        {
            if (_testSpherePrefab != null)
            {
                Vector3 position = new Vector3(2, 1, 0);
                CreateObjectCommand createCommand = new CreateObjectCommand(_testSpherePrefab, position);
                _commandHistory.ExecuteCommand(createCommand);
                
                Debug.Log($"测试创建命令: 在位置 {position} 创建球体");
            }
            else
            {
                // 如果没有预制体，创建一个简单的球体
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "测试球体";
                sphere.transform.position = new Vector3(2, 1, 0);
                
                Debug.Log("创建了测试球体");
            }
        }
        
        void TestDeleteCommand()
        {
            if (_testObject != null)
            {
                DeleteObjectCommand deleteCommand = new DeleteObjectCommand(_testObject);
                _commandHistory.ExecuteCommand(deleteCommand);
                
                Debug.Log($"测试删除命令: 删除 {_testObject.name}");
            }
        }
        
        void TestUndoRedo()
        {
            Debug.Log("=== 测试撤销/重做功能 ===");
            
            // 撤销所有操作
            while (_commandHistory.CanUndo)
            {
                _commandHistory.Undo();
            }
            
            Debug.Log("所有操作已撤销");
            
            // 重新执行所有操作
            while (_commandHistory.CanRedo)
            {
                _commandHistory.Redo();
            }
            
            Debug.Log("所有操作已重做");
            Debug.Log("=== 命令模式测试演示结束 ===");
        }
        
        void Update()
        {
            // 键盘测试
            if (Input.GetKeyDown(KeyCode.U))
            {
                _commandHistory.Undo();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                _commandHistory.Redo();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                _commandHistory.Clear();
                Debug.Log("命令历史已清空");
            }
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 220, 300, 150));
            GUILayout.Label("命令模式测试控制:", GUI.skin.box);
            GUILayout.Label("U键: 撤销");
            GUILayout.Label("R键: 重做");
            GUILayout.Label("C键: 清空历史");
            GUILayout.Label($"可撤销: {_commandHistory.CanUndo}");
            GUILayout.Label($"可重做: {_commandHistory.CanRedo}");
            GUILayout.EndArea();
        }
    }
}