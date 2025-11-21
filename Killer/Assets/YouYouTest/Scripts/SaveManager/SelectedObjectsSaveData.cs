using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 选中对象的保存数据结构
/// </summary>
[System.Serializable]
public class SelectedObjectsSaveData
{
    public string saveTime;
    public string saveName; // 存档名字（与JSON文件名不同）
    public int objectCount;
    public Vector3 centerPosition; // 选中对象的中心位置
    public List<ObjectSaveData> objects;
}

/// <summary>
/// 选中对象存档信息
/// </summary>
[System.Serializable]
public class SelectedObjectsSaveInfo
{
    public string fileName; // 文件名（如：SelectedObjects_20251120_143043.json）
    public string saveName; // 存档名字（用户自定义的名称）
    public string saveTime; // 保存时间
    public int objectCount; // 对象数量
    public long fileSize; // 文件大小（字节）
}