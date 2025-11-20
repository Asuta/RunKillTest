using UnityEngine;
using System;
using System.Collections.Generic;
using VInspector;

/// <summary>
/// 通用的Transform存档组件
/// 将此脚本挂载到任何游戏对象上，即可保存和恢复其Transform信息
/// 同时支持保存IConfigurable接口的配置数据
/// </summary>
public class TransformSavable : MonoBehaviour, ISaveable
{
    [Tooltip("用于实例化的预制体ID")]
    public string prefabID;
    
    [Serializable]
    public struct TransformSaveData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        
        // IConfigurable 相关数据
        public List<ConfigItemData> configItems;
    }
    
    [Serializable]
    public struct ConfigItemData
    {
        public string name;
        public string value;
        public ConfigType type;
    }

    public string PrefabID => prefabID;
    public System.Type DataType => typeof(TransformSaveData);

    /// <summary>
    /// 捕获当前Transform的状态和IConfigurable配置数据
    /// </summary>
    /// <returns>包含Transform数据和配置数据的对象</returns>
    public object CaptureState()
    {
        // 捕获IConfigurable数据
        List<ConfigItemData> configItemDataList = new List<ConfigItemData>();
        
        // 查找同对象上的IConfigurable组件
        IConfigurable configurable = GetComponent<IConfigurable>();
        if (configurable != null)
        {
            List<ConfigItem> configItems = configurable.GetConfigItems();
            if (configItems != null)
            {
                foreach (var configItem in configItems)
                {
                    string stringValue = ConvertToString(configItem.Value, configItem.Type);
                    configItemDataList.Add(new ConfigItemData
                    {
                        name = configItem.Name,
                        value = stringValue,
                        type = configItem.Type
                    });
                }
                Debug.Log($"TransformSavable: 捕获了 {configItems.Count} 个配置项");
            }
        }
        else
        {
            Debug.Log("TransformSavable: 没有找到IConfigurable组件，跳过配置数据保存");
        }

        return new TransformSaveData
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            configItems = configItemDataList
        };
    }

    /// <summary>
    /// 恢复Transform的状态和IConfigurable配置数据
    /// </summary>
    /// <param name="state">从文件中加载的数据对象</param>
    public void RestoreState(object state)
    {
        if (!(state is TransformSaveData))
        {
            Debug.LogWarning("TransformSavable: 尝试恢复状态时，数据类型不匹配！");
            return;
        }

        TransformSaveData data = (TransformSaveData)state;

        transform.position = data.position;
        transform.rotation = data.rotation;
        transform.localScale = data.scale;

        Debug.Log($"TransformSavable: '{gameObject.name}' 的Transform状态已恢复。");

        // 恢复IConfigurable数据
        IConfigurable configurable = GetComponent<IConfigurable>();
        if (configurable != null && data.configItems != null && data.configItems.Count > 0)
        {
            List<ConfigItem> currentConfigItems = configurable.GetConfigItems();
            if (currentConfigItems != null)
            {
                int restoredCount = 0;
                foreach (var savedItem in data.configItems)
                {
                    var currentItem = currentConfigItems.Find(item => item.Name == savedItem.name);
                    if (currentItem != null)
                    {
                        object restoredValue = ConvertFromString(savedItem.value, savedItem.type);
                        if (restoredValue != null)
                        {
                            // 调用配置项的回调来更新值
                            currentItem.OnValueChanged?.Invoke(restoredValue);
                            restoredCount++;
                            Debug.Log($"TransformSavable: 已恢复配置项 '{savedItem.name}' 的值为 '{restoredValue}'");
                        }
                        else
                        {
                            Debug.LogWarning($"TransformSavable: 无法恢复配置项 '{savedItem.name}' 的值！");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"TransformSavable: 在当前配置中找不到配置项 '{savedItem.name}'！");
                    }
                }
                Debug.Log($"TransformSavable: 配置状态已恢复，共恢复 {restoredCount}/{data.configItems.Count} 个配置项");
            }
        }
        else
        {
            Debug.Log("TransformSavable: 没有配置数据需要恢复或没有找到IConfigurable组件");
        }
    }

    /// <summary>
    /// 将对象值转换为字符串
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <param name="type">值类型</param>
    /// <returns>字符串表示</returns>
    private string ConvertToString(object value, ConfigType type)
    {
        if (value == null) return "";
        
        switch (type)
        {
            case ConfigType.Int:
                return value.ToString();
            case ConfigType.Float:
                return value.ToString();
            case ConfigType.String:
                return value.ToString();
            case ConfigType.Bool:
                return value.ToString();
            default:
                return value.ToString();
        }
    }

    /// <summary>
    /// 将字符串转换为指定类型的对象
    /// </summary>
    /// <param name="stringValue">字符串值</param>
    /// <param name="type">目标类型</param>
    /// <returns>转换后的对象</returns>
    private object ConvertFromString(string stringValue, ConfigType type)
    {
        if (string.IsNullOrEmpty(stringValue)) return null;
        
        try
        {
            switch (type)
            {
                case ConfigType.Int:
                    return Convert.ToInt32(stringValue);
                case ConfigType.Float:
                    return Convert.ToSingle(stringValue);
                case ConfigType.String:
                    return stringValue;
                case ConfigType.Bool:
                    return Convert.ToBoolean(stringValue);
                default:
                    return stringValue;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TransformSavable: 转换值 '{stringValue}' 到类型 {type} 时出错: {e.Message}");
            return null;
        }
    }

    [Button]
    public void SetID()
    {
        prefabID = gameObject.name;
    }
}