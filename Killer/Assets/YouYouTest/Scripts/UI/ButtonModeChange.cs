using UnityEngine;
using UnityEngine.UI;
using VInspector;
using UnityEngine.EventSystems;
using VInspector;

public class ButtonModeChange : MonoBehaviour
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
    [Button]
    void OnButtonClick()
    {
        // 获取GameManager实例并切换模式
        if (GameManager.Instance != null)
        {
            // 切换到相反的模式
            GameManager.Instance.SetPlayMode(!GameManager.Instance.IsPlayMode);
            Debug.Log($"按钮点击切换模式: {(GameManager.Instance.IsPlayMode ? "游戏模式" : "编辑模式")}");
        }
        else
        {
            Debug.LogWarning("GameManager实例未找到，无法切换模式");
        }
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
