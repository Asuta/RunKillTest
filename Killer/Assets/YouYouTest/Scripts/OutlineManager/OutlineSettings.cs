// OutlineSettings.cs
using UnityEngine;
using System;

// 1. 定义所有可能的交互状态
public enum InteractableState
{
    None,    // 默认状态，无交互
    Hover,   // 鼠标悬停/准星对准
    Select,  // 单击选中
    Hold,    // 按住不放
    // 可以继续扩展, e.g., Targeted
}

// 2. 创建一个可序列化的类来配置每个状态对应的描边效果
// [Serializable] 让我们可以在 Inspector 面板中编辑它
[Serializable]
public class OutlineProfile
{
    public InteractableState State;
    public Color Color = Color.white;
    [Range(0, 10)]
    public float Thickness = 2.0f;
    // 你还可以添加更多属性，比如描边模式（硬边、模糊边等）
}
