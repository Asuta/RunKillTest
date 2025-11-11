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
    /// 本组件负责在 hover/selected 状态时显示描边，在 None 状态时隐藏描边。
    /// </summary>
    [DisallowMultipleComponent]
    public class OutlineReceiver : MonoBehaviour
    {
        private Outlinable outlinable;

        private OutlineState currentState = OutlineState.None;
        
        private void Awake()
        {
            outlinable = GetComponent<Outlinable>();
        }
        
        public bool IsValid => outlinable != null;
    
    
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
                    DisableOutline();
                    break;
                case OutlineState.Hover:
                    ApplyColor(color);
                    break;
                case OutlineState.Selected:
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
        /// 禁用描边：在非 hover/selected 状态下隐藏描边。
        /// </summary>
        public void DisableOutline()
        {
            if (!IsValid)
                return;
    
            if (outlinable.RenderStyle == RenderStyle.Single)
            {
                // 不在 hover 或 selected 时隐藏描边
                outlinable.OutlineParameters.Enabled = false;
            }
            else
            {
                // 不在 hover 或 selected 时隐藏描边
                outlinable.FrontParameters.Enabled = false;
                outlinable.BackParameters.Enabled = false;
            }
    
            currentState = OutlineState.None;
        }

        private void OnDisable()
        {
            // 确保禁用时隐藏描边
            DisableOutline();
        }
    }
}