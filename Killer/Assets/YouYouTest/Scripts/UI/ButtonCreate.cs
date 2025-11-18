using UnityEngine;
using UnityEngine.UI;
using VInspector;
using UnityEngine.EventSystems;

public class ButtonCreate : MonoBehaviour, IPointerDownHandler
{
    private Button button;
    public GameObject createPrefab;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        
        // 如果找到了Button组件，移除原有的onClick事件监听
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
            Debug.Log("Button组件已找到并移除原有onClick事件监听");
        }
        else
        {
            Debug.LogWarning("未找到Button组件，请确保此GameObject上有Button组件");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 按钮点击事件处理方法（保留以防其他地方调用）
    void OnButtonClick()
    {
        Debug.Log("按钮被点击了！");
        // 在这里添加按钮点击后要执行的逻辑
        GlobalEvent.CreateButtonPoke.Invoke(createPrefab,transform);
    }
    
    // 实现IPointerDownHandler接口，在鼠标按下时立即触发
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("按钮被按下（鼠标还未抬起）！");
        // 在鼠标按下时立即执行逻辑
        GlobalEvent.CreateButtonPoke.Invoke(createPrefab,transform);
    }
    
    // 当对象被销毁时移除事件监听，防止内存泄漏
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
