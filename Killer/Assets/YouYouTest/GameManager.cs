using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例实例
    private static GameManager _instance;

    // 公开的实例访问器
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();

                if (_instance == null)
                {
                    GameObject gameManagerObject = new GameObject("GameManager");
                    _instance = gameManagerObject.AddComponent<GameManager>();
                }
            }

            return _instance;
        }
    }

    // 确保只有一个实例存在
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }


    [SerializeField]
    private Transform _playerCameraT;
    public Transform PlayerCameraT
    {
        get
        {
            if (_playerCameraT == null)
            {
                Debug.LogError("Player camera is not assigned! Make sure PlayerSetting component sets the camera reference.");
            }
            return _playerCameraT;
        }
        set
        {
            _playerCameraT = value;
            if (_playerCameraT != null)
            {
                Debug.Log("Player camera set in GameManager");
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 不再进行严格的null检查，因为相机可能在之后设置
        if (_playerCameraT == null)
        {
            Debug.LogWarning("Player camera is not assigned yet. It might be set later.");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
