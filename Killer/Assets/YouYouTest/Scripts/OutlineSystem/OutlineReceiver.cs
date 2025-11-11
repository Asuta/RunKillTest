using UnityEngine;
using EPOOutline;

namespace YouYouTest.OutlineSystem
{
    /// <summary>
    /// 被动接受射线并自行管理描边颜色的组件。
    /// CameraOutlineController 会调用 <see cref="OnHover"/>, <see cref="OnClick"/>, <see cref="Restore"/> 来切换描边颜色，
    /// 本组件负责缓存原始颜色并在恢复时还原。
    /// </summary>
    [DisallowMultipleComponent]
    public class OutlineReceiver : MonoBehaviour
    {
        private Outlinable outlinable;

        private struct OriginalColors
        {
            public Color single;
            public Color front;
            public Color back;
            // 存储原始的 Enabled 状态，便于还原
            public bool singleEnabled;
            public bool frontEnabled;
            public bool backEnabled;
            public bool stored;
        }
    
        private OriginalColors original;
        private bool hasStored;
    
        private void Awake()
        {
            outlinable = GetComponent<Outlinable>();
        }
    
        public bool IsValid => outlinable != null;
    
        private void StoreOriginalIfNeeded()
        {
            if (outlinable == null || hasStored)
                return;
    
            if (outlinable.RenderStyle == RenderStyle.Single)
            {
                original.single = outlinable.OutlineParameters.Color;
                original.singleEnabled = outlinable.OutlineParameters.Enabled;
            }
            else
            {
                original.front = outlinable.FrontParameters.Color;
                original.back = outlinable.BackParameters.Color;
                original.frontEnabled = outlinable.FrontParameters.Enabled;
                original.backEnabled = outlinable.BackParameters.Enabled;
            }
    
            original.stored = true;
            hasStored = true;
        }
    
        private void ApplyColor(Color color)
        {
            if (outlinable == null)
                return;
    
            // 显示描边并设置颜色（hover/click 时启用描边）
            if (outlinable.RenderStyle == RenderStyle.Single)
            {
                outlinable.OutlineParameters.Color = color;
                outlinable.OutlineParameters.Enabled = true;
            }
            else
            {
                outlinable.FrontParameters.Color = color;
                outlinable.BackParameters.Color = color;
                outlinable.FrontParameters.Enabled = true;
                outlinable.BackParameters.Enabled = true;
            }
        }

        /// <summary>
        /// 鼠标 Hover 时调用（被动响应）。
        /// </summary>
        public void OnHover(Color hoverColor)
        {
            if (!IsValid) return;
            StoreOriginalIfNeeded();
            ApplyColor(hoverColor);
        }

        /// <summary>
        /// 鼠标 Click 时调用（被动响应）。
        /// </summary>
        public void OnClick(Color clickColor)
        {
            if (!IsValid) return;
            StoreOriginalIfNeeded();
            ApplyColor(clickColor);
        }

        /// <summary>
        /// 恢复：还原颜色并在非 hover/selected 状态下隐藏描边。
        /// </summary>
        public void Restore()
        {
            if (!IsValid || !hasStored)
                return;
    
            if (outlinable.RenderStyle == RenderStyle.Single)
            {
                outlinable.OutlineParameters.Color = original.single;
                // 不在 hover 或 selected 时隐藏描边
                outlinable.OutlineParameters.Enabled = false;
            }
            else
            {
                outlinable.FrontParameters.Color = original.front;
                outlinable.BackParameters.Color = original.back;
                // 不在 hover 或 selected 时隐藏描边
                outlinable.FrontParameters.Enabled = false;
                outlinable.BackParameters.Enabled = false;
            }
    
            hasStored = false;
            original = default;
        }

        private void OnDisable()
        {
            // 确保禁用时还原
            Restore();
        }
    }
}