// OutlineController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 如果使用QuickOutline插件，需要添加这个 using
// using cakeslice;

// 确保该组件依赖 Interactable 组件
[RequireComponent(typeof(Interactable))] 
public class OutlineController : MonoBehaviour
{
    // 在 Inspector 中配置不同状态对应的描边效果
    public List<OutlineProfile> outlineProfiles = new List<OutlineProfile>();

    // 缓存组件引用
    private Interactable _interactable;
    // 假设这是你的描边组件的引用
    private cakeslice.Outline _outlineEffect; // 以 QuickOutline 为例

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        
        // 获取或添加描边组件实例
        _outlineEffect = GetComponent<cakeslice.Outline>();
        if (_outlineEffect == null)
        {
            _outlineEffect = gameObject.AddComponent<cakeslice.Outline>();
        }

        // 初始时禁用描边
        _outlineEffect.enabled = false;
    }

    private void OnEnable()
    {
        // 订阅状态变更事件
        _interactable.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        _interactable.OnStateChanged -= HandleStateChanged;
    }

    // 事件处理函数
    private void HandleStateChanged(InteractableState newState)
    {
        ApplyOutlineForState(newState);
    }

    // 根据状态应用描边效果
    private void ApplyOutlineForState(InteractableState state)
    {
        // 查找与当前状态匹配的配置
        var profile = outlineProfiles.FirstOrDefault(p => p.State == state);

        if (state == InteractableState.None || profile == null)
        {
            // 如果是 None 状态或没有找到对应配置，则关闭描边
            _outlineEffect.enabled = false;
        }
        else
        {
            // 找到配置，应用效果
            _outlineEffect.enabled = true;
            _outlineEffect.color = profile.Color; // QuickOutline 用 0/1/2 代表颜色
            _outlineEffect.OutlineColor = profile.Color; // 如果是其他插件，API可能不同
            _outlineEffect.OutlineWidth = profile.Thickness; // 同上
            
            // 提醒：具体的属性设置取决于你用的描边插件
            // 例如，QuickOutline 使用 _outlineEffect.color = 0, 1, 2...
            // 这里为了通用性，直接用了Color，你需要根据实际情况修改
        }
    }
}
