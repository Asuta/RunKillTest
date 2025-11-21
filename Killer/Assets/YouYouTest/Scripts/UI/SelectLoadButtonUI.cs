using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YouYouTest;
using YouYouTest.OutlineSystem;
using System.Collections.Generic;

public class SelectLoadButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    public string ButtonName = "SelectLoadButton";
    public string JsonName = "SelectLoadJson";
    public TMPro.TextMeshProUGUI buttonText;
    
    private Button buttonComponent;
    private int lastPointerId = -1;
    private Vector2 lastPointerPosition;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取Button组件，用于控制交互状态
        buttonComponent = GetComponent<Button>();
        buttonText.text = ButtonName;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 吸收事件并阻断拖拽：在按下时立刻 Use() 事件并阻止拖拽流程
    public void OnPointerDown(PointerEventData eventData)
    {
        // 记录指针信息，用于后续模拟 pointerUp/reset 可视状态
        lastPointerId = eventData.pointerId;
        lastPointerPosition = eventData.position;

        // 标记事件已被处理，阻止后续的拖拽/长按触发（例如 ScrollRect）
        eventData.Use();

        // 取消系统选中，双重保险
        var es = EventSystem.current;
        if (es != null)
        {
            es.SetSelectedGameObject(null);
        }

        // 视觉上禁用交互，阻止持续按住
        if (buttonComponent != null)
        {
            buttonComponent.interactable = false;
        }

        // 执行加载逻辑
        DoLoad();

        // 在下一帧重新启用按钮交互并恢复可视状态
        StartCoroutine(ReEnableButton());
    }

    // 当父级（如 ScrollRect）准备开始拖拽时也强行吸收，防止其启动拖拽流程
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // 吸收并清理可能导致拖拽的问题字段
        eventData.Use();
        eventData.pointerDrag = null;
        eventData.pointerPress = null;
        eventData.rawPointerPress = null;

        // 并确保 EventSystem 不选择任何对象
        var es = EventSystem.current;
        if (es != null)
        {
            es.SetSelectedGameObject(null);
        }
    }

    // 确保抬起、拖拽开始/过程/结束都被吸收，避免其他 UI 响应
    public void OnPointerUp(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    // 将原有加载逻辑抽成方法，方便复用
    private void DoLoad()
    {
        Debug.Log($"{ButtonName} 触发加载存档: {JsonName}");
        List<GameObject> loadedObjects = SaveLoadManager.Instance.LoadSelectedObjectsByFileName(JsonName, transform);

        if (loadedObjects != null && loadedObjects.Count > 0)
        {
            GlobalEvent.OnLoadObjectsSetSelected.Invoke(loadedObjects);
            Debug.Log($"已通过全局事件发送 {loadedObjects.Count} 个对象的选中请求");
        }
        else
        {
            Debug.LogWarning("没有加载的对象，无需设置选中状态");
        }
    }
    
    private System.Collections.IEnumerator ReEnableButton()
    {
        // 等待一帧
        yield return null;
        
        // 重新启用按钮交互
        if (buttonComponent != null)
        {
            buttonComponent.interactable = true;

            // 使用之前记录的指针信息来模拟 pointerUp / pointerExit，恢复按钮可视状态
            var es = EventSystem.current;
            if (es != null && lastPointerId != -1)
            {
                var upEvent = new PointerEventData(es)
                {
                    pointerId = lastPointerId,
                    position = lastPointerPosition
                };

                ExecuteEvents.Execute(buttonComponent.gameObject, upEvent, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(buttonComponent.gameObject, upEvent, ExecuteEvents.pointerExitHandler);

                // 清理选择
                es.SetSelectedGameObject(null);
            }

            // 重置记录
            lastPointerId = -1;
        }
    }
}
