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
                original.single = outlinable.OutlineParameters.Color;
            else
            {
                original.front = outlinable.FrontParameters.Color;
                original.back = outlinable.BackParameters.Color;
            }

            original.stored = true;
            hasStored = true;
        }

        private void ApplyColor(Color color)
        {
            if (outlinable == null)
                return;

            if (outlinable.RenderStyle == RenderStyle.Single)
                outlinable.OutlineParameters.Color = color;
            else
            {
                outlinable.FrontParameters.Color = color;
                outlinable.BackParameters.Color = color;
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
        /// 恢复为原始颜色（如果曾缓存）。
        /// </summary>
        public void Restore()
        {
            if (!IsValid || !hasStored)
                return;

            if (outlinable.RenderStyle == RenderStyle.Single)
                outlinable.OutlineParameters.Color = original.single;
            else
            {
                outlinable.FrontParameters.Color = original.front;
                outlinable.BackParameters.Color = original.back;
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