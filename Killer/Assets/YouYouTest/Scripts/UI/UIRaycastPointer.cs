using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class UIRaycastPointer : MonoBehaviour
{
    [Header("UI射线检测")]
    [Tooltip("Canvas上的GraphicRaycaster组件")]
    public GraphicRaycaster graphicRaycaster;

    [Header("XR射线检测")]
    [Tooltip("XR射线交互器，用于获取射线位置")]
    public NearFarInteractor nearFarInteractor;

    [Header("提示点")]
    [Tooltip("用于显示射线落点的小红点UI")]
    public RectTransform hitPointMarker;

    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private Canvas targetCanvas;

    void Start()
    {
        // 获取场景中的EventSystem
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("UIRaycastPointer: No EventSystem found in scene! Please add one.");
            return;
        }

        // 如果没有在Inspector中指定GraphicRaycaster，就尝试在Canvas上找
        if (graphicRaycaster == null)
        {
            graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        }

        if (graphicRaycaster == null)
        {
            Debug.LogError("UIRaycastPointer: No GraphicRaycaster found! Please add one to your Canvas.");
            return;
        }

        // 如果没有指定XR射线交互器，尝试在场景中找
        if (nearFarInteractor == null)
        {
            nearFarInteractor = FindObjectOfType<NearFarInteractor>();
        }

        if (nearFarInteractor == null)
        {
            Debug.LogWarning("UIRaycastPointer: No NearFarInteractor found. Will fall back to mouse input.");
        }

        // 初始化PointerEventData，这是射线检测所必需的
        pointerEventData = new PointerEventData(eventSystem);

        // 获取Canvas信息用于调试
        targetCanvas = graphicRaycaster.GetComponent<Canvas>();
        if (targetCanvas != null)
        {
            Debug.Log("UIRaycastPointer: Found Canvas with render mode: " + targetCanvas.renderMode);
        }

        // 初始时隐藏小红点
        if (hitPointMarker != null)
        {
            hitPointMarker.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 如果没有指定GraphicRaycaster或小红点，则不执行任何操作
        if (graphicRaycaster == null || hitPointMarker == null)
        {
            Debug.LogWarning("UIRaycastPointer: Missing graphicRaycaster or hitPointMarker");
            return;
        }

        // --- 核心逻辑 ---

        // 1. 设置射线检测的屏幕位置
        Vector2 screenPosition;
        
        if (nearFarInteractor != null)
        {
            // 使用NearFarInteractor的UI射线检测结果
            if (nearFarInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiRaycastResult))
            {
                Debug.Log("UIRaycastPointer: XR UI ray hit on UI element: " + uiRaycastResult.gameObject.name);
                
                // World Space Canvas：使用 worldPosition 直接放置到世界坐标上（hitPointMarker为该 Canvas 的子物体）
                if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace)
                {
                    if (hitPointMarker != null)
                    {
                        hitPointMarker.gameObject.SetActive(true);
                        // 使用射线结果的 worldPosition（NearFarInteractor 提供）
                        hitPointMarker.position = uiRaycastResult.worldPosition;
                        // 可选：让 marker 面向摄像机（如果需要朝向调整）
                        // hitPointMarker.rotation = Quaternion.LookRotation(-uiRaycastResult.worldNormal, targetCanvas.transform.up);
                        hitPointMarker.SetAsLastSibling();
                    }
                }
                else
                {
                    // Screen Space 模式：使用 screenPosition -> 转换为 Canvas 本地坐标
                    Vector2 screenPos = uiRaycastResult.screenPosition;
                    if (hitPointMarker != null)
                    {
                        hitPointMarker.gameObject.SetActive(true);
                        RectTransform canvasRectTransform = hitPointMarker.parent as RectTransform;
                        if (canvasRectTransform != null)
                        {
                            Camera cam = (targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? targetCanvas.worldCamera : null;
                            Vector2 localPoint;
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                canvasRectTransform,
                                screenPos,
                                cam,
                                out localPoint
                            );
                            hitPointMarker.anchoredPosition = localPoint;
                        }
                        else
                        {
                            hitPointMarker.position = screenPos;
                        }
                        hitPointMarker.SetAsLastSibling();
                    }
                }

                return; // 直接返回，不需要执行后续的GraphicRaycaster检测
            }
            else
            {
                // 如果没有UI射线检测结果，使用屏幕中心作为回退
                screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                Debug.Log("UIRaycastPointer: XR UI ray no hit, using fallback position: " + screenPosition);
            }
        }
        else
        {
            // 回退到鼠标位置
            screenPosition = Input.mousePosition;
            Debug.Log("UIRaycastPointer: Using mouse position: " + screenPosition);
        }
        
        pointerEventData.position = screenPosition;

        // 2. 创建一个列表来存储射线检测的结果
        List<RaycastResult> results = new List<RaycastResult>();

        // 3. 执行射线检测
        graphicRaycaster.Raycast(pointerEventData, results);

        // 调试信息：输出射线检测结果数量
        Debug.Log("UIRaycastPointer: Found " + results.Count + " UI elements at mouse position " + Input.mousePosition);
        
        // 如果没有检测到UI元素，尝试使用物理射线检测作为备选方案
        if (results.Count == 0)
        {
            // 尝试使用Physics.Raycast检测3D物体上的UI
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("UIRaycastPointer: Physics raycast hit: " + hit.collider.gameObject.name);
                
                // 将3D世界坐标转换为屏幕坐标
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hit.point);
                Debug.Log("UIRaycastPointer: Converted to screen position: " + screenPos);
                
                // 创建一个假的RaycastResult
                RaycastResult fakeResult = new RaycastResult();
                fakeResult.screenPosition = screenPos;
                fakeResult.gameObject = hit.collider.gameObject;
                results.Add(fakeResult);
            }
        }

        // 4. 处理检测结果
        if (results.Count > 0)
        {
            // results[0]是离屏幕最近（最顶层）的UI元素
            RaycastResult firstHit = results[0];

            // 调试信息：输出击中的UI元素名称
            Debug.Log("UIRaycastPointer: Hit UI element: " + firstHit.gameObject.name + " at screen position " + firstHit.screenPosition);

            // 激活小红点
            hitPointMarker.gameObject.SetActive(true);

            // 将小红点的位置设置到射线的落点处
            // 根据 Canvas 类型选择不同的坐标转换方式
            if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                // World Space Canvas：使用 worldPosition（NearFarInteractor 或 GraphicRaycaster 的 RaycastResult 可能包含 worldPosition）
                // 如果 firstHit 提供了 worldPosition，优先使用；否则尝试把 screenPosition 投影回世界（不常见）
                Vector3 worldPos = firstHit.worldPosition;
                if (worldPos != Vector3.zero)
                {
                    hitPointMarker.position = worldPos;
                }
                else
                {
                    // 作为保护：将屏幕点转换为世界点（需要摄像机）
                    Vector3 screenPoint = firstHit.screenPosition;
                    Camera cam = Camera.main;
                    Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, cam.nearClipPlane + 0.5f));
                    hitPointMarker.position = wp;
                }
            }
            else
            {
                // Screen Space（Overlay / Camera）：使用 ScreenPointToLocalPointInRectangle，传入合适的 camera
                RectTransform canvasRectTransform = hitPointMarker.parent as RectTransform;
                if (canvasRectTransform != null)
                {
                    Camera cam = (targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? targetCanvas.worldCamera : null;
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRectTransform,
                        firstHit.screenPosition,
                        cam,
                        out localPoint
                    );
                    hitPointMarker.anchoredPosition = localPoint;
                }
                else
                {
                    hitPointMarker.position = firstHit.screenPosition;
                }
            }
            
            // 为了确保小红点显示在所有其他UI之上，可以把它在Hierarchy中设置为最后一个子物体
            hitPointMarker.SetAsLastSibling();

            // (可选) 在Console中打印出击中的UI物体名称，方便调试
            // Debug.Log("Hit UI: " + firstHit.gameObject.name);
        }
        else
        {
            // 如果没有检测到任何UI元素，则隐藏小红点
            Debug.Log("UIRaycastPointer: No UI elements detected at mouse position");
            hitPointMarker.gameObject.SetActive(false);
        }
    }
}
