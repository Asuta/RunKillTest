using UnityEngine;
using System;
using System.Collections.Generic;
using VInspector;

/// <summary>
/// 用于保存Enemy组件数据的独立脚本
/// 将此脚本挂载到与Enemy脚本相同的游戏对象上
/// </summary>
public class EnemySavable : BaseSavable
{
    [Serializable]
    public struct EnemySaveData
    {
        public Vector3 checkBoxPosition;
        public Quaternion checkBoxRotation;
        public Vector3 checkBoxScale;
        
        // IConfigurable 相关数据
        public List<ConfigItemData> configItems;
    }

    public override Type DataType => typeof(EnemySaveData);

    /// <summary>
    /// 捕获Enemy组件的CheckBox Transform状态和IConfigurable配置数据
    /// </summary>
    /// <returns>包含CheckBox Transform数据和配置数据的对象</returns>
    public override object CaptureState()
    {
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogError("EnemySavable: 在同一个游戏对象上找不到Enemy组件！");
            return null;
        }

        if (enemyComponent.checkBox == null)
        {
            Debug.LogError("EnemySavable: Enemy组件上的checkBox为空！");
            return null;
        }

        // 使用基类方法捕获IConfigurable数据
        List<ConfigItemData> configItemDataList = CaptureConfigurableData(enemyComponent as IConfigurable);

        return new EnemySaveData
        {
            checkBoxPosition = enemyComponent.checkBox.position,
            checkBoxRotation = enemyComponent.checkBox.rotation,
            checkBoxScale = enemyComponent.checkBox.localScale,
            configItems = configItemDataList
        };
    }

    /// <summary>
    /// 恢复Enemy组件的CheckBox Transform状态和IConfigurable配置数据
    /// </summary>
    /// <param name="state">从文件中加载的数据对象</param>
    public override void RestoreState(object state)
    {
        if (!(state is EnemySaveData))
        {
            Debug.LogWarning("EnemySavable: 尝试恢复状态时，数据类型不匹配！");
            return;
        }

        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogError("EnemySavable: 在同一个游戏对象上找不到Enemy组件！");
            return;
        }

        if (enemyComponent.checkBox == null)
        {
            Debug.LogError("EnemySavable: Enemy组件上的checkBox为空，无法恢复状态！");
            return;
        }

        EnemySaveData data = (EnemySaveData)state;

        // 恢复CheckBox的Transform
        enemyComponent.checkBox.position = data.checkBoxPosition;
        enemyComponent.checkBox.rotation = data.checkBoxRotation;
        enemyComponent.checkBox.localScale = data.checkBoxScale;

        Debug.Log($"EnemySavable: CheckBox的Transform状态已恢复。");

        // 使用基类方法恢复IConfigurable数据
        RestoreConfigurableData(enemyComponent as IConfigurable, data.configItems);
    }

}