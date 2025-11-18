using UnityEngine;
using VInspector;
using UnityEngine.SceneManagement;
using UnityEngine.Events;


public class ManyButtonTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 监听 GlobalEvent.OnSelect 事件
        GlobalEvent.OnSelect.AddListener(OnSelectHandler);
    }

    void OnDestroy()
    {
        // 移除事件监听，避免内存泄漏
        GlobalEvent.OnSelect.RemoveListener(OnSelectHandler);
    }

    // 处理 OnSelect 事件的方法
    void OnSelectHandler()
    {
        Debug.LogError("多选完成");
    }

    // Update is called once per frame
    void Update()
    {

    }

    
    [Button(" select")]
    void ButtonSelect()
    {
        GlobalEvent.OnSelect.Invoke();
    }

    [Button(" 模式切换")]
    void Button1()
    {
        GlobalEvent.ModeButtonPoke.Invoke();
    }

    [Button]
    void Button2()
    {
        Debug.Log("Button2");
    }

    [Button("enter play scene and load target json")]
    void Button3(string jsonName = "slot One")
    {
        // 将要加载的存档槽名写入 GameManager，SceneLoadManager 或者后续的加载回调会读取它
        if (GameManager.Instance != null)
        {
            GameManager.Instance.nowLoadSaveSlot = jsonName;
        }
        else
        {
            Debug.LogError("GameManager 实例不存在，无法设置存档槽名");
            return;
        }

        // 如果已经在目标场景，直接触发加载
        if (SceneManager.GetActiveScene().name == "KillScene")
        {
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadSceneObjects(jsonName);
                // 修复：使用公共方法而不是直接赋值
                GameManager.Instance.SetCanSwitchMode(true);
            }
            else
            {
                Debug.LogWarning("SaveLoadManager 实例不存在，无法立即加载存档，已设置 nowLoadSaveSlot 等待初始化。");
            }
            return;
        }

        // 在场景加载完成后再调用加载存档，使用局部回调并在调用后移除订阅
        UnityAction<Scene, LoadSceneMode> onLoaded = null;
        onLoaded = (scene, mode) =>
        {
            if (scene.name == "KillScene")
            {
                SceneManager.sceneLoaded -= onLoaded;

                if (SaveLoadManager.Instance != null && GameManager.Instance != null)
                {
                    SaveLoadManager.Instance.LoadSceneObjects(jsonName);
                    // 修复：使用公共方法而不是直接赋值
                    GameManager.Instance.SetCanSwitchMode(true);
                    // 设置为非PlayMode（编辑模式）
                    GameManager.Instance.SetPlayMode(false);
                }
                else
                {
                    Debug.LogWarning("场景已切换到 KillScene，但 SaveLoadManager 或 GameManager 实例尚不可用。");
                }
            }
        };

        SceneManager.sceneLoaded += onLoaded;

        // 切换到 KillScene
        SceneManager.LoadScene("KillScene");
    }


    [Button("enter lobby scene and load target json")]
    void Button4(string jsonName = "slot One")
    {
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

    public void OnHoverEnter()
    {
        Debug.Log("鼠标进入");
    }

    public void OnHoverExit()
    {
        Debug.Log("鼠标离开");
    }
}
