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
    /// 保存场景中所有实现了ISaveable接口的对象的信息到JSON文件
    /// </summary>
    public void SaveSceneObjects()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始保存场景对象...");
        }
        
        // 查找所有MonoBehaviour，然后筛选出实现了ISaveable接口的
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        var saveableObjects = new List<MonoBehaviour>();
        
        foreach (var mb in allMonoBehaviours)
        {
            if (mb is ISaveable)
            {
                saveableObjects.Add(mb);
            }
        }

        if (saveableObjects.Count == 0)
        {
            Debug.LogWarning("场景中没有找到任何实现ISaveable接口的组件!");
            return;
        }
        
        // 创建保存数据列表
        List<ObjectSaveData> saveDataList = new List<ObjectSaveData>();
        
        // 遍历所有ISaveable组件
        foreach (var mb in saveableObjects)
        {
            ISaveable saveable = mb as ISaveable;
            if (saveable == null || string.IsNullOrEmpty(saveable.PrefabID))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"发现空的ISaveable组件或PrefabID为空，跳过保存。对象: {mb?.gameObject.name}");
                }
                continue;
            }

            // 检查是否已经保存过同一个GameObject上的其他ISaveable组件
            // 我们将同一个GameObject上的所有ISaveable数据合并到一个ObjectSaveData中
            GameObject obj = mb.gameObject;
            ObjectSaveData existingData = saveDataList.Find(data => data.objectName == obj.name);
            
            if (existingData != null)
            {
                // 如果已存在，只需更新customData
                MergeCustomData(existingData, saveable);
            }
            else
            {
                // 如果不存在，创建新的保存数据
                ObjectSaveData newSaveData = new ObjectSaveData
                {
                    prefabID = saveable.PrefabID,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    scale = obj.transform.localScale,
                    objectName = obj.name
                };
                
                // 创建并合并自定义数据
                MergeCustomData(newSaveData, saveable);

                saveDataList.Add(newSaveData);
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"准备保存对象: {mb.gameObject.name} (ID: {saveable.PrefabID})");
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
    /// 合并ISaveable组件的数据到ObjectSaveData中
    /// </summary>
    /// <param name="saveData">要合并到的目标数据</param>
    /// <param name="saveable">提供数据的ISaveable组件</param>
    private void MergeCustomData(ObjectSaveData saveData, ISaveable saveable)
    {
        var customState = new System.Collections.Generic.Dictionary<string, object>();
        
        // 如果customData不为空，先反序列化它
        if (!string.IsNullOrEmpty(saveData.customData))
        {
            try
            {
                SerializationHelper helper = JsonUtility.FromJson<SerializationHelper>(saveData.customData);
                if (helper != null && helper.keys != null && helper.jsonValues != null)
                {
                    // 这里是关键：我们应该从helper重建字典，而不是直接用ToDictionary()
                    // 因为ToDictionary()会再次反序列化，而此时我们只需要键值对
                    for(int i = 0; i < helper.keys.Count; i++)
                    {
                        // 暂时将json字符串存入，避免类型问题
                        // 在ToDictionary()时才真正反序列化
                        customState[helper.keys[i]] = helper.jsonValues[i];
                    }
                }
            }
            catch(System.Exception e)
            {
                 Debug.LogError($"MergeCustomData: 反序列化旧数据时出错: {e.Message}");
            }
        }
        
        // 添加新的组件数据
        customState[saveable.GetType().ToString()] = saveable.CaptureState();
        
        // 重新序列化并保存
        saveData.customData = JsonUtility.ToJson(new SerializationHelper(customState));
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
        
        // --- 逻辑：加载自定义组件数据 ---
        if (!string.IsNullOrEmpty(objectData.customData))
        {
            try
            {
                if (enableDebugLog) Debug.Log($"准备恢复自定义数据: {objectData.customData}");

                // 反序列化自定义数据
                SerializationHelper helper = JsonUtility.FromJson<SerializationHelper>(objectData.customData);
                if (helper != null)
                {
                    if (enableDebugLog) Debug.Log("SerializationHelper 反序列化成功。");
                    // ToDictionary() 现在需要 newObj 来查找组件并获取正确的DataType
                    var customState = helper.ToDictionary(newObj);
                    
                    foreach (var stateEntry in customState)
                    {
                        if (enableDebugLog) Debug.Log($"正在处理组件类型: {stateEntry.Key}");
                        
                        // 获取组件类型
                        System.Type componentType = System.Type.GetType(stateEntry.Key);
                        if (componentType != null)
                        {
                            if (enableDebugLog) Debug.Log($"找到类型: {componentType.FullName}");

                            // 尝试获取该类型的组件
                            var component = newObj.GetComponent(componentType) as ISaveable;
                            if (component != null)
                            {
                                if (enableDebugLog) Debug.Log($"在 '{newObj.name}' 上找到组件 {stateEntry.Key}，准备恢复状态。");

                                // ToDictionary() 已经为我们反序列化好了，直接使用
                                object restoredState = stateEntry.Value;
                                if (enableDebugLog) Debug.Log($"从字典获取状态对象: {(restoredState != null ? "不为空" : "为空")}");
                                
                                if(restoredState != null)
                                {
                                    component.RestoreState(restoredState);
                                }
                                else
                                {
                                    Debug.LogError($"恢复状态失败，因为从字典获取的状态对象为null。类型: {stateEntry.Key}");
                                }
                                
                                if (enableDebugLog)
                                {
                                    Debug.Log($"已恢复组件 {stateEntry.Key} 的状态");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"在 '{newObj.name}' 上未找到组件 {stateEntry.Key} 或其未实现ISaveable。");
                            }
                        }
                        else
                        {
                            Debug.LogError($"无法从字符串 '{stateEntry.Key}' 获取类型。");
                        }
                    }
                }
                else
                {
                    Debug.LogError("SerializationHelper 反序列化失败，helper为null。");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"恢复自定义数据时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        // --- 结束 ---
        
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
    
    // 新增字段：用于存储自定义组件数据
    public string customData;
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

/// <summary>
/// 辅助类，用于序列化Dictionary
/// </summary>
[System.Serializable]
public class SerializationHelper
{
    public List<string> keys;
    public List<string> jsonValues; // 改为存储JSON字符串

    public SerializationHelper(Dictionary<string, object> dictionary)
    {
        keys = new List<string>(dictionary.Keys);
        jsonValues = new List<string>();
        foreach (var kvp in dictionary)
        {
            if (kvp.Value is string)
            {
                // 如果值已经是json字符串（来自MergeCustomData），直接添加
                jsonValues.Add(kvp.Value as string);
            }
            else
            {
                // 否则，序列化它
                jsonValues.Add(JsonUtility.ToJson(kvp.Value));
            }
        }
    }

    public Dictionary<string, object> ToDictionary(GameObject targetObject)
    {
        var dictionary = new Dictionary<string, object>();
        if (keys == null || jsonValues == null || keys.Count != jsonValues.Count)
        {
            Debug.LogError("SerializationHelper: Keys and Values count mismatch or null lists!");
            return dictionary;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            // 根据key（组件类型名）获取组件
            System.Type componentType = System.Type.GetType(keys[i]);
            if (componentType != null)
            {
                var component = targetObject.GetComponent(componentType) as ISaveable;
                if (component != null)
                {
                    // 使用组件提供的DataType来反序列化
                    object obj = JsonUtility.FromJson(jsonValues[i], component.DataType);
                    dictionary[keys[i]] = obj;
                }
                else
                {
                    Debug.LogError($"SerializationHelper: Could not find component '{keys[i]}' on '{targetObject.name}'");
                }
            }
            else
            {
                Debug.LogError($"SerializationHelper: Could not find type for key '{keys[i]}'");
            }
        }
        return dictionary;
    }
}
