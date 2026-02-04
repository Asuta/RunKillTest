using UnityEngine;

public class DeviceIDManager
{
    // 对外统一接口
    public static string GetDeviceID()
    {
        string deviceId = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        deviceId = GetAndroidID();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
        deviceId = GetWindowsPersistentGuid();
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

    // --- Windows 平台实现（使用持久化 GUID 文件，避免注册表依赖） ---
    private static string GetWindowsPersistentGuid()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, "device_id.txt");
            if (System.IO.File.Exists(path))
            {
                var existing = System.IO.File.ReadAllText(path);
                if (!string.IsNullOrEmpty(existing)) return existing;
            }

            var guid = System.Guid.NewGuid().ToString("N");
            System.IO.File.WriteAllText(path, guid);
            return guid;
        }
        catch (System.Exception e)
        {
            Debug.LogError("生成/读取持久化设备ID失败: " + e.Message);
            return null;
        }
#else
        return null;
#endif
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
