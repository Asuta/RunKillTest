using UnityEngine;
using YouYouTest;
using System.Collections.Generic;
using VInspector;
using System.IO;
using System;

public class SelectSaveTest : MonoBehaviour
{
    private EditorPlayer editorPlayer;
    public Transform createPosition;

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




    [Button("读取第一个select存档")]
    public void LoadFirstSelectInfo(Transform createPosition)
    {
        if (createPosition == null)
        {
            Debug.LogError("创建位置不能为空");
            return;
        }

        string folderPath = GetSelectedObjectsFolderPath();
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("选中对象存档文件夹不存在");
            return;
        }

        // 获取所有存档文件并按时间排序，获取最新的一个
        var saveFiles = Directory.GetFiles(folderPath, "SelectedObjects_*.json");
        if (saveFiles.Length == 0)
        {
            Debug.LogError("没有找到选中对象存档文件");
            return;
        }

        // 按文件修改时间排序，获取最新的文件
        Array.Sort(saveFiles, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));
        string latestSaveFile = saveFiles[0];

        try
        {
            string jsonContent = File.ReadAllText(latestSaveFile);
            SelectedObjectsSaveData saveData = JsonUtility.FromJson<SelectedObjectsSaveData>(jsonContent);
            
            if (saveData == null || saveData.objects == null || saveData.objects.Count == 0)
            {
                Debug.LogError("存档数据为空或格式错误");
                return;
            }

            Debug.Log($"开始加载选中对象存档，共 {saveData.objectCount} 个对象");
            Debug.Log($"存档时间: {saveData.saveTime}");
            Debug.Log($"原始中心位置: {saveData.centerPosition}");
            Debug.Log($"新的创建中心位置: {createPosition.position}");

            // 加载所有对象
            LoadSelectedObjects(saveData, createPosition.position);
            
            Debug.Log("选中对象存档加载完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载选中对象存档失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载选中对象存档数据
    /// </summary>
    /// <param name="saveData">存档数据</param>
    /// <param name="newCenterPosition">新的中心位置</param>
    private void LoadSelectedObjects(SelectedObjectsSaveData saveData, Vector3 newCenterPosition)
    {
        if (saveData == null || saveData.objects == null || saveData.objects.Count == 0)
        {
            Debug.LogWarning("LoadSelectedObjects: 存档数据为空或没有对象");
            return;
        }

        Debug.Log($"原始中心位置: {saveData.centerPosition}");
        Debug.Log($"新的创建中心位置: {newCenterPosition}");

        foreach (var objectData in saveData.objects)
        {
            try
            {
                // 根据prefabID获取预制体
                GameObject prefab = GetPrefabByID(objectData.prefabID);
                if (prefab == null)
                {
                    Debug.LogWarning($"找不到预制体ID: {objectData.prefabID}，跳过对象 {objectData.objectName}");
                    continue;
                }

                // 直接按世界坐标生成：newCenterPosition + 相对坐标（objectData.position）
                Vector3 worldPosition = newCenterPosition + objectData.position;
                GameObject newObject = Instantiate(prefab, worldPosition, objectData.rotation);
                newObject.transform.localScale = objectData.scale;
                newObject.name = objectData.objectName;
 
                Debug.Log($"创建对象: {objectData.objectName}，相对位置: {objectData.position}，世界位置: {worldPosition}");
                
                // 应用自定义数据（在 TransformSavable 恢复中会以传入的 worldPosition 覆盖位置）
                ApplyCustomDataToObject(newObject, objectData.customData, worldPosition);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载对象 {objectData.objectName} 失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 根据预制体ID获取预制体
    /// </summary>
    /// <param name="prefabID">预制体ID</param>
    /// <returns>预制体GameObject</returns>
    private GameObject GetPrefabByID(string prefabID)
    {
        GameObject prefab = null;
        
        // 尝试多个可能的路径，与ObjectSaveManager保持一致
        string[] possiblePaths = {
            prefabID, // 直接使用预制体名
            $"Prefabs/{prefabID}", // Prefabs/预制体名
            $"Prefabs/EditorElement/{prefabID}", // Prefabs/EditorElement/预制体名
            $"EditorElement/{prefabID}" // EditorElement/预制体名
        };
        
        foreach (string path in possiblePaths)
        {
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"成功找到预制体: {path}");
                return prefab;
            }
        }
        
        Debug.LogWarning($"无法找到预制体: {prefabID}，已尝试以下路径: {string.Join(", ", possiblePaths)}");
        return null;
    }

    /// <summary>
    /// 应用自定义数据到对象
    /// </summary>
    /// <param name="targetObject">目标对象</param>
    /// <param name="customDataJson">自定义数据JSON</param>
    /// <param name="worldPosition">世界位置</param>
    private void ApplyCustomDataToObject(GameObject targetObject, string customDataJson, Vector3 worldPosition)
    {
        if (string.IsNullOrEmpty(customDataJson))
        {
            return;
        }

        try
        {
            var customData = JsonUtility.FromJson<SerializationHelper>(customDataJson);
            if (customData == null || customData.keys == null || customData.jsonValues == null)
            {
                return;
            }

            for (int i = 0; i < customData.keys.Count; i++)
            {
                string key = customData.keys[i];
                string jsonValue = customData.jsonValues[i];

                // 根据key找到对应的组件并应用数据
                ApplyDataByComponentKey(targetObject, key, jsonValue, worldPosition);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"应用自定义数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 根据组件键应用数据
    /// </summary>
    /// <param name="targetObject">目标对象</param>
    /// <param name="componentKey">组件键</param>
    /// <param name="jsonValue">JSON数据</param>
    /// <param name="worldPosition">世界位置</param>
    private void ApplyDataByComponentKey(GameObject targetObject, string componentKey, string jsonValue, Vector3 worldPosition)
    {
        switch (componentKey)
        {
            case "TransformSavable":
                // TransformSavable数据已经在创建对象时应用了位置、旋转和缩放
                // 这里可以处理额外的TransformSavable特定数据
                var transformSavable = targetObject.GetComponent<TransformSavable>();
                if (transformSavable != null)
                {
                    // 如果TransformSavable有额外的配置项，可以在这里应用
                    try
                    {
                        var transformData = JsonUtility.FromJson<TransformSavable.TransformSaveData>(jsonValue);

                        // 使用加载时计算出的世界位置覆盖存档中的位置，避免被存档中的绝对位置覆盖
                        transformData.position = worldPosition;

                        // 恢复Transform（包含位置、旋转、缩放及配置项）
                        transformSavable.RestoreState(transformData);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"解析TransformSavable数据失败: {e.Message}");
                    }
                }
                break;
                
            // 可以添加其他组件类型的处理逻辑
            // case "EnemySavable":
            //     var enemySavable = targetObject.GetComponent<EnemySavable>();
            //     if (enemySavable != null)
            //     {
            //         // 应用敌人特定数据
            //     }
            //     break;
                
            default:
                Debug.LogWarning($"未知的组件类型: {componentKey}");
                break;
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
                    for (int i = 0; i < helper.keys.Count; i++)
                    {
                        customState[helper.keys[i]] = helper.jsonValues[i];
                    }
                }
            }
            catch (System.Exception e)
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
