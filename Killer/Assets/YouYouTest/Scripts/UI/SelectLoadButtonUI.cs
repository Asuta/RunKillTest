using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YouYouTest;
using YouYouTest.OutlineSystem;
using System.Collections.Generic;

public class SelectLoadButtonUI : MonoBehaviour, IPointerDownHandler
{
    public string ButtonName = "SelectLoadButton";
    public string JsonName = "SelectLoadJson";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 不再需要Button组件的onClick事件，因为我们使用IPointerDownHandler
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 实现IPointerDownHandler接口，处理按下事件
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"{ButtonName} 被按下，开始加载存档: {JsonName}");
        // 调用SaveLoadManager的方法，使用按钮的Transform作为创建位置
        List<GameObject> loadedObjects = SaveLoadManager.Instance.LoadSelectedObjectsByFileName(JsonName, transform);
        
        // 通过全局事件将加载的对象设置为选中状态
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
}
