using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace YouYouTest.Interaction
{
    /// <summary>
    /// 检测射线与整体UI的进入和退出状态
    /// 当射线从非UI区域进入任何UI元素时触发进入事件
    /// 当射线从所有UI区域离开到非UI区域时触发退出事件
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/Interaction/UI Hover Detector")]
    public class UIHoverDetector : MonoBehaviour
    {
        [System.Serializable]
        public class UIHoverEvent : UnityEvent { }

        [System.Serializable]
        public class UIHoverInfoEvent : UnityEvent<UIHoverInfo> { }

        [System.Serializable]
        public class UIHoverInfo
        {
            public GameObject currentUIElement;
            public Vector3 hitPosition;
            public Vector3 hitNormal;
            public bool isUIHit;
        }

        [Header("UI整体悬停事件")]
        [Tooltip("当射线从非UI区域进入任何UI元素时触发")]
        public UIHoverEvent onUIEnter = new UIHoverEvent();

        [Tooltip("当射线从所有UI区域离开到非UI区域时触发")]
        public UIHoverEvent onUIExit = new UIHoverEvent();

        [Tooltip("当UI状态发生变化时触发，包含详细信息")]
        public UIHoverInfoEvent onUIStateChanged = new UIHoverInfoEvent();

        [Header("调试信息")]
        [SerializeField] private bool m_IsHoveringUI = false;
        [SerializeField] private GameObject m_CurrentUIElement;
        [SerializeField] private Vector3 m_LastHitPosition;
        [SerializeField] private Vector3 m_LastHitNormal;

        private NearFarInteractor m_NearFarInteractor;
        private UIHoverInfo m_HoverInfo = new UIHoverInfo();

        /// <summary>
        /// 当前是否正在悬停UI
        /// </summary>
        public bool isHoveringUI => m_IsHoveringUI;

        /// <summary>
        /// 当前悬停的UI元素
        /// </summary>
        public GameObject currentUIElement => m_CurrentUIElement;

        void Awake()
        {
            m_NearFarInteractor = GetComponent<NearFarInteractor>();
            if (m_NearFarInteractor == null)
            {
                Debug.LogError("UIHoverDetector: NearFarInteractor component not found on the same GameObject!", this);
                enabled = false;
                return;
            }
        }

        void Update()
        {
            CheckUIState();
        }

        /// <summary>
        /// 检查UI状态变化
        /// </summary>
        private void CheckUIState()
        {
            if (m_NearFarInteractor == null) return;

            bool isCurrentlyHittingUI = m_NearFarInteractor.TryGetCurrentUIRaycastResult(out RaycastResult raycastResult);
            GameObject currentUIElement = isCurrentlyHittingUI ? raycastResult.gameObject : null;
            Vector3 hitPosition = isCurrentlyHittingUI ? raycastResult.worldPosition : Vector3.zero;
            Vector3 hitNormal = isCurrentlyHittingUI ? raycastResult.worldNormal : Vector3.zero;

            // 检查状态是否发生变化
            bool stateChanged = false;
            
            // UI悬停状态变化
            if (isCurrentlyHittingUI != m_IsHoveringUI)
            {
                m_IsHoveringUI = isCurrentlyHittingUI;
                stateChanged = true;

                // 触发全局事件，通知射线命中UI状态变化
                GlobalEvent.RaycastHittingUIChange.Invoke(m_IsHoveringUI);

                if (m_IsHoveringUI)
                {
                    Debug.Log($"UIHoverDetector: 进入UI区域 - {currentUIElement?.name}");
                    onUIEnter?.Invoke();
                }
                else
                {
                    Debug.Log("UIHoverDetector: 离开UI区域");
                    onUIExit?.Invoke();
                }
            }

            // UI元素变化（在UI区域内移动到不同元素）
            if (currentUIElement != m_CurrentUIElement)
            {
                m_CurrentUIElement = currentUIElement;
                stateChanged = true;
            }

            // 位置或法线变化
            if (hitPosition != m_LastHitPosition || hitNormal != m_LastHitNormal)
            {
                m_LastHitPosition = hitPosition;
                m_LastHitNormal = hitNormal;
                stateChanged = true;
            }

            // 如果状态发生变化，触发详细信息事件
            if (stateChanged)
            {
                m_HoverInfo.currentUIElement = m_CurrentUIElement;
                m_HoverInfo.hitPosition = m_LastHitPosition;
                m_HoverInfo.hitNormal = m_LastHitNormal;
                m_HoverInfo.isUIHit = m_IsHoveringUI;

                onUIStateChanged?.Invoke(m_HoverInfo);
            }
        }

        /// <summary>
        /// 手动检查当前UI状态
        /// </summary>
        /// <returns>如果当前射中UI返回true</returns>
        public bool IsCurrentlyHittingUI()
        {
            if (m_NearFarInteractor == null) return false;
            return m_NearFarInteractor.TryGetCurrentUIRaycastResult(out _);
        }

        /// <summary>
        /// 获取当前UI射线检测结果
        /// </summary>
        /// <param name="raycastResult">射线检测结果</param>
        /// <returns>如果当前射中UI返回true</returns>
        public bool TryGetCurrentUIRaycastResult(out RaycastResult raycastResult)
        {
            if (m_NearFarInteractor == null)
            {
                raycastResult = default;
                return false;
            }
            return m_NearFarInteractor.TryGetCurrentUIRaycastResult(out raycastResult);
        }

        /// <summary>
        /// 获取当前悬停信息
        /// </summary>
        /// <returns>悬停信息</returns>
        public UIHoverInfo GetCurrentHoverInfo()
        {
            return new UIHoverInfo
            {
                currentUIElement = m_CurrentUIElement,
                hitPosition = m_LastHitPosition,
                hitNormal = m_LastHitNormal,
                isUIHit = m_IsHoveringUI
            };
        }

        void OnValidate()
        {
            // 在Inspector中显示当前状态
            if (Application.isPlaying && m_NearFarInteractor != null)
            {
                m_IsHoveringUI = IsCurrentlyHittingUI();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && m_IsHoveringUI && m_CurrentUIElement != null)
            {
                // 在UI元素位置绘制一个指示器
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_LastHitPosition, 0.05f);
                
                // 绘制法线方向
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(m_LastHitPosition, m_LastHitNormal * 0.1f);
            }
        }
    }
}