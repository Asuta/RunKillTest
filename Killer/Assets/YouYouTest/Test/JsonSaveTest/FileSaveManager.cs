using UnityEngine;
using System.IO;

/// <summary>
/// 文件保存管理器，处理文件读写操作
/// </summary>
public class FileSaveManager
{
    private bool enableDebugLog;
    
    public FileSaveManager(bool debugLog = true)
    {
        enableDebugLog = debugLog;
    }
    
    /// <summary>
    /// 获取用户文件夹路径
    /// </summary>
    /// <returns>用户文件夹路径</returns>
    public string GetUserFolderPath()
    {
        // 在不同平台上使用不同的用户文件夹
        #if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "..", "UserSaveData");
        #elif UNITY_STANDALONE_WIN
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "YourGameName");
        #elif UNITY_STANDALONE_OSX
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Library", "Application Support", "YourGameName");
        #elif UNITY_STANDALONE_LINUX
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), ".yourgamename");
        #else
            return Application.persistentDataPath;
        #endif
    }
    
    /// <summary>
    /// 保存数据到文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="jsonData">JSON数据</param>
    /// <returns>是否保存成功</returns>
    public bool SaveToFile(string fileName, string jsonData)
    {
        try
        {
            // 获取用户文件夹路径
            string userFolderPath = GetUserFolderPath();
            string filePath = Path.Combine(userFolderPath, fileName);
            
            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 写入文件
            File.WriteAllText(filePath, jsonData);
            
            if (enableDebugLog)
            {
                Debug.Log($"成功保存数据到: {filePath}");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存文件失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 从文件加载数据
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件内容，如果失败返回null</returns>
    public string LoadFromFile(string fileName)
    {
        try
        {
            // 获取文件路径
            string userFolderPath = GetUserFolderPath();
            string filePath = Path.Combine(userFolderPath, fileName);
            
            if (!File.Exists(filePath))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"保存文件不存在: {filePath}");
                }
                return null;
            }
            
            // 读取文件内容
            string jsonData = File.ReadAllText(filePath);
            
            if (enableDebugLog)
            {
                Debug.Log($"成功从文件加载数据: {filePath}");
            }
            
            return jsonData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载文件失败: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件是否存在</returns>
    public bool FileExists(string fileName)
    {
        string userFolderPath = GetUserFolderPath();
        string filePath = Path.Combine(userFolderPath, fileName);
        return File.Exists(filePath);
    }
}