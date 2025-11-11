using UnityEngine;
using EPOOutline;

namespace YouYouTest.OutlineSystem
{
    public enum OutlineState
    {
        None = 0,
        Hover = 1,
        Selected = 2
    }

    /// <summary>
    /// 被动接受射线并自行管理描边颜色的组件。
    /// CameraOutlineController 会调用 <see cref="SetState"/> 来切换状态（Hover/Selected/None），
    /// 本组件负责缓存原始颜色并在恢复时还原或隐藏描边。
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
        private OutlineState currentState = OutlineState.None;
        
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
        /// 通过状态接口设置当前状态（None/Hover/Selected）。
        /// color 在 Hover/Selected 状态时用于设置描边颜色，None 时被忽略。
        /// </summary>
        public void SetState(OutlineState newState, Color color)
        {
            if (!IsValid) return;
            if (currentState == newState) return;
            // Selected 优先，不要在 Selected 时降级为 Hover
            if (currentState == OutlineState.Selected && newState == OutlineState.Hover)
                return;
    
            switch (newState)
            {
                case OutlineState.None:
                    Restore();
                    break;
                case OutlineState.Hover:
                    StoreOriginalIfNeeded();
                    ApplyColor(color);
                    break;
                case OutlineState.Selected:
                    StoreOriginalIfNeeded();
                    ApplyColor(color);
                    break;
            }
    
            currentState = newState;
        }
    
        /// <summary>
        /// 兼容旧接口：鼠标 Hover 时调用（被动响应）。
        /// </summary>
        public void OnHover(Color hoverColor) => SetState(OutlineState.Hover, hoverColor);
    
        /// <summary>
        /// 兼容旧接口：鼠标 Click 时调用（被动响应）。
        /// </summary>
        public void OnClick(Color clickColor) => SetState(OutlineState.Selected, clickColor);

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
            currentState = OutlineState.None;
        }

        private void OnDisable()
        {
            // 确保禁用时还原
            Restore();
        }
    }
}