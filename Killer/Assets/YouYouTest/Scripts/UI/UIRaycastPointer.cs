using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.SceneManagement;

public class UIRaycastPointer : MonoBehaviour
{
    [Header("UI射线检测")]
    [Tooltip("Canvas上的GraphicRaycaster组件")]
    public GraphicRaycaster graphicRaycaster;

    [Header("XR射线检测")]
    [Tooltip("XR射线交互器，用于获取射线位置")]
    public NearFarInteractor nearFarInteractor;
    [Tooltip("目标交互器的物体名称关键词（例如 'Right' 或 'Left'）")]
    public string interactorNameKeyword = "Right";

    [Header("提示点")]
    [Tooltip("用于显示射线落点的小红点UI")]
    public RectTransform hitPointMarker;

    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private Canvas targetCanvas;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景切换时强制重置引用，确保重新寻找
        nearFarInteractor = null;
        graphicRaycaster = null;
        eventSystem = null;
        Initialize();
    }

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 1. 获取场景中的EventSystem
        if (eventSystem == null)
        {
            eventSystem = Object.FindFirstObjectByType<EventSystem>();
        }
        
        // 2. 获取 GraphicRaycaster
        if (graphicRaycaster == null)
        {
            graphicRaycaster = Object.FindFirstObjectByType<GraphicRaycaster>();
        }

        if (graphicRaycaster != null)
        {
            targetCanvas = graphicRaycaster.GetComponent<Canvas>();
            // WorldSpace 下自动修复相机
            if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace && targetCanvas.worldCamera == null)
            {
                targetCanvas.worldCamera = Camera.main;
            }
        }

        // 3. 核心修复：强制重新验证交互器
        ValidateInteractor();

        // 4. 初始化 PointerEventData
        if (eventSystem != null && pointerEventData == null)
        {
            pointerEventData = new PointerEventData(eventSystem);
        }

        // 5. 初始时隐藏小红点
        if (hitPointMarker != null && !hasHit)
        {
            hitPointMarker.gameObject.SetActive(false);
        }
    }

    private void ValidateInteractor()
    {
        // 如果当前交互器不匹配关键词，或者为空，则重新寻找
        if (nearFarInteractor == null || !nearFarInteractor.gameObject.name.Contains(interactorNameKeyword))
        {
            NearFarInteractor[] interactors = Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
            bool foundMatch = false;
            foreach (var inter in interactors)
            {
                if (inter.gameObject.name.Contains(interactorNameKeyword))
                {
                    nearFarInteractor = inter;
                    foundMatch = true;
                    break;
                }
            }
            
            if (!foundMatch && nearFarInteractor == null)
            {
                // 只有在完全找不到匹配项且当前为空时，才尝试保底
                nearFarInteractor = Object.FindFirstObjectByType<NearFarInteractor>();
            }
        }
    }

    private bool hasHit = false;

    void Update()
    {
        // 自动恢复引用
        if (graphicRaycaster == null || eventSystem == null || nearFarInteractor == null || !nearFarInteractor.gameObject.name.Contains(interactorNameKeyword))
        {
            Initialize();
            if (graphicRaycaster == null || hitPointMarker == null) return;
        }

        // 确保 WorldSpace Canvas 相机实时有效
        if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace && targetCanvas.worldCamera == null)
        {
            targetCanvas.worldCamera = Camera.main;
        }

        // --- 核心逻辑 ---

        bool hasHit = false;
        Vector3 hitWorldPos = Vector3.zero;
        Vector2 hitScreenPos = Vector2.zero;

        // 1. 优先使用指定的 XR 射线交互器
        if (nearFarInteractor != null)
        {
            if (nearFarInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiRaycastResult))
            {
                if (uiRaycastResult.gameObject != null)
                {
                    hasHit = true;
                    hitWorldPos = uiRaycastResult.worldPosition;
                    hitScreenPos = uiRaycastResult.screenPosition;
                }
            }
        }

        // 2. 如果 XR 没检测到，回退到鼠标/中心点
        if (!hasHit)
        {
            Vector2 screenPos = Input.mousePosition;
            if (screenPos == Vector2.zero) screenPos = new Vector2(Screen.width / 2, Screen.height / 2);
            
            if (pointerEventData == null && eventSystem != null) pointerEventData = new PointerEventData(eventSystem);
            
            if (pointerEventData != null)
            {
                pointerEventData.position = screenPos;
                List<RaycastResult> results = new List<RaycastResult>();
                graphicRaycaster.Raycast(pointerEventData, results);

                if (results.Count > 0)
                {
                    hasHit = true;
                    hitScreenPos = results[0].screenPosition;
                    hitWorldPos = results[0].worldPosition;
                }
            }
        }

        // 3. 处理显示
        if (hasHit)
        {
            hitPointMarker.gameObject.SetActive(true);

            if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                if (hitWorldPos == Vector3.zero)
                {
                    Camera cam = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
                    if (cam != null)
                    {
                        float dist = Vector3.ProjectOnPlane(targetCanvas.transform.position - cam.transform.position, targetCanvas.transform.forward).magnitude;
                        hitWorldPos = cam.ScreenToWorldPoint(new Vector3(hitScreenPos.x, hitScreenPos.y, dist));
                    }
                }
                if (hitWorldPos != Vector3.zero) hitPointMarker.position = hitWorldPos;
            }
            else
            {
                RectTransform canvasRectTransform = hitPointMarker.parent as RectTransform;
                if (canvasRectTransform != null)
                {
                    Camera cam = (targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? targetCanvas.worldCamera : null;
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, hitScreenPos, cam, out localPoint);
                    hitPointMarker.anchoredPosition = localPoint;
                }
            }
            hitPointMarker.SetAsLastSibling();
        }
        else
        {
            if (hitPointMarker.gameObject.activeSelf)
            {
                hitPointMarker.gameObject.SetActive(false);
            }
        }
    }
}
