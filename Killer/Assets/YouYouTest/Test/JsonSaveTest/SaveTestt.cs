using UnityEngine;
using System.Collections.Generic;

public class SaveTestt : MonoBehaviour
{
    // 单例模式实现
    private static SaveTestt _instance;
    public static SaveTestt Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectsByType<SaveTestt>(FindObjectsSortMode.None)[0];
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveTestt_Singleton");
                    _instance = go.AddComponent<SaveTestt>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    [Header("保存设置")]
    [SerializeField] private string saveFileName = "SceneObjects.json";
    [SerializeField] private bool autoSaveOnStart = false;
    [SerializeField] private bool enableDebugLog = true;
    
    // 管理器实例
    private FileSaveManager fileManager;
    private ObjectSaveManager objectManager;
    
    private void Start()
    {
        // 初始化管理器
        fileManager = new FileSaveManager(enableDebugLog);
        objectManager = gameObject.AddComponent<ObjectSaveManager>();
        
        if (autoSaveOnStart)
        {
            SaveSceneObjects();
        }
    }

    private void Update()
    {
        // 按S键保存场景对象
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveSceneObjects();
        }
        
        // 按L键加载场景对象
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadSceneObjects();
        }
        
        // 按D键调试Resources文件夹内容
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugResourcesContent();
        }
    }
    
    /// <summary>
    /// 保存场景中所有实现了ISaveable接口的对象的信息到JSON文件
    /// </summary>
    public void SaveSceneObjects()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始保存场景对象...");
        }
        
        // 收集所有可保存对象的数据
        List<ObjectSaveData> saveDataList = objectManager.CollectSaveData();
        
        if (saveDataList.Count == 0)
        {
            Debug.LogWarning("没有找到可保存的对象!");
            return;
        }
        
        // 创建完整的保存数据
        SceneSaveData sceneData = new SceneSaveData
        {
            saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            objectCount = saveDataList.Count,
            objects = saveDataList
        };
        
        // 序列化为JSON
        string jsonData = JsonUtility.ToJson(sceneData, true);
        
        // 保存到文件
        bool success = fileManager.SaveToFile(saveFileName, jsonData);
        
        if (success && enableDebugLog)
        {
            Debug.Log($"成功保存 {saveDataList.Count} 个对象");
        }
    }
    
    /// <summary>
    /// 从JSON文件加载场景对象
    /// </summary>
    public void LoadSceneObjects()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始加载场景对象...");
        }
        
        // 检查文件是否存在
        if (!fileManager.FileExists(saveFileName))
        {
            Debug.LogWarning($"保存文件不存在: {saveFileName}");
            return;
        }
        
        // 从文件读取数据
        string jsonData = fileManager.LoadFromFile(saveFileName);
        
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("无法读取保存文件!");
            return;
        }
        
        try
        {
            // 反序列化JSON数据
            SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(jsonData);
            
            if (sceneData == null || sceneData.objects == null)
            {
                Debug.LogError("无法解析保存文件数据!");
                return;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"准备加载 {sceneData.objectCount} 个对象 (保存时间: {sceneData.saveTime})");
            }
            
            // 清除现有的ObjectIdentifier对象（可选）
            // objectManager.ClearExistingObjects();
            
            // 加载每个对象
            int loadedCount = 0;
            foreach (ObjectSaveData objectData in sceneData.objects)
            {
                if (objectManager.LoadSingleObject(objectData))
                {
                    loadedCount++;
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"成功加载 {loadedCount}/{sceneData.objectCount} 个对象");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载数据时出错: {e.Message}");
        }
    }
    
    /// <summary>
    /// 调试Resources文件夹内容
    /// </summary>
    public void DebugResourcesContent()
    {
        objectManager.DebugResourcesContent();
    }
}
