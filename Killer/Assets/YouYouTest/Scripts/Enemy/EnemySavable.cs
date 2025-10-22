using UnityEngine;
using System;

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
        public int health;
        public bool hasFiredFirstShot;
    }

    public string PrefabID => prefabID;

    /// <summary>
    /// 捕获Enemy组件的状态
    /// </summary>
    /// <returns>包含Enemy数据的对象</returns>
    public object CaptureState()
    {
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogError("EnemySavable: 在同一个游戏对象上找不到Enemy组件！");
            return null;
        }

        // 使用反射来获取私有字段hasFiredFirstShot
        // 这是一个更通用的方法，可以访问非公有成员
        bool hasFiredFirstShot = false;
        var enemyType = typeof(Enemy);
        var fieldInfo = enemyType.GetField("hasFiredFirstShot", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (fieldInfo != null)
        {
            hasFiredFirstShot = (bool)fieldInfo.GetValue(enemyComponent);
        }
        else
        {
            Debug.LogWarning("EnemySavable: 无法找到Enemy类中的hasFiredFirstShot字段。请确保字段名正确。");
        }

        return new EnemySaveData
        {
            health = enemyComponent.health,
            hasFiredFirstShot = hasFiredFirstShot
        };
    }

    /// <summary>
    /// 恢复Enemy组件的状态
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

        EnemySaveData data = (EnemySaveData)state;

        // 恢复公有字段
        enemyComponent.health = data.health;

        // 使用反射来设置私有字段hasFiredFirstShot
        var enemyType = typeof(Enemy);
        var fieldInfo = enemyType.GetField("hasFiredFirstShot", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            fieldInfo.SetValue(enemyComponent, data.hasFiredFirstShot);
        }
        else
        {
            Debug.LogWarning("EnemySavable: 无法找到Enemy类中的hasFiredFirstShot字段来设置值。");
        }

        Debug.Log($"EnemySavable: 状态已恢复 - Health: {data.health}, HasFiredFirstShot: {data.hasFiredFirstShot}");
    }
}