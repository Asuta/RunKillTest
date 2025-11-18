using UnityEngine;
using System;
using VInspector;

/// <summary>
/// VRPlayer专用的保存组件
/// 处理VRPlayer的特殊保存和加载逻辑
/// </summary>
public class VRPlayerSavable : MonoBehaviour, ISaveable
{
    [Tooltip("用于实例化的预制体ID")]
    public string prefabID;
    
    [Serializable]
    public struct VRPlayerSaveData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public string PrefabID => prefabID;
    public System.Type DataType => typeof(VRPlayerSaveData);

    /// <summary>
    /// 捕获VRPlayer的状态
    /// </summary>
    /// <returns>包含VRPlayer数据的对象</returns>
    public object CaptureState()
    {
        return new VRPlayerSaveData
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale
        };
    }

    /// <summary>
    /// 恢复VRPlayer的状态
    /// </summary>
    /// <param name="state">从文件中加载的数据对象</param>
    public void RestoreState(object state)
    {
        if (!(state is VRPlayerSaveData))
        {
            Debug.LogWarning("VRPlayerSavable: 尝试恢复状态时，数据类型不匹配！");
            return;
        }

        VRPlayerSaveData data = (VRPlayerSaveData)state;
        
        // 获取Rigidbody组件
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 使用Rigidbody来设置位置和旋转，确保物理系统正确更新
            rb.MovePosition(data.position);
            rb.MoveRotation(data.rotation);
            
            // 重置速度，避免加载后出现意外的移动
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            // 如果没有Rigidbody，直接设置Transform
            transform.position = data.position;
            transform.rotation = data.rotation;
        }
        
        transform.localScale = data.scale;

        Debug.Log($"VRPlayerSavable: '{gameObject.name}' 的状态已恢复。位置: {data.position}");
    }

    [Button]
    public void SetID()
    {
        prefabID = gameObject.name;
    }
}