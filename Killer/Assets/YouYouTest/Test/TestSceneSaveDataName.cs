using UnityEngine;
using YouYouTest;

public class TestSceneSaveDataName : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private string testSlotName = "TestLevel";
    [SerializeField] private string testLevelName = "测试关卡";
    [SerializeField] private string newLevelName = "新的关卡名称";
    
    [ContextMenu("测试保存场景对象")]
    public void TestSaveSceneObjects()
    {
        Debug.Log("=== 测试保存场景对象 ===");
        SaveLoadManager.Instance.SaveSceneObjects(testSlotName);
    }
    
    [ContextMenu("测试创建空档位")]
    public void TestCreateEmptySaveSlot()
    {
        Debug.Log("=== 测试创建空档位 ===");
        SaveLoadManager.Instance.CreateEmptySaveSlot(testSlotName);
    }
    
    [ContextMenu("测试读取所有存档档位")]
    public void TestGetAllSaveSlots()
    {
        Debug.Log("=== 测试读取所有存档档位 ===");
        SaveLoadManager.Instance.DebugAllSaveSlots();
    }
    
    [ContextMenu("测试加载场景对象")]
    public void TestLoadSceneObjects()
    {
        Debug.Log("=== 测试加载场景对象 ===");
        SaveLoadManager.Instance.LoadSceneObjects(testSlotName);
    }
    
    [ContextMenu("测试更新关卡名称")]
    public void TestUpdateLevelName()
    {
        Debug.Log("=== 测试更新关卡名称 ===");
        bool success = SaveLoadManager.Instance.UpdateLevelName(testSlotName, newLevelName);
        if (success)
        {
            Debug.Log($"关卡名称更新成功: {testSlotName} -> {newLevelName}");
        }
        else
        {
            Debug.LogError("关卡名称更新失败");
        }
    }
}