using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class ButtonAngleSnap : MonoBehaviour
{
    private Button button;
    private Image buttonBackground;
    private GameManager gameManager;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color enabledColor = Color.green;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        buttonBackground = GetComponent<Image>();
        gameManager = GameManager.Instance;
        
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

        RefreshButtonBackgroundColor();
    }

    private void OnEnable()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (buttonBackground == null)
        {
            buttonBackground = GetComponent<Image>();
        }

        RefreshButtonBackgroundColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 按钮点击事件处理方法（保留以防其他地方调用）
    [Button]
    void OnButtonClick()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("未找到GameManager，无法切换角度贴合状态");
            return;
        }

        gameManager.EnableGrabRotationSnap = !gameManager.EnableGrabRotationSnap;
        RefreshButtonBackgroundColor();
        Debug.Log($"角度贴合已切换为: {(gameManager.EnableGrabRotationSnap ? "开启" : "关闭")}");
    }

    private void RefreshButtonBackgroundColor()
    {
        if (buttonBackground == null || gameManager == null)
        {
            return;
        }

        buttonBackground.color = gameManager.EnableGrabRotationSnap ? enabledColor : normalColor;
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
