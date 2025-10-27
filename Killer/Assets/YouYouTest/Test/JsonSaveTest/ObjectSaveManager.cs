using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 对象保存管理器，处理对象查找、保存和加载逻辑
/// </summary>
public class ObjectSaveManager : MonoBehaviour
{
    private bool enableDebugLog;
    
    public ObjectSaveManager(bool debugLog = true)
    {
        enableDebugLog = debugLog;
    }
    
    /// <summary>
    /// 收集场景中所有可保存对象的数据
    /// </summary>
    /// <returns>对象保存数据列表</returns>
    public List<ObjectSaveData> CollectSaveData()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始收集场景对象数据...");
        }
        
        // 查找所有MonoBehaviour，然后筛选出实现了ISaveable接口的
        MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
            return new List<ObjectSaveData>();
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

            // 检查GameObject是否处于激活状态，如果未激活则跳过保存
            if (!mb.gameObject.activeInHierarchy)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"对象 {mb.gameObject.name} 处于非激活状态，跳过保存");
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
        
        if (enableDebugLog)
        {
            Debug.Log($"收集完成，共 {saveDataList.Count} 个对象");
        }
        
        return saveDataList;
    }
    
    /// <summary>
    /// 加载单个对象
    /// </summary>
    /// <param name="objectData">对象数据</param>
    /// <returns>是否加载成功</returns>
    public bool LoadSingleObject(ObjectSaveData objectData)
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
    /// 清除现有的ObjectIdentifier对象（可选功能）
    /// </summary>
    public void ClearExistingObjects()
    {
        ObjectIdentifier[] existingObjects = FindObjectsByType<ObjectIdentifier>(FindObjectsSortMode.None);
        
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
        
        // 动态加载Resources文件夹中所有的预制体
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        Debug.Log($"Resources文件夹中共有 {allPrefabs.Length} 个预制体:");
        
        // 创建一个字典来存储预制体名称和对应的路径
        Dictionary<string, List<string>> prefabPaths = new Dictionary<string, List<string>>();
        
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab != null)
            {
                Debug.Log($"  - {prefab.name}");
                
                // 尝试找到预制体的实际路径
                string prefabPath = GetPrefabPath(prefab);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    if (!prefabPaths.ContainsKey(prefab.name))
                    {
                        prefabPaths[prefab.name] = new List<string>();
                    }
                    prefabPaths[prefab.name].Add(prefabPath);
                }
            }
        }
        
        // 输出找到的预制体路径信息
        Debug.Log("=== 预制体路径信息 ===");
        foreach (var kvp in prefabPaths)
        {
            foreach (string path in kvp.Value)
            {
                Debug.Log($"✓ 找到预制体: {kvp.Key} (路径: {path})");
            }
        }
        
        Debug.Log("=== Debug完成 ===");
    }
    
    /// <summary>
    /// 获取预制体在Resources文件夹中的相对路径
    /// </summary>
    /// <param name="prefab">预制体对象</param>
    /// <returns>相对路径，如果找不到则返回空字符串</returns>
    private string GetPrefabPath(GameObject prefab)
    {
        if (prefab == null) return string.Empty;
        
        // 尝试常见的路径结构
        string[] possiblePaths = {
            prefab.name,
            $"Prefabs/{prefab.name}",
            $"Prefabs/EditorElement/{prefab.name}",
            $"EditorElement/{prefab.name}"
        };
        
        foreach (string path in possiblePaths)
        {
            GameObject testPrefab = Resources.Load<GameObject>(path);
            if (testPrefab != null && testPrefab == prefab)
            {
                return path;
            }
        }
        
        return string.Empty;
    }
}