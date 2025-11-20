using UnityEngine;
using System;
using System.Collections.Generic;
using VInspector;

/// <summary>
/// 通用的Transform存档组件
/// 将此脚本挂载到任何游戏对象上，即可保存和恢复其Transform信息
/// 同时支持保存IConfigurable接口的配置数据
/// </summary>
public class TransformSavable : BaseSavable
{
    [Serializable]
    public struct TransformSaveData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        
        // IConfigurable 相关数据
        public List<ConfigItemData> configItems;
    }

    public override Type DataType => typeof(TransformSaveData);

    /// <summary>
    /// 捕获当前Transform的状态和IConfigurable配置数据
    /// </summary>
    /// <returns>包含Transform数据和配置数据的对象</returns>
    public override object CaptureState()
    {
        // 使用基类方法捕获IConfigurable数据
        List<ConfigItemData> configItemDataList = CaptureConfigurableData(GetComponent<IConfigurable>());

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
    public override void RestoreState(object state)
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

        // 使用基类方法恢复IConfigurable数据
        RestoreConfigurableData(GetComponent<IConfigurable>(), data.configItems);
    }

}