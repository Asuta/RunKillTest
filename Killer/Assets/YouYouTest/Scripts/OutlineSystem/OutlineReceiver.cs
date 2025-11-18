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
        
        [Header("描边颜色配置")]
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color selectedColor = Color.cyan;
        
        private void Awake()
        {
            outlinable = GetComponent<Outlinable>();
        }
        
        public bool IsValid => outlinable != null;
 
        /// <summary>
        /// 当前是否处于 Selected 状态（供外部控制器查询）。
        /// </summary>
        public bool IsSelected => currentState == OutlineState.Selected;
    
    
        private void ApplyColor(Color color)
        {
            if (outlinable == null)
                return;
    
            // 启用整个 Outlinable 脚本并设置颜色（hover/click 时启用描边）
            outlinable.enabled = true;
            
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
        /// 颜色由 Receiver 内部根据状态自行决定，外部不再传入颜色。
        /// </summary>
        public void SetState(OutlineState newState)
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
                    ApplyColor(hoverColor);
                    break;
                case OutlineState.Selected:
                    ApplyColor(selectedColor);
                    break;
            }
    
            currentState = newState;
        }
    
        /// <summary>
        /// 兼容旧接口：鼠标 Hover 时调用（被动响应）。
        /// </summary>
        public void OnHover() => SetState(OutlineState.Hover);
    
        /// <summary>
        /// 兼容旧接口：鼠标 Click 时调用（被动响应）。
        /// 现在由 Receiver 自行决定是否切换状态：如果当前已 Selected 则取消，否则设置为 Selected。
        /// </summary>
        public void OnClick()
        {
            if (!IsValid) return;
            if (currentState == OutlineState.Selected)
            {
                SetState(OutlineState.None);
            }
            else
            {
                SetState(OutlineState.Selected);
            }
        }
 
        /// <summary>
        /// 清除 Hover 状态（仅在当前状态为 Hover 时生效），Selected 状态不会被此方法取消。
        /// 由外部控制器调用以通知 Receiver 取消 hover 而保持 Selected。
        /// </summary>
        public void ClearHover()
        {
            if (!IsValid) return;
            if (currentState == OutlineState.Hover)
            {
                SetState(OutlineState.None);
            }
        }

        /// <summary>
        /// 禁用描边：在非 hover/selected 状态下隐藏描边。
        /// </summary>
        public void DisableOutline()
        {
            if (!IsValid)
                return;
    
            // 禁用整个 Outlinable 脚本来隐藏描边
            outlinable.enabled = false;
    
            currentState = OutlineState.None;
        }

        private void OnDisable()
        {
            // 确保禁用时隐藏描边
            DisableOutline();
        }
    }
}