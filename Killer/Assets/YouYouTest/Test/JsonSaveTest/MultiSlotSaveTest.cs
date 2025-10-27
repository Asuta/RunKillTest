using UnityEngine;

/// <summary>
/// 多档位存档功能测试脚本
/// </summary>
public class MultiSlotSaveTest : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool enableTestOnStart = false;
    
    private void Start()
    {
        if (enableTestOnStart)
        {
            // 延迟执行测试，确保所有系统初始化完成
            Invoke(nameof(RunTest), 1f);
        }
    }
    
    private void Update()
    {
        // 按T键运行测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            RunTest();
        }
    }
    
    /// <summary>
    /// 运行多档位存档测试
    /// </summary>
    private void RunTest()
    {
        Debug.Log("=== 开始多档位存档测试 ===");
        
        SaveLoadManager saveManager = SaveLoadManager.Instance;
        
        // 测试1: 保存到不同档位
        Debug.Log("测试1: 保存到不同档位");
        saveManager.SaveSceneObjects("TestSlot1");
        saveManager.SaveSceneObjects("TestSlot2");
        saveManager.SaveSceneObjects("TestSlot3");
        
        // 测试2: 检查档位是否存在
        Debug.Log("测试2: 检查档位是否存在");
        Debug.Log($"TestSlot1 存在: {saveManager.SaveSlotExists("TestSlot1")}");
        Debug.Log($"TestSlot2 存在: {saveManager.SaveSlotExists("TestSlot2")}");
        Debug.Log($"TestSlot3 存在: {saveManager.SaveSlotExists("TestSlot3")}");
        Debug.Log($"NonExistentSlot 存在: {saveManager.SaveSlotExists("NonExistentSlot")}");
        
        // 测试3: 获取所有存档档位信息
        Debug.Log("测试3: 获取所有存档档位信息");
        var allSlots = saveManager.GetAllSaveSlots();
        Debug.Log($"总共找到 {allSlots.Count} 个存档档位");
        
        // 测试4: 从不同档位加载
        Debug.Log("测试4: 从不同档位加载");
        // 注意：这里只是测试加载功能，实际加载会清理场景
        // saveManager.LoadSceneObjects("TestSlot1");
        
        // 测试5: 删除档位
        Debug.Log("测试5: 删除档位");
        bool deleteResult1 = saveManager.DeleteSaveSlot("TestSlot1");
        bool deleteResult2 = saveManager.DeleteSaveSlot("NonExistentSlot");
        Debug.Log($"删除 TestSlot1 结果: {deleteResult1}");
        Debug.Log($"删除 NonExistentSlot 结果: {deleteResult2}");
        
        // 测试6: 再次检查档位
        Debug.Log("测试6: 删除后再次检查档位");
        Debug.Log($"TestSlot1 删除后存在: {saveManager.SaveSlotExists("TestSlot1")}");
        Debug.Log($"TestSlot2 仍然存在: {saveManager.SaveSlotExists("TestSlot2")}");
        
        // 测试7: 显示所有存档信息
        Debug.Log("测试7: 显示所有存档信息");
        saveManager.DebugAllSaveSlots();
        
        Debug.Log("=== 多档位存档测试完成 ===");
    }
    
    /// <summary>
    /// 在Inspector中显示测试说明
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("多档位存档测试说明:", GUI.skin.box);
        GUILayout.Label("按 T 键: 运行完整测试");
        GUILayout.Label("按 S 键: 保存到默认档位");
        GUILayout.Label("按 Shift+S: 保存到档位1");
        GUILayout.Label("按 Ctrl+S: 保存到档位2");
        GUILayout.Label("按 Alt+S: 保存到档位3");
        GUILayout.Label("按 L 键: 从默认档位加载");
        GUILayout.Label("按 Shift+L: 从档位1加载");
        GUILayout.Label("按 Ctrl+L: 从档位2加载");
        GUILayout.Label("按 Alt+L: 从档位3加载");
        GUILayout.Label("按 I 键: 显示所有存档信息");
        GUILayout.Label("按 Shift+D: 删除档位1");
        GUILayout.Label("按 Ctrl+D: 删除档位2");
        GUILayout.Label("按 Alt+D: 删除档位3");
        GUILayout.EndArea();
    }
}