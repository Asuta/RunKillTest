using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // 引入Linq以便于查找

public class TabController : MonoBehaviour
{
    [Header("颜色设置")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    // 这两个列表将由脚本在运行时自动填充
    public List<Button> tabButtons;
    public List<GameObject> pages;

    // tab切换音效
    public AudioClip tabSwitchSound;

    // 使用 Awake 以确保在其他脚本的 Start 之前完成初始化
    void Awake()
    {
        // 1. 自动查找子对象中的按钮和页面
        FindAndAssignUIElements();

        // 2. 验证是否找到了必要的元素
        if (tabButtons == null || pages == null || tabButtons.Count == 0 || pages.Count == 0)
        {
            Debug.LogError("TabController could not find required UI elements (Buttons/Pages) in its children. Make sure they are named correctly (e.g., 'TabButton_0', 'Page_0').");
            return;
        }

        // 3. 绑定所有按钮的点击事件
        for (int i = 0; i < tabButtons.Count; i++)
        {
            int index = i; // 闭包
            tabButtons[i].onClick.RemoveAllListeners(); // 清理旧监听器
            tabButtons[i].onClick.AddListener(() => OnTabClick(index));
        }
    }

    void Start()
    {
        // 4. 默认显示并选中第一个Tab
        if (tabButtons != null && tabButtons.Count > 0)
        {
            OnTabClick(0);
        }
    }

    private void FindAndAssignUIElements()
    {
        // 查找所有名为 "TabButtonContainer" 的子对象中的按钮
        Transform buttonContainer = transform.Find("TabButtonContainer");
        if (buttonContainer != null)
        {
            // 获取所有Button组件并按名字排序，以确保顺序正确
            tabButtons = buttonContainer.GetComponentsInChildren<Button>().OrderBy(b => b.name).ToList();
        }

        // 查找所有名为 "PageContainer" 的子对象中的页面
        Transform pageContainer = transform.Find("PageContainer");
        if (pageContainer != null)
        {
            // 获取所有直接子对象并按名字排序
            pages = new List<GameObject>();
            foreach (Transform child in pageContainer)
            {
                pages.Add(child.gameObject);
            }
            pages = pages.OrderBy(p => p.name).ToList();
        }
    }

    private int currentTabIndex = -1;

    /// <summary>
    /// 处理Tab按钮点击事件
    /// </summary>
    /// <param name="activeIndex">被激活的Tab索引</param>
    public void OnTabClick(int activeIndex)
    {
        if (pages == null || tabButtons == null) return;

        // 如果点击的是当前已选中的Tab，则不执行任何操作（或者根据需求决定是否重复播放音效）
        if (currentTabIndex == activeIndex) return;

        for (int i = 0; i < pages.Count; i++)
        {
            bool isActive = (i == activeIndex);
            if (pages.Count > i && pages[i] != null)
            {
                pages[i].SetActive(isActive);
            }

            if (tabButtons.Count > i && tabButtons[i] != null)
            {
                // 改变Tab按钮的颜色以示选中状态
                Image buttonImage = tabButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isActive ? activeTabColor : inactiveTabColor;
                }

                // 如果是新选中的Tab，在按钮位置播放音效
                if (isActive && tabSwitchSound != null)
                {
                    // 只有在不是初始化（Start中的调用）时才播放音效，或者如果用户希望初始化也播放，可以去掉 currentTabIndex != -1 的判断
                    if (currentTabIndex != -1)
                    {
                        AudioSource.PlayClipAtPoint(tabSwitchSound, tabButtons[i].transform.position);
                    }
                }
            }
        }

        currentTabIndex = activeIndex;
    }
}