using UnityEngine;
using UnityEngine.UI;
using VInspector;
using UnityEngine.EventSystems;
using VInspector;

public class ButtonGoTutorial : MonoBehaviour
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
        // 加载名为 "TutorialScene" 的场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("TutorialScene");
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
