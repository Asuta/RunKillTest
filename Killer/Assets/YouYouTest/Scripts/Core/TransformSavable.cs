using UnityEngine;
using System;
using VInspector;

/// <summary>
/// 通用的Transform存档组件
/// 将此脚本挂载到任何游戏对象上，即可保存和恢复其Transform信息
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
    }

    public string PrefabID => prefabID;

    /// <summary>
    /// 捕获当前Transform的状态
    /// </summary>
    /// <returns>包含Transform数据的对象</returns>
    public object CaptureState()
    {
        return new TransformSaveData
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale
        };
    }

    /// <summary>
    /// 恢复Transform的状态
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
    }

    [Button]
    public void SetID()
    {
        prefabID = gameObject.name;
    }
}