using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class ButtonPositionSnap : MonoBehaviour
{
    private Button button;
    private Image buttonBackground;
    private GameManager gameManager;

    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color enabledColor = Color.green;

    void Start()
    {
        button = GetComponent<Button>();
        buttonBackground = GetComponent<Image>();
        gameManager = GameManager.Instance;

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
            Debug.Log("Position Snap 按钮已绑定点击事件");
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

    [Button]
    void OnButtonClick()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("未找到GameManager，无法切换位置贴合状态");
            return;
        }

        gameManager.EnableGrabPositionSnap = !gameManager.EnableGrabPositionSnap;
        RefreshButtonBackgroundColor();
        Debug.Log($"位置贴合已切换为: {(gameManager.EnableGrabPositionSnap ? "开启" : "关闭")}");
    }

    private void RefreshButtonBackgroundColor()
    {
        if (buttonBackground == null || gameManager == null)
        {
            return;
        }

        buttonBackground.color = gameManager.EnableGrabPositionSnap ? enabledColor : normalColor;
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
