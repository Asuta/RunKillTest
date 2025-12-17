using UnityEngine;
#if UNITY_STANDALONE_WIN
using Microsoft.Win32; // 引用 Windows 注册表命名空间
#endif

public class DeviceIDManager
{
    // 对外统一接口
    public static string GetDeviceID()
    {
        string deviceId = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        deviceId = GetAndroidID();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
        deviceId = GetWindowsMachineGuid();
#else
        // 编辑器模式下返回测试ID
        deviceId = "EDITOR_TEST_ID_123456"; 
#endif

        // 如果上面都没取到（比如报错了），使用 Unity 自带的 ID 作为兜底
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = SystemInfo.deviceUniqueIdentifier;
        }

        return deviceId;
    }

    // --- Windows 平台实现 ---
    private static string GetWindowsMachineGuid()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            // 打开注册表项：HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
            {
                if (key != null)
                {
                    object val = key.GetValue("MachineGuid");
                    if (val != null)
                    {
                        return val.ToString();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("获取 Windows GUID 失败: " + e.Message);
        }
#endif
        return null;
    }

    // --- Android 平台实现 (上一条回复的内容) ---
    private static string GetAndroidID()
    {
#if UNITY_ANDROID
        try
        {
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
            AndroidJavaClass secure = new AndroidJavaClass("android.provider.Settings$Secure");
            return secure.CallStatic<string>("getString", contentResolver, "android_id");
        }
        catch (System.Exception)
        {
            return null;
        }
#else
        return null;
#endif
    }
}