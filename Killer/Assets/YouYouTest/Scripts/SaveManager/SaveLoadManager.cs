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
    
    [Header("过程加载设置")]
    [SerializeField] private bool useProgressiveLoading = true;
    [SerializeField] private float progressiveLoadingDuration = 3.0f; // 加载持续时间（秒）
    [SerializeField] private float minObjectsPerFrame = 1; // 每帧最少加载对象数
    [SerializeField] private float maxObjectsPerFrame = 50; // 每帧最多加载对象数
    
    // 管理器实例
    private FileSaveManager fileManager;
    private ObjectSaveManager objectManager;
    
    // 过程加载状态
    private bool isLoading = false;
    private Coroutine loadingCoroutine = null;
    
    // 加载进度事件
    public System.Action<float, int, int> OnLoadingProgress; // 进度(0-1), 当前加载数量, 总数量
    public System.Action OnLoadingComplete; // 加载完成
    public System.Action<string> OnLoadingError; // 加载错误
    
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
    /// 创建空档位（不包含任何对象）
    /// </summary>
    /// <param name="slotName">档位名称</param>
    public void CreateEmptySaveSlot(string slotName)
    {
        if (enableDebugLog)
        {
            Debug.Log($"创建空档位: {slotName}");
        }
        
        if (string.IsNullOrEmpty(slotName))
        {
            Debug.LogError("档位名称不能为空");
            return;
        }
        
        // 创建空的保存数据
        SceneSaveData sceneData = new SceneSaveData
        {
            saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            objectCount = 0,
            objects = new List<ObjectSaveData>()
        };
        
        // 序列化为JSON
        string jsonData = JsonUtility.ToJson(sceneData, true);
        
        // 生成文件名
        string fileName = GenerateSaveFileName(slotName);
        
        // 保存到文件
        bool success = fileManager.SaveToFile(fileName, jsonData);
        
        if (success && enableDebugLog)
        {
            Debug.Log($"成功创建空档位: {slotName}");
        }
    }
    
    /// <summary>
    /// 从JSON文件加载场景对象
    /// </summary>
    /// <param name="slotName">档位名称，如果为空则使用默认档位</param>
    public void LoadSceneObjects(string slotName = null)
    {
        if (isLoading)
        {
            Debug.LogWarning("已有加载任务在进行中，忽略新的加载请求");
            return;
        }
        
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
            OnLoadingError?.Invoke($"保存文件不存在: {fileName}");
            return;
        }
        
        // 从文件读取数据
        string jsonData = fileManager.LoadFromFile(fileName);
        
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("无法读取保存文件!");
            OnLoadingError?.Invoke("无法读取保存文件!");
            return;
        }
        
        try
        {
            // 反序列化JSON数据
            SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(jsonData);
            
            if (sceneData == null || sceneData.objects == null)
            {
                Debug.LogError("无法解析保存文件数据!");
                OnLoadingError?.Invoke("无法解析保存文件数据!");
                return;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"准备加载 {sceneData.objectCount} 个对象 (保存时间: {sceneData.saveTime})");
            }
            
            // 在加载前先清理场景
            CleanupSceneBeforeLoad();
            
            // 根据设置选择加载方式
            if (useProgressiveLoading && sceneData.objectCount > 0)
            {
                // 使用过程加载
                StartProgressiveLoading(sceneData);
            }
            else
            {
                // 使用即时加载（原有逻辑）
                LoadObjectsImmediately(sceneData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载数据时出错: {e.Message}");
            OnLoadingError?.Invoke($"加载数据时出错: {e.Message}");
        }
    }
    
    /// <summary>
    /// 立即加载所有对象（原有逻辑）
    /// </summary>
    /// <param name="sceneData">场景数据</param>
    private void LoadObjectsImmediately(SceneSaveData sceneData)
    {
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
        
        OnLoadingComplete?.Invoke();
    }
    
    /// <summary>
    /// 开始过程加载
    /// </summary>
    /// <param name="sceneData">场景数据</param>
    private void StartProgressiveLoading(SceneSaveData sceneData)
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        
        loadingCoroutine = StartCoroutine(ProgressiveLoadingCoroutine(sceneData));
    }
    
    /// <summary>
    /// 过程加载协程
    /// </summary>
    /// <param name="sceneData">场景数据</param>
    /// <returns></returns>
    private System.Collections.IEnumerator ProgressiveLoadingCoroutine(SceneSaveData sceneData)
    {
        isLoading = true;
        
        int totalObjects = sceneData.objects.Count;
        
        if (enableDebugLog)
        {
            Debug.Log($"开始过程加载，目标时长: {progressiveLoadingDuration}秒，对象数量: {totalObjects}");
        }
        
        // 清除现有的ObjectIdentifier对象（可选）
        // objectManager.ClearExistingObjects();
        
        int loadedCount = 0;
        float startTime = Time.time;
        
        // 智能计算加载策略
        float estimatedFrameRate = 60f; // 默认60fps
        float totalFrames = progressiveLoadingDuration * estimatedFrameRate;
        
        // 如果对象数量很少，使用基于时间的加载策略
        if (totalObjects <= totalFrames)
        {
            // 对象数量少于总帧数，使用基于时间的加载
            yield return StartCoroutine(TimeBasedLoading(sceneData, totalObjects, startTime));
        }
        else
        {
            // 对象数量较多，使用基于帧的加载
            yield return StartCoroutine(FrameBasedLoading(sceneData, totalObjects, startTime, totalFrames));
        }
        
        // 加载完成
        isLoading = false;
        loadingCoroutine = null;
        
        float totalTime = Time.time - startTime;
        
        if (enableDebugLog)
        {
            Debug.Log($"过程加载完成! 成功加载 {loadedCount}/{totalObjects} 个对象，总用时: {totalTime:F2}秒");
        }
        
        OnLoadingComplete?.Invoke();
    }
    
    /// <summary>
    /// 基于时间的加载策略（适用于对象数量较少的情况）
    /// </summary>
    private System.Collections.IEnumerator TimeBasedLoading(SceneSaveData sceneData, int totalObjects, float startTime)
    {
        int loadedCount = 0;
        
        if (enableDebugLog)
        {
            Debug.Log("使用基于时间的加载策略");
        }
        
        while (loadedCount < totalObjects)
        {
            float elapsedTime = Time.time - startTime;
            float targetProgress = Mathf.Clamp01(elapsedTime / progressiveLoadingDuration);
            int targetCount = Mathf.FloorToInt(targetProgress * totalObjects);
            
            // 加载当前进度应该加载的对象
            while (loadedCount < targetCount && loadedCount < totalObjects)
            {
                ObjectSaveData objectData = sceneData.objects[loadedCount];
                if (objectManager.LoadSingleObject(objectData))
                {
                    loadedCount++;
                }
                else
                {
                    // 即使加载失败也要增加计数，避免死循环
                    loadedCount++;
                }
                
                // 更新进度
                float progress = (float)loadedCount / totalObjects;
                OnLoadingProgress?.Invoke(progress, loadedCount, totalObjects);
                
                if (enableDebugLog)
                {
                    Debug.Log($"加载进度: {progress:P1} ({loadedCount}/{totalObjects}) - 已用时: {elapsedTime:F2}秒");
                }
            }
            
            // 等待下一帧
            yield return null;
        }
    }
    
    /// <summary>
    /// 基于帧的加载策略（适用于对象数量较多的情况）
    /// </summary>
    private System.Collections.IEnumerator FrameBasedLoading(SceneSaveData sceneData, int totalObjects, float startTime, float totalFrames)
    {
        int loadedCount = 0;
        
        // 计算每帧应该加载的对象数量
        float objectsPerFrame = totalObjects / totalFrames;
        
        // 限制每帧加载对象数量
        objectsPerFrame = Mathf.Clamp(objectsPerFrame, minObjectsPerFrame, maxObjectsPerFrame);
        
        if (enableDebugLog)
        {
            Debug.Log($"使用基于帧的加载策略，每帧加载对象数量: {objectsPerFrame:F2}");
        }
        
        // 分帧加载对象
        while (loadedCount < totalObjects)
        {
            // 计算这一帧应该加载的对象数量
            int objectsToLoadThisFrame = Mathf.CeilToInt(objectsPerFrame);
            objectsToLoadThisFrame = Mathf.Min(objectsToLoadThisFrame, totalObjects - loadedCount);
            
            // 加载这一帧的对象
            for (int i = 0; i < objectsToLoadThisFrame && loadedCount < totalObjects; i++)
            {
                ObjectSaveData objectData = sceneData.objects[loadedCount];
                if (objectManager.LoadSingleObject(objectData))
                {
                    loadedCount++;
                }
                else
                {
                    // 即使加载失败也要增加计数，避免死循环
                    loadedCount++;
                }
            }
            
            // 计算进度
            float progress = (float)loadedCount / totalObjects;
            float elapsedTime = Time.time - startTime;
            
            // 触发进度事件
            OnLoadingProgress?.Invoke(progress, loadedCount, totalObjects);
            
            if (enableDebugLog && loadedCount % 50 == 0) // 每50个对象输出一次日志
            {
                Debug.Log($"加载进度: {progress:P1} ({loadedCount}/{totalObjects}) - 已用时: {elapsedTime:F2}秒");
            }
            
            // 如果还有对象未加载，等待下一帧
            if (loadedCount < totalObjects)
            {
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// 停止当前加载
    /// </summary>
    public void StopLoading()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        isLoading = false;
        
        if (enableDebugLog)
        {
            Debug.Log("已停止加载");
        }
    }
    
    /// <summary>
    /// 获取当前加载状态
    /// </summary>
    /// <returns>是否正在加载</returns>
    public bool IsLoading()
    {
        return isLoading;
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
