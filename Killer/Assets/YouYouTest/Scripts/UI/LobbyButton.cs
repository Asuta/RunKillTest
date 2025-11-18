using UnityEngine;
using VInspector;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;


public class LobbyButton : MonoBehaviour
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
        // 获取GameManager中的当前存档槽
        // 如果已经在目标场景，直接设置状态
        if (SceneManager.GetActiveScene().name == "LobbyScene")
        {
            if (GameManager.Instance != null)
            {
                // 设置相反的状态：禁用模式切换，设置为PlayMode
                GameManager.Instance.SetCanSwitchMode(false);
                GameManager.Instance.SetPlayMode(true);
            }
            else
            {
                Debug.LogError("GameManager 实例不存在，无法设置游戏状态");
            }
            return;
        }

        // 在场景加载完成后再设置状态，使用局部回调并在调用后移除订阅
        UnityAction<Scene, LoadSceneMode> onLoaded = null;
        onLoaded = (scene, mode) =>
        {
            if (scene.name == "LobbyScene")
            {
                SceneManager.sceneLoaded -= onLoaded;

                if (GameManager.Instance != null)
                {
                    // 设置相反的状态：禁用模式切换，设置为PlayMode
                    GameManager.Instance.SetCanSwitchMode(false);
                    GameManager.Instance.SetPlayMode(true);
                }
                else
                {
                    Debug.LogWarning("场景已切换到 LobbyScene，但 GameManager 实例尚不可用。");
                }
            }
        };

        SceneManager.sceneLoaded += onLoaded;

        // 切换到 LobbyScene
        SceneManager.LoadScene("LobbyScene");

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
