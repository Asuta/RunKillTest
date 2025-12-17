using UnityEngine;

public class WEBTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TestDeviceIDManager();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 测试DeviceIDManager的设备ID获取功能
    /// </summary>
    [ContextMenu("Test DeviceIDManager")]
    private void TestDeviceIDManager()
    {
        Debug.Log("=== DeviceIDManager 测试开始 ===");
        
        // 测试获取设备ID
        string deviceId = DeviceIDManager.GetDeviceID();
        Debug.Log($"设备ID: {deviceId}");
        
        // 验证设备ID是否为空或null
        if (string.IsNullOrEmpty(deviceId))
        {
            Debug.LogError("设备ID获取失败，返回为空或null");
        }
        else
        {
            Debug.Log("设备ID获取成功");
            
            // 检查是否为编辑器测试ID
            if (deviceId == "EDITOR_TEST_ID_123456")
            {
                Debug.Log("当前在编辑器模式下，返回的是测试ID");
            }
            else
            {
                Debug.Log("当前在运行时模式，返回的是真实设备ID");
            }
        }
        
        // 额外测试：多次调用看结果是否一致
        string deviceId2 = DeviceIDManager.GetDeviceID();
        Debug.Log($"第二次获取设备ID: {deviceId2}");
        
        if (deviceId == deviceId2)
        {
            Debug.Log("设备ID一致性验证通过");
        }
        else
        {
            Debug.LogWarning("设备ID不一致，可能存在异常");
        }
        
        // 输出系统信息
        Debug.Log($"系统设备唯一标识符: {SystemInfo.deviceUniqueIdentifier}");
        
        Debug.Log("=== DeviceIDManager 测试结束 ===");
    }
}
