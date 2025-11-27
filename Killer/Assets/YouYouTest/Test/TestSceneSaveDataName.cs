using UnityEngine;
using YouYouTest;

public class TestSceneSaveDataName : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private string testSlotName = "TestLevel";
    [SerializeField] private string testLevelName = "测试关卡";
    
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
}