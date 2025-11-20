using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 选中对象的保存数据结构
/// </summary>
[System.Serializable]
public class SelectedObjectsSaveData
{
    public string saveTime;
    public int objectCount;
    public Vector3 centerPosition; // 选中对象的中心位置
    public List<ObjectSaveData> objects;
}