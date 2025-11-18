using UnityEngine;
using YouYouTest.CommandFramework;

namespace YouYouTest.Test
{
    /// <summary>
    /// 测试CommandHistory的历史记录限制功能
    /// </summary>
    public class CommandHistoryTest : MonoBehaviour
    {
        public GameObject testPrefab;
        public int testHistoryLimit = 5;
        
        void Start()
        {
            // 设置历史记录限制
            CommandHistory.Instance.MaxHistorySize = testHistoryLimit;
            Debug.Log($"设置历史记录限制为: {testHistoryLimit}");
        }
        
        void Update()
        {
            // 按T键创建测试对象
            if (Input.GetKeyDown(KeyCode.T))
            {
                CreateTestObjects();
            }
            
            // 按U键撤销
            if (Input.GetKeyDown(KeyCode.U))
            {
                CommandHistory.Instance.Undo();
            }
            
            // 按R键重做
            if (Input.GetKeyDown(KeyCode.R))
            {
                CommandHistory.Instance.Redo();
            }
            
            // 按C键清空历史
            if (Input.GetKeyDown(KeyCode.C))
            {
                CommandHistory.Instance.Clear();
            }
            
            // 按L键调整历史记录限制
            if (Input.GetKeyDown(KeyCode.L))
            {
                // 随机设置一个新的限制值
                int newLimit = Random.Range(3, 10);
                CommandHistory.Instance.MaxHistorySize = newLimit;
                Debug.Log($"调整历史记录限制为: {newLimit}");
            }
            
            // 显示当前状态
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log($"当前撤销栈数量: {CommandHistory.Instance.UndoCount}");
                Debug.Log($"当前重做栈数量: {CommandHistory.Instance.RedoCount}");
                Debug.Log($"历史记录限制: {CommandHistory.Instance.MaxHistorySize}");
            }
        }
        
        /// <summary>
        /// 创建测试对象，超过历史记录限制
        /// </summary>
        private void CreateTestObjects()
        {
            if (testPrefab == null)
            {
                Debug.LogWarning("测试预制体未设置");
                return;
            }
            
            // 创建超过限制数量的对象
            for (int i = 0; i < testHistoryLimit + 3; i++)
            {
                Vector3 position = new Vector3(i * 2.0f, 0, 0);
                CreateObjectCommand command = new CreateObjectCommand(testPrefab, position);
                CommandHistory.Instance.ExecuteCommand(command);
            }
            
            Debug.Log($"已创建 {testHistoryLimit + 3} 个测试对象，超出历史记录限制");
        }
    }
}