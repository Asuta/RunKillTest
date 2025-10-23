using UnityEngine;
using UnityEngine.UI;
using VInspector;
using UnityEngine.EventSystems;

public class LoadButton : MonoBehaviour
{
    private Button button;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        
        // 如果找到了Button组件，添加onClick事件监听
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
            Debug.Log("Button组件已找到并添加onClick事件监听");
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
        Debug.Log("加载按钮被点击了！");
        // 调用SaveTestt单例的加载方法
        SaveTestt.Instance.LoadSceneObjects();
    }
    
    // // 实现IPointerDownHandler接口，在鼠标按下时立即触发
    // public void OnPointerDown(PointerEventData eventData)
    // {
    //     Debug.Log("按钮被按下（鼠标还未抬起）！");
    //     // 在鼠标按下时立即执行逻辑
    //     GlobalEvent.CreateButtonPoke.Invoke(createPrefab,transform);
    // }
    
    // 当对象被销毁时移除事件监听，防止内存泄漏
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
