using UnityEngine;
using System.Collections.Generic;
using System.IO;
using YouYouTest.CommandFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveLoadManager : MonoBehaviour
{
    // 单例模式实现
    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectsByType<SaveLoadManager>(FindObjectsSortMode.None)[0];
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveTestt_Singleton");
                    _instance = go.AddComponent<SaveLoadManager>();
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
    [SerializeField] private string defaultSlotName = "Default";
    
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
    
    /// <summary>
    /// 清理场景中的存档对象和命令历史
    /// </summary>
    private void CleanupSceneBeforeLoad()
    {
        if (enableDebugLog)
        {
            Debug.Log("开始清理场景中的存档对象...");
        }
        
        // 查找场景中所有实现了存档接口的对象（只查找场景实例）
        var savableObjects = new List<MonoBehaviour>();
        
        // 查找场景中所有MonoBehaviour组件（包括非激活的）
        MonoBehaviour[] allMonoBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in allMonoBehaviours)
        {
            if (mb is ISaveable)
            {
                savableObjects.Add(mb);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"找到 {savableObjects.Count} 个存档对象需要清理");
        }
        
        // 销毁所有存档对象
        foreach (var savable in savableObjects)
        {
            if (savable != null && savable.gameObject != null)
            {
                DestroyImmediate(savable.gameObject);
            }
        }
        
        // 清空命令历史
        CommandHistory.Instance.Clear();
        
        if (enableDebugLog)
        {
            Debug.Log("场景清理完成，已销毁所有存档对象并清空命令历史");
        }
    }

    private void Update()
    {
        // 按S键保存场景对象（默认档位）
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveSceneObjects();
        }
        
        // 按L键加载场景对象（默认档位）
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadSceneObjects();
        }
        
        // 按D键调试Resources文件夹内容
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugResourcesContent();
        }
        
        // 按Shift+S保存到档位1
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift))
        {
            SaveSceneObjects("Slot1");
        }
        
        // 按Shift+L从档位1加载
        if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftShift))
        {
            LoadSceneObjects("Slot1");
        }
        
        // 按Ctrl+S保存到档位2
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
        {
            SaveSceneObjects("Slot2");
        }
        
        // 按Ctrl+L从档位2加载
        if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftControl))
        {
            LoadSceneObjects("Slot2");
        }
        
        // 按Alt+S保存到档位3
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftAlt))
        {
            SaveSceneObjects("Slot3");
        }
        
        // 按Alt+L从档位3加载
        if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftAlt))
        {
            LoadSceneObjects("Slot3");
        }
        
        // 按I键显示所有存档档位信息
        if (Input.GetKeyDown(KeyCode.I))
        {
            DebugAllSaveSlots();
        }
        
        // 按Shift+D删除档位1
        if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftShift))
        {
            DeleteSaveSlot("Slot1");
        }
        
        // 按Ctrl+D删除档位2
        if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
        {
            DeleteSaveSlot("Slot2");
        }
        
        // 按Alt+D删除档位3
        if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftAlt))
        {
            DeleteSaveSlot("Slot3");
        }
    }
    
    /// <summary>
    /// 根据档位名称生成文件名
    /// </summary>
    /// <param name="slotName">档位名称</param>
    /// <returns>生成的文件名</returns>
    private string GenerateSaveFileName(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
        {
            slotName = defaultSlotName;
        }
        
        // 移除文件名中的非法字符
        string invalidChars = new string(Path.GetInvalidFileNameChars());
        foreach (char c in invalidChars)
        {
            slotName = slotName.Replace(c.ToString(), "");
        }
        
        return $"{slotName}_SceneObjects.json";
    }
    
    /// <summary>
    /// 保存场景中所有实现了ISaveable接口的对象的信息到JSON文件
    /// </summary>
    /// <param name="slotName">档位名称，如果为空则使用默认档位</param>
    public void SaveSceneObjects(string slotName = null)
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
        
        // 生成文件名
        string fileName = GenerateSaveFileName(slotName);
        
        // 保存到文件
        bool success = fileManager.SaveToFile(fileName, jsonData);
        
        if (success && enableDebugLog)
        {
            Debug.Log($"成功保存 {saveDataList.Count} 个对象");
        }
    }
    
    /// <summary>
    /// 从JSON文件加载场景对象
    /// </summary>
    /// <param name="slotName">档位名称，如果为空则使用默认档位</param>
    public void LoadSceneObjects(string slotName = null)
    {
        if (enableDebugLog)
        {
            Debug.Log("开始加载场景对象...");
        }
        
        // 生成文件名
        string fileName = GenerateSaveFileName(slotName);
        
        // 检查文件是否存在
        if (!fileManager.FileExists(fileName))
        {
            Debug.LogWarning($"保存文件不存在: {fileName}");
            return;
        }
        
        // 在加载前先清理场景
        CleanupSceneBeforeLoad();
        
        // 从文件读取数据
        string jsonData = fileManager.LoadFromFile(fileName);
        
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
    
    /// <summary>
    /// 获取所有存档档位信息
    /// </summary>
    /// <returns>存档档位信息列表</returns>
    public List<SaveSlotInfo> GetAllSaveSlots()
    {
        List<SaveSlotInfo> slotInfos = new List<SaveSlotInfo>();
        
        try
        {
            string userFolderPath = fileManager.GetUserFolderPath();
            
            if (!Directory.Exists(userFolderPath))
            {
                if (enableDebugLog)
                {
                    Debug.Log("存档文件夹不存在，返回空列表");
                }
                return slotInfos;
            }
            
            // 获取所有JSON文件
            string[] jsonFiles = Directory.GetFiles(userFolderPath, "*_SceneObjects.json");
            
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // 提取档位名称（去掉"_SceneObjects"后缀）
                if (fileName.EndsWith("_SceneObjects"))
                {
                    string slotName = fileName.Substring(0, fileName.Length - "_SceneObjects".Length);
                    
                    // 获取文件信息
                    FileInfo fileInfo = new FileInfo(filePath);
                    
                    // 尝试读取保存时间
                    string saveTime = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                    int objectCount = 0;
                    
                    try
                    {
                        string jsonData = File.ReadAllText(filePath);
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(jsonData);
                            if (sceneData != null)
                            {
                                saveTime = sceneData.saveTime;
                                objectCount = sceneData.objectCount;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        if (enableDebugLog)
                        {
                            Debug.LogWarning($"读取存档文件 {slotName} 的详细信息时出错: {e.Message}");
                        }
                    }
                    
                    slotInfos.Add(new SaveSlotInfo
                    {
                        slotName = slotName,
                        fileName = Path.GetFileName(filePath),
                        saveTime = saveTime,
                        objectCount = objectCount,
                        fileSize = fileInfo.Length
                    });
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"找到 {slotInfos.Count} 个存档档位");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取存档档位列表时出错: {e.Message}");
        }
        
        return slotInfos;
    }
    
    /// <summary>
    /// 删除指定档位
    /// </summary>
    /// <param name="slotName">档位名称</param>
    /// <returns>是否删除成功</returns>
    public bool DeleteSaveSlot(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
        {
            Debug.LogWarning("档位名称不能为空");
            return false;
        }
        
        try
        {
            string fileName = GenerateSaveFileName(slotName);
            string userFolderPath = fileManager.GetUserFolderPath();
            string filePath = Path.Combine(userFolderPath, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                
                if (enableDebugLog)
                {
                    Debug.Log($"成功删除存档档位: {slotName}");
                }
                
                return true;
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"存档档位不存在: {slotName}");
                }
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"删除存档档位 {slotName} 时出错: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 检查指定档位是否存在
    /// </summary>
    /// <param name="slotName">档位名称</param>
    /// <returns>档位是否存在</returns>
    public bool SaveSlotExists(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
        {
            return false;
        }
        
        string fileName = GenerateSaveFileName(slotName);
        return fileManager.FileExists(fileName);
    }
    
    /// <summary>
    /// 打印所有存档档位信息到控制台
    /// </summary>
    public void DebugAllSaveSlots()
    {
        List<SaveSlotInfo> slots = GetAllSaveSlots();
        
        Debug.Log("=== 所有存档档位信息 ===");
        if (slots.Count == 0)
        {
            Debug.Log("没有找到任何存档档位");
        }
        else
        {
            foreach (var slot in slots)
            {
                Debug.Log($"档位名称: {slot.slotName}");
                Debug.Log($"  保存时间: {slot.saveTime}");
                Debug.Log($"  对象数量: {slot.objectCount}");
                Debug.Log($"  文件大小: {slot.fileSize} 字节");
                Debug.Log($"  文件名: {slot.fileName}");
                Debug.Log("---");
            }
        }
        Debug.Log("=== 调试完成 ===");
    }
}

/// <summary>
/// 存档档位信息
/// </summary>
[System.Serializable]
public class SaveSlotInfo
{
    public string slotName;
    public string fileName;
    public string saveTime;
    public int objectCount;
    public long fileSize;
}
