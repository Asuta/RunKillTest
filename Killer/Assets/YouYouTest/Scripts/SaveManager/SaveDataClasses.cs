using UnityEngine;
using System.Collections.Generic;

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