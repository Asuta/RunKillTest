using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SaveTestt : MonoBehaviour
{
    [Header("保存设置")]
    [SerializeField] private string saveFileName = "SceneObjects.json";
    [SerializeField] private bool autoSaveOnStart = false;
    [SerializeField] private bool enableDebugLog = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (autoSaveOnStart)
        {
            SaveSceneObjects();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 按S键保存场景对象
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveSceneObjects();
        }
        
        // 按L键加载场景对象
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadSceneObjects();
        }
        
        // 按D键调试Resources文件夹内容
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugResourcesContent();
        }
    }
    
    /// <summary>
    /// 保存场景中所有ObjectIdentifier对象的信息到JSON文件
    /// </summary>
    public void SaveSceneObjects()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始保存场景对象...");
        }
        
        // 查找所有ObjectIdentifier组件
        ObjectIdentifier[] objectIdentifiers = FindObjectsOfType<ObjectIdentifier>();
        
        if (objectIdentifiers.Length == 0)
        {
            Debug.LogWarning("场景中没有找到任何ObjectIdentifier组件!");
            return;
        }
        
        // 创建保存数据列表
        List<ObjectSaveData> saveDataList = new List<ObjectSaveData>();
        
        // 遍历所有ObjectIdentifier组件
        foreach (ObjectIdentifier objIdentifier in objectIdentifiers)
        {
            if (objIdentifier == null || string.IsNullOrEmpty(objIdentifier.prefabID))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("发现空的ObjectIdentifier或prefabID为空，跳过保存");
                }
                continue;
            }
            
            // 创建保存数据
            ObjectSaveData saveData = new ObjectSaveData
            {
                prefabID = objIdentifier.prefabID,
                position = objIdentifier.transform.position,
                rotation = objIdentifier.transform.rotation,
                scale = objIdentifier.transform.localScale,
                objectName = objIdentifier.gameObject.name
            };
            
            saveDataList.Add(saveData);
            
            if (enableDebugLog)
            {
                Debug.Log($"准备保存对象: {saveData.objectName} (ID: {saveData.prefabID})");
            }
        }
        
        // 创建完整的保存数据
        SceneSaveData sceneData = new SceneSaveData
        {
            saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            objectCount = saveDataList.Count,
            objects = saveDataList
        };
        
        // 序列化为JSON
        string jsonData = JsonUtility.ToJson(sceneData, true);
        
        // 获取用户文件夹路径
        string userFolderPath = GetUserFolderPath();
        string filePath = Path.Combine(userFolderPath, saveFileName);
        
        try
        {
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
                Debug.Log($"成功保存 {saveDataList.Count} 个对象到: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存文件失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 从JSON文件加载场景对象
    /// </summary>
    public void LoadSceneObjects()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始加载场景对象...");
        }
        
        // 获取文件路径
        string userFolderPath = GetUserFolderPath();
        string filePath = Path.Combine(userFolderPath, saveFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"保存文件不存在: {filePath}");
            return;
        }
        
        try
        {
            // 读取文件内容
            string jsonData = File.ReadAllText(filePath);
            
            // 反序列化JSON数据
            SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(jsonData);
            
            if (sceneData == null || sceneData.objects == null)
            {
                Debug.LogError("无法解析保存文件数据!");
                return;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"准备加载 {sceneData.objectCount} 个对象 (保存时间: {sceneData.saveTime})");
            }
            
            // 清除现有的ObjectIdentifier对象（可选）
            // ClearExistingObjects();
            
            // 加载每个对象
            int loadedCount = 0;
            foreach (ObjectSaveData objectData in sceneData.objects)
            {
                if (LoadSingleObject(objectData))
                {
                    loadedCount++;
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"成功加载 {loadedCount}/{sceneData.objectCount} 个对象");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载文件失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 加载单个对象
    /// </summary>
    /// <param name="objectData">对象数据</param>
    /// <returns>是否加载成功</returns>
    private bool LoadSingleObject(ObjectSaveData objectData)
    {
        if (string.IsNullOrEmpty(objectData.prefabID))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("对象数据的prefabID为空，跳过加载");
            }
            return false;
        }
        
        GameObject prefab = null;
        
        // 尝试多种路径来加载预制体
        string[] possiblePaths = {
            objectData.prefabID, // 直接使用prefabID
            $"Prefabs/{objectData.prefabID}", // Prefabs/预制体名
            $"Prefabs/EditorElement/{objectData.prefabID}", // Prefabs/EditorElement/预制体名
            $"EditorElement/{objectData.prefabID}" // EditorElement/预制体名
        };
        
        foreach (string path in possiblePaths)
        {
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"成功找到预制体: {path}");
                }
                break;
            }
        }
        
        if (prefab == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"无法找到预制体: {objectData.prefabID}，已尝试以下路径: {string.Join(", ", possiblePaths)}");
            }
            return false;
        }
        
        // 实例化预制体
        GameObject newObj = Instantiate(prefab);
        newObj.name = objectData.objectName;
        
        // 设置Transform
        newObj.transform.position = objectData.position;
        newObj.transform.rotation = objectData.rotation;
        newObj.transform.localScale = objectData.scale;
        
        if (enableDebugLog)
        {
            Debug.Log($"成功加载对象: {objectData.objectName} (ID: {objectData.prefabID})");
        }
        
        return true;
    }
    
    /// <summary>
    /// 清除现有的ObjectIdentifier对象（可选功能）
    /// </summary>
    private void ClearExistingObjects()
    {
        ObjectIdentifier[] existingObjects = FindObjectsOfType<ObjectIdentifier>();
        
        foreach (ObjectIdentifier obj in existingObjects)
        {
            if (obj != null && obj.gameObject != null)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"清除现有对象: {obj.gameObject.name}");
                }
                DestroyImmediate(obj.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 调试Resources文件夹内容
    /// </summary>
    public void DebugResourcesContent()
    {
        Debug.Log("=== Debug Resources文件夹内容 ===");
        
        // 尝试加载所有可能路径的预制体
        string[] testPaths = {
            "Wall",
            "Prefabs/Wall",
            "Prefabs/EditorElement/Wall",
            "EditorElement/Wall",
            "Enemy",
            "Prefabs/Enemy",
            "Prefabs/EditorElement/Enemy",
            "EditorElement/Enemy",
            "CheckPoint",
            "Prefabs/CheckPoint",
            "Prefabs/EditorElement/CheckPoint",
            "EditorElement/CheckPoint"
        };
        
        foreach (string path in testPaths)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"✓ 找到预制体: {path}");
            }
            else
            {
                Debug.Log($"✗ 未找到预制体: {path}");
            }
        }
        
        // 列出Resources文件夹中所有的预制体
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        Debug.Log($"Resources文件夹中共有 {allPrefabs.Length} 个预制体:");
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab != null)
            {
                Debug.Log($"  - {prefab.name}");
            }
        }
        
        Debug.Log("=== Debug完成 ===");
    }
    
    /// <summary>
    /// 获取用户文件夹路径
    /// </summary>
    /// <returns>用户文件夹路径</returns>
    private string GetUserFolderPath()
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
}

/// <summary>
/// 单个对象的保存数据结构
/// </summary>
[System.Serializable]
public class ObjectSaveData
{
    public string prefabID;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string objectName;
}

/// <summary>
/// 整个场景的保存数据结构
/// </summary>
[System.Serializable]
public class SceneSaveData
{
    public string saveTime;
    public int objectCount;
    public List<ObjectSaveData> objects;
}
