using UnityEngine;
using System;
using System.Collections.Generic;
using VInspector;

/// <summary>
/// 可保存组件的基类，提供公共的保存和恢复功能
/// </summary>
public abstract class BaseSavable : MonoBehaviour, ISaveable
{
    [Tooltip("用于实例化的预制体ID")]
    public string prefabID;
    
    [Serializable]
    public struct ConfigItemData
    {
        public string name;
        public string value;
        public ConfigType type;
    }

    public string PrefabID => prefabID;
    public abstract Type DataType { get; }

    /// <summary>
    /// 捕获IConfigurable组件的配置数据
    /// </summary>
    /// <param name="configurable">IConfigurable组件实例</param>
    /// <returns>配置数据列表</returns>
    protected List<ConfigItemData> CaptureConfigurableData(IConfigurable configurable)
    {
        List<ConfigItemData> configItemDataList = new List<ConfigItemData>();
        
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
                Debug.Log($"{GetType().Name}: 捕获了 {configItems.Count} 个配置项");
            }
        }
        else
        {
            Debug.Log($"{GetType().Name}: 没有找到IConfigurable组件，跳过配置数据保存");
        }

        return configItemDataList;
    }

    /// <summary>
    /// 恢复IConfigurable组件的配置数据
    /// </summary>
    /// <param name="configurable">IConfigurable组件实例</param>
    /// <param name="savedConfigItems">保存的配置数据</param>
    protected void RestoreConfigurableData(IConfigurable configurable, List<ConfigItemData> savedConfigItems)
    {
        if (configurable != null && savedConfigItems != null && savedConfigItems.Count > 0)
        {
            List<ConfigItem> currentConfigItems = configurable.GetConfigItems();
            if (currentConfigItems != null)
            {
                int restoredCount = 0;
                foreach (var savedItem in savedConfigItems)
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
                            Debug.Log($"{GetType().Name}: 已恢复配置项 '{savedItem.name}' 的值为 '{restoredValue}'");
                        }
                        else
                        {
                            Debug.LogWarning($"{GetType().Name}: 无法恢复配置项 '{savedItem.name}' 的值！");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{GetType().Name}: 在当前配置中找不到配置项 '{savedItem.name}'！");
                    }
                }
                Debug.Log($"{GetType().Name}: 配置状态已恢复，共恢复 {restoredCount}/{savedConfigItems.Count} 个配置项");
            }
        }
        else
        {
            Debug.Log($"{GetType().Name}: 没有配置数据需要恢复或没有找到IConfigurable组件");
        }
    }

    /// <summary>
    /// 将对象值转换为字符串
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <param name="type">值类型</param>
    /// <returns>字符串表示</returns>
    protected string ConvertToString(object value, ConfigType type)
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
    protected object ConvertFromString(string stringValue, ConfigType type)
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
            Debug.LogError($"{GetType().Name}: 转换值 '{stringValue}' 到类型 {type} 时出错: {e.Message}");
            return null;
        }
    }

    [Button]
    public void SetID()
    {
        prefabID = gameObject.name;
    }

    // 抽象方法，由子类实现
    public abstract object CaptureState();
    public abstract void RestoreState(object state);
}