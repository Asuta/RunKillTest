using UnityEngine;
using System;
using VInspector;

/// <summary>
/// 用于保存Enemy组件数据的独立脚本
/// 将此脚本挂载到与Enemy脚本相同的游戏对象上
/// </summary>
public class EnemySavable : MonoBehaviour, ISaveable
{
    [Tooltip("用于实例化的预制体ID")]
    public string prefabID;
    
    [Serializable]
    public struct EnemySaveData
    {
        public Vector3 checkBoxPosition;
        public Quaternion checkBoxRotation;
        public Vector3 checkBoxScale;
    }

    public string PrefabID => prefabID;
    public Type DataType => typeof(EnemySaveData);

    /// <summary>
    /// 捕获Enemy组件的CheckBox Transform状态
    /// </summary>
    /// <returns>包含CheckBox Transform数据的对象</returns>
    public object CaptureState()
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

        return new EnemySaveData
        {
            checkBoxPosition = enemyComponent.checkBox.position,
            checkBoxRotation = enemyComponent.checkBox.rotation,
            checkBoxScale = enemyComponent.checkBox.localScale
        };
    }

    /// <summary>
    /// 恢复Enemy组件的CheckBox Transform状态
    /// </summary>
    /// <param name="state">从文件中加载的数据对象</param>
    public void RestoreState(object state)
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
    }

    [Button]
    public void SetID()
    {
        prefabID = gameObject.name;
    }
}