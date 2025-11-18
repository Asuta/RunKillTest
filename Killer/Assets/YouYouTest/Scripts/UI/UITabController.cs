using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI标签页逻辑控制器
/// 自动查找Canvas中的按钮和内容面板，并添加切换逻辑
/// </summary>
public class UITabController : MonoBehaviour
{
    [Header("颜色设置")]
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = Color.gray;

    [Header("引用设置")]
    // 允许外部脚本（如 UICreate）在运行时赋值
    // public UICreate uiCreate;

    private Canvas canvas;
    private Button[] tabButtons;
    private GameObject[] tabContents;
    private int currentTabIndex = 0;

    void Start()
    {
        // // 如果没有手动分配UICreate，尝试在同一GameObject上查找
        // if (uiCreate == null)
        // {
        //     uiCreate = GetComponent<UICreate>();
        // }

        // if (uiCreate == null)
        // {
        //     Debug.LogError("UITabController: 未找到UICreate组件！");
        //     return;
        // }

        // 等待一帧确保UICreate已经创建了UI
        Invoke(nameof(InitializeLogic), 0.1f);
    }

    /// <summary>
    /// 初始化逻辑：查找所有UI元素并添加事件
    /// </summary>
    void InitializeLogic()
    {
        // 获取Canvas引用
        canvas = this.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("UITabController: Canvas引用为空！");
            return;
        }

        // 查找所有标签按钮
        FindTabButtons();

        // 查找所有标签内容面板
        FindTabContents();

        // 为按钮添加点击事件
        AttachButtonEvents();

        // 初始化第一个标签为激活状态
        SwitchTab(0);

        Debug.Log($"UITabController: 成功初始化，找到 {tabButtons.Length} 个按钮和 {tabContents.Length} 个内容面板");
    }

    /// <summary>
    /// 查找所有标签按钮
    /// </summary>
    void FindTabButtons()
    {
        // 在Canvas中查找TabButtonPanel
        Transform buttonPanel = canvas.transform.Find("BackgroundPanel/TabButtonPanel");
        if (buttonPanel == null)
        {
            Debug.LogError("UITabController: 未找到TabButtonPanel！");
            return;
        }

        // 获取所有子对象中的Button组件
        tabButtons = buttonPanel.GetComponentsInChildren<Button>();
        Debug.Log($"找到 {tabButtons.Length} 个标签按钮");
    }

    /// <summary>
    /// 查找所有标签内容面板
    /// </summary>
    void FindTabContents()
    {
        // 在Canvas中查找TabContentPanel
        Transform contentPanel = canvas.transform.Find("BackgroundPanel/TabContentPanel");
        if (contentPanel == null)
        {
            Debug.LogError("UITabController: 未找到TabContentPanel！");
            return;
        }

        // 获取所有直接子对象（每个都是一个标签内容）
        int childCount = contentPanel.childCount;
        tabContents = new GameObject[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            tabContents[i] = contentPanel.GetChild(i).gameObject;
        }

        Debug.Log($"找到 {tabContents.Length} 个内容面板");
    }

    /// <summary>
    /// 为所有按钮附加点击事件
    /// </summary>
    void AttachButtonEvents()
    {
        if (tabButtons == null || tabButtons.Length == 0)
        {
            Debug.LogError("UITabController: 没有找到按钮，无法添加事件！");
            return;
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i; // 捕获索引用于闭包
            Button button = tabButtons[i];
            
            // 清除现有的所有监听器
            button.onClick.RemoveAllListeners();
            
            // 添加新的点击事件
            button.onClick.AddListener(() => OnTabButtonClick(index));
            
            Debug.Log($"为按钮 {i} 添加了点击事件");
        }
    }

    /// <summary>
    /// 标签按钮点击事件处理
    /// </summary>
    void OnTabButtonClick(int tabIndex)
    {
        Debug.Log($"点击了标签按钮: {tabIndex}");
        SwitchTab(tabIndex);
    }

    /// <summary>
    /// 切换标签页
    /// </summary>
    void SwitchTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabButtons.Length || tabIndex >= tabContents.Length)
        {
            Debug.LogWarning($"UITabController: 无效的标签索引 {tabIndex}");
            return;
        }

        // 隐藏当前标签内容并重置按钮颜色
        if (currentTabIndex >= 0 && currentTabIndex < tabContents.Length)
        {
            tabContents[currentTabIndex].SetActive(false);
            
            if (currentTabIndex < tabButtons.Length)
            {
                Image buttonImage = tabButtons[currentTabIndex].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = inactiveTabColor;
                }
            }
        }

        // 显示新标签内容并设置按钮颜色
        currentTabIndex = tabIndex;
        tabContents[currentTabIndex].SetActive(true);
        
        Image activeButtonImage = tabButtons[currentTabIndex].GetComponent<Image>();
        if (activeButtonImage != null)
        {
            activeButtonImage.color = activeTabColor;
        }

        Debug.Log($"切换到标签 {tabIndex}");
    }

    /// <summary>
    /// 公开方法：通过代码切换到指定标签
    /// </summary>
    public void SwitchToTab(int tabIndex)
    {
        SwitchTab(tabIndex);
    }

    /// <summary>
    /// 获取当前激活的标签索引
    /// </summary>
    public int GetCurrentTabIndex()
    {
        return currentTabIndex;
    }
}
