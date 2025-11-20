using UnityEngine;
using YouYouTest;
using System.Collections.Generic;
using VInspector;
using System.IO;
using System;

public class SelectSaveTest : MonoBehaviour
{
    private EditorPlayer editorPlayer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取EditorPlayer实例
        editorPlayer = FindObjectOfType<EditorPlayer>();
        if (editorPlayer == null)
        {
            Debug.LogError("未找到EditorPlayer实例");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 按下空格键时记录选中的对象
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LogSelectedObjects();
        }
    }
    
    /// <summary>
    /// 获取并记录EditorPlayer中所有正在被select的对象，并在它们的中心位置生成一个立方体
    /// </summary>
    [Button("记录选中对象")]
    public void LogSelectedObjects()
    {
        if (editorPlayer == null)
        {
            Debug.LogError("EditorPlayer实例为空，无法获取选中对象");
            return;
        }
        
        // 使用反射获取私有的GetSelectedObjects方法
        var getSelectedObjectsMethod = typeof(EditorPlayer).GetMethod("GetSelectedObjects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (getSelectedObjectsMethod != null)
        {
            var selectedObjects = getSelectedObjectsMethod.Invoke(editorPlayer, null) as GameObject[];
            
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                Debug.Log($"当前选中的对象数量: {selectedObjects.Length}");
                
                Vector3 centerPosition = Vector3.zero;
                int validObjectCount = 0;
                
                // 记录每个选中对象的信息并计算中心位置
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    if (selectedObjects[i] != null)
                    {
                        Debug.Log($"选中对象 {i + 1}: {selectedObjects[i].name} (位置: {selectedObjects[i].transform.position})");
                        centerPosition += selectedObjects[i].transform.position;
                        validObjectCount++;
                    }
                }
                
                // 计算中心位置
                if (validObjectCount > 0)
                {
                    centerPosition /= validObjectCount;
                    Debug.Log($"所有选中对象的中心位置: {centerPosition}");
                    
                    // 在中心位置生成一个立方体
                    CreateCubeAtCenter(centerPosition);
                }
            }
            else
            {
                Debug.Log("当前没有选中的对象");
            }
        }
        else
        {
            Debug.LogError("无法找到EditorPlayer的GetSelectedObjects方法");
        }
    }
    
    /// <summary>
    /// 在指定位置创建一个立方体
    /// </summary>
    /// <param name="position">立方体的位置</param>
    private void CreateCubeAtCenter(Vector3 position)
    {
        // 创建一个新的立方体
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "SelectObjectsCenterCube";
        cube.transform.position = position;
        
        // 设置立方体的大小（可选）
        cube.transform.localScale = Vector3.one * 0.2f;
        
        // 设置立方体的颜色为黄色以便识别
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }
        
        Debug.Log($"已在位置 {position} 创建中心立方体");
    }
    
    /// <summary>
    /// 保存当前选中的对象到专门的选中对象存档文件夹
    /// </summary>
    [Button("保存选中对象")]
    public void SaveSelectedObjects()
    {
        if (editorPlayer == null)
        {
            Debug.LogError("EditorPlayer实例为空，无法获取选中对象");
            return;
        }
        
        // 使用反射获取私有的GetSelectedObjects方法
        var getSelectedObjectsMethod = typeof(EditorPlayer).GetMethod("GetSelectedObjects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (getSelectedObjectsMethod != null)
        {
            var selectedObjects = getSelectedObjectsMethod.Invoke(editorPlayer, null) as GameObject[];
            
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                Debug.Log($"准备保存 {selectedObjects.Length} 个选中对象");
                
                Vector3 centerPosition = Vector3.zero;
                int validObjectCount = 0;
                
                // 计算中心位置
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    if (selectedObjects[i] != null)
                    {
                        centerPosition += selectedObjects[i].transform.position;
                        validObjectCount++;
                    }
                }
                
                if (validObjectCount > 0)
                {
                    centerPosition /= validObjectCount;
                    Debug.Log($"选中对象的中心位置: {centerPosition}");
                    
                    // 创建选中对象的保存数据
                    List<ObjectSaveData> selectedSaveDataList = new List<ObjectSaveData>();
                    
                    foreach (var obj in selectedObjects)
                    {
                        if (obj == null) continue;
                        
                        // 获取对象上的ISaveable组件
                        ISaveable[] saveableComponents = obj.GetComponents<ISaveable>();
                        
                        if (saveableComponents.Length > 0)
                        {
                            // 创建保存数据，使用相对坐标
                            ObjectSaveData saveData = new ObjectSaveData
                            {
                                prefabID = saveableComponents[0].PrefabID,
                                // 使用相对于中心位置的坐标
                                position = obj.transform.position - centerPosition,
                                rotation = obj.transform.rotation,
                                scale = obj.transform.localScale,
                                objectName = obj.name
                            };
                            
                            // 合并所有ISaveable组件的数据
                            foreach (var saveable in saveableComponents)
                            {
                                if (!string.IsNullOrEmpty(saveable.PrefabID))
                                {
                                    MergeCustomData(saveData, saveable);
                                }
                            }
                            
                            selectedSaveDataList.Add(saveData);
                            Debug.Log($"准备保存选中对象: {obj.name} (相对位置: {saveData.position})");
                        }
                        else
                        {
                            Debug.LogWarning($"对象 {obj.name} 没有ISaveable组件，跳过保存");
                        }
                    }
                    
                    if (selectedSaveDataList.Count > 0)
                    {
                        // 创建选中对象保存数据结构
                        SelectedObjectsSaveData selectedSaveData = new SelectedObjectsSaveData
                        {
                            saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            objectCount = selectedSaveDataList.Count,
                            centerPosition = centerPosition,
                            objects = selectedSaveDataList
                        };
                        
                        // 序列化为JSON
                        string jsonData = JsonUtility.ToJson(selectedSaveData, true);
                        
                        // 保存到专门的选中对象存档文件夹
                        bool success = SaveSelectedObjectsToFile("SelectedObjects_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json", jsonData);
                        
                        if (success)
                        {
                            Debug.Log($"成功保存 {selectedSaveDataList.Count} 个选中对象到专门的存档文件夹");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("没有找到可保存的选中对象");
                    }
                }
            }
            else
            {
                Debug.Log("当前没有选中的对象");
            }
        }
        else
        {
            Debug.LogError("无法找到EditorPlayer的GetSelectedObjects方法");
        }
    }
    
    /// <summary>
    /// 保存选中对象数据到专门的文件夹
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="jsonData">JSON数据</param>
    /// <returns>是否保存成功</returns>
    private bool SaveSelectedObjectsToFile(string fileName, string jsonData)
    {
        try
        {
            // 获取选中对象存档文件夹路径
            string selectedObjectsFolderPath = GetSelectedObjectsFolderPath();
            string filePath = Path.Combine(selectedObjectsFolderPath, fileName);
            
            // 确保目录存在
            if (!Directory.Exists(selectedObjectsFolderPath))
            {
                Directory.CreateDirectory(selectedObjectsFolderPath);
            }
            
            // 写入文件
            File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"成功保存选中对象数据到: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存选中对象文件失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取选中对象存档文件夹路径
    /// </summary>
    /// <returns>选中对象存档文件夹路径</returns>
    private string GetSelectedObjectsFolderPath()
    {
        // 在不同平台上使用不同的用户文件夹
        #if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "..", "UserSaveData", "SelectedObjects");
        #elif UNITY_STANDALONE_WIN
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "YourGameName", "SelectedObjects");
        #elif UNITY_STANDALONE_OSX
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Library", "Application Support", "YourGameName", "SelectedObjects");
        #elif UNITY_STANDALONE_LINUX
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), ".yourgamename", "SelectedObjects");
        #else
            return Path.Combine(Application.persistentDataPath, "SelectedObjects");
        #endif
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
                    for(int i = 0; i < helper.keys.Count; i++)
                    {
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
}
