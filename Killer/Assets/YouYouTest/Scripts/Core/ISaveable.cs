using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定义一个对象是否可以被保存和加载的接口
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 获取用于实例化的预制体ID
    /// </summary>
    string PrefabID { get; }

    /// <summary>
    /// 捕获当前状态，返回一个可序列化的对象
    /// </summary>
    /// <returns>包含需要保存的数据的对象</returns>
    object CaptureState();

    /// <summary>
    /// 从给定的数据对象中恢复状态
    /// </summary>
    /// <param name="state">从文件中加载的数据对象</param>
    void RestoreState(object state);
}