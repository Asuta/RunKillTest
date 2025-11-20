using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YouYouTest;

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
        // 直接调用SaveLoadManager的方法，使用按钮的Transform作为创建位置
        SaveLoadManager.Instance.LoadSelectedObjectsByFileName(JsonName, transform);
    }
}
