using UnityEngine;
using UnityEngine.UI;

public class SelectLoadButtonUI : MonoBehaviour
{
    public string ButtonName = "SelectLoadButton";
    public string JsonName = "SelectLoadJson";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取Button组件
        Button button = GetComponent<Button>();
        
        // 如果Button组件存在，绑定点击事件
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogWarning("未找到Button组件！");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 按钮点击事件处理方法
    void OnButtonClick()
    {
        
    }
}
