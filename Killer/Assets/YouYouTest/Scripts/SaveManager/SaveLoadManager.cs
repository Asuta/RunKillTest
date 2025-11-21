using UnityEngine;
using System.Collections.Generic;
using System.IO;
using YouYouTest.CommandFramework;
using YouYouTest;
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
    /// <summary>
    /// 根据指定的JSON文件名加载选中对象存档
    /// </summary>
    /// <param name="jsonFileName">JSON文件名（不包含路径）</param>
    /// <param name="createPosition">创建位置</param>
    /// <returns>加载的对象列表</returns>
    public List<GameObject> LoadSelectedObjectsByFileName(string jsonFileName, Transform createPosition)
    {
        List<GameObject> loadedObjects = new List<GameObject>();
        
        if (createPosition == null)
        {
            Debug.LogError("创建位置不能为空");
            return loadedObjects;
        }

        if (string.IsNullOrEmpty(jsonFileName))
        {
            Debug.LogError("JSON文件名不能为空");
            return loadedObjects;
        }

        string folderPath = GetSelectedObjectsFolderPath();
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("选中对象存档文件夹不存在");
            return loadedObjects;
        }

        // 确保文件名包含.json扩展名
        if (!jsonFileName.EndsWith(".json"))
        {
            jsonFileName += ".json";
        }

        string filePath = Path.Combine(folderPath, jsonFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"选中对象存档文件不存在: {filePath}");
            return loadedObjects;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            SelectedObjectsSaveData saveData = JsonUtility.FromJson<SelectedObjectsSaveData>(jsonContent);
            
            if (saveData == null || saveData.objects == null || saveData.objects.Count == 0)
            {
                Debug.LogError("存档数据为空或格式错误");
                return loadedObjects;
            }

            Debug.Log($"开始加载选中对象存档，共 {saveData.objectCount} 个对象");
            Debug.Log($"存档时间: {saveData.saveTime}");
            Debug.Log($"原始中心位置: {saveData.centerPosition}");
            Debug.Log($"新的创建中心位置: {createPosition.position}");

            // 加载所有对象
            loadedObjects = LoadSelectedObjects(saveData, createPosition.position);
            
            Debug.Log("选中对象存档加载完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载选中对象存档失败: {e.Message}");
        }
        
        return loadedObjects;
    }

    /// <summary>
    /// 读取第一个选中对象存档
    /// </summary>
    /// <param name="createPosition">创建位置</param>
    /// <returns>加载的对象列表</returns>
    public List<GameObject> LoadFirstSelectedObjects(Transform createPosition)
    {
        List<GameObject> loadedObjects = new List<GameObject>();
        
        if (createPosition == null)
        {
            Debug.LogError("创建位置不能为空");
            return loadedObjects;
        }

        string folderPath = GetSelectedObjectsFolderPath();
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("选中对象存档文件夹不存在");
            return loadedObjects;
        }

        // 获取所有存档文件并按时间排序，获取最新的一个
        var saveFiles = Directory.GetFiles(folderPath, "SelectedObjects_*.json");
        if (saveFiles.Length == 0)
        {
            Debug.LogError("没有找到选中对象存档文件");
            return loadedObjects;
        }

        // 按文件修改时间排序，获取最新的文件
        System.Array.Sort(saveFiles, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));
        string latestSaveFile = saveFiles[0];

        try
        {
            string jsonContent = File.ReadAllText(latestSaveFile);
            SelectedObjectsSaveData saveData = JsonUtility.FromJson<SelectedObjectsSaveData>(jsonContent);
            
            if (saveData == null || saveData.objects == null || saveData.objects.Count == 0)
            {
                Debug.LogError("存档数据为空或格式错误");
                return loadedObjects;
            }

            Debug.Log($"开始加载选中对象存档，共 {saveData.objectCount} 个对象");
            Debug.Log($"存档时间: {saveData.saveTime}");
            Debug.Log($"原始中心位置: {saveData.centerPosition}");
            Debug.Log($"新的创建中心位置: {createPosition.position}");

            // 加载所有对象
            loadedObjects = LoadSelectedObjects(saveData, createPosition.position);
            
            Debug.Log("选中对象存档加载完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载选中对象存档失败: {e.Message}");
        }
        
        return loadedObjects;
    }
    
    /// <summary>
    /// 加载选中对象存档数据
    /// </summary>
    /// <param name="saveData">存档数据</param>
    /// <param name="newCenterPosition">新的中心位置</param>
    /// <returns>加载的对象列表</returns>
    private List<GameObject> LoadSelectedObjects(SelectedObjectsSaveData saveData, Vector3 newCenterPosition)
    {
        List<GameObject> loadedObjects = new List<GameObject>();
        
        if (saveData == null || saveData.objects == null || saveData.objects.Count == 0)
        {
            Debug.LogWarning("LoadSelectedObjects: 存档数据为空或没有对象");
            return loadedObjects;
        }

        Debug.Log($"原始中心位置: {saveData.centerPosition}");
        Debug.Log($"新的创建中心位置: {newCenterPosition}");

        foreach (var objectData in saveData.objects)
        {
            try
            {
                // 根据prefabID获取预制体
                GameObject prefab = GetPrefabByID(objectData.prefabID);
                if (prefab == null)
                {
                    Debug.LogWarning($"找不到预制体ID: {objectData.prefabID}，跳过对象 {objectData.objectName}");
                    continue;
                }

                // 直接按世界坐标生成：newCenterPosition + 相对坐标（objectData.position）
                Vector3 worldPosition = newCenterPosition + objectData.position;
                GameObject newObject = Instantiate(prefab, worldPosition, objectData.rotation);
                newObject.transform.localScale = objectData.scale;
                newObject.name = objectData.objectName;
                
                // 添加到加载对象列表
                loadedObjects.Add(newObject);
 
                Debug.Log($"创建对象: {objectData.objectName}，相对位置: {objectData.position}，世界位置: {worldPosition}");
                
                // 应用自定义数据（在 TransformSavable 恢复中会以传入的 worldPosition 覆盖位置）
                ApplyCustomDataToObject(newObject, objectData.customData, worldPosition);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载对象 {objectData.objectName} 失败: {e.Message}");
            }
        }
        
        return loadedObjects;
    }
    
    /// <summary>
    /// 根据预制体ID获取预制体
    /// </summary>
    /// <param name="prefabID">预制体ID</param>
    /// <returns>预制体GameObject</returns>
    private GameObject GetPrefabByID(string prefabID)
    {
        GameObject prefab = null;
        
        // 尝试多个可能的路径，与ObjectSaveManager保持一致
        string[] possiblePaths = {
            prefabID, // 直接使用预制体名
            $"Prefabs/{prefabID}", // Prefabs/预制体名
            $"Prefabs/EditorElement/{prefabID}", // Prefabs/EditorElement/预制体名
            $"EditorElement/{prefabID}" // EditorElement/预制体名
        };
        
        foreach (string path in possiblePaths)
        {
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"成功找到预制体: {path}");
                return prefab;
            }
        }
        
        Debug.LogWarning($"无法找到预制体: {prefabID}，已尝试以下路径: {string.Join(", ", possiblePaths)}");
        return null;
    }
    
    /// <summary>
    /// 应用自定义数据到对象
    /// </summary>
    /// <param name="targetObject">目标对象</param>
    /// <param name="customDataJson">自定义数据JSON</param>
    /// <param name="worldPosition">世界位置</param>
    private void ApplyCustomDataToObject(GameObject targetObject, string customDataJson, Vector3 worldPosition)
    {
        if (string.IsNullOrEmpty(customDataJson))
        {
            return;
        }

        try
        {
            var customData = JsonUtility.FromJson<SerializationHelper>(customDataJson);
            if (customData == null || customData.keys == null || customData.jsonValues == null)
            {
                return;
            }

            for (int i = 0; i < customData.keys.Count; i++)
            {
                string key = customData.keys[i];
                string jsonValue = customData.jsonValues[i];

                // 根据key找到对应的组件并应用数据
                ApplyDataByComponentKey(targetObject, key, jsonValue, worldPosition);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"应用自定义数据失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 根据组件键应用数据
    /// </summary>
    /// <param name="targetObject">目标对象</param>
    /// <param name="componentKey">组件键</param>
    /// <param name="jsonValue">JSON数据</param>
    /// <param name="worldPosition">世界位置</param>
    private void ApplyDataByComponentKey(GameObject targetObject, string componentKey, string jsonValue, Vector3 worldPosition)
    {
        switch (componentKey)
        {
            case "TransformSavable":
                // TransformSavable数据已经在创建对象时应用了位置、旋转和缩放
                // 这里可以处理额外的TransformSavable特定数据
                var transformSavable = targetObject.GetComponent<TransformSavable>();
                if (transformSavable != null)
                {
                    // 如果TransformSavable有额外的配置项，可以在这里应用
                    try
                    {
                        var transformData = JsonUtility.FromJson<TransformSavable.TransformSaveData>(jsonValue);

                        // 使用加载时计算出的世界位置覆盖存档中的位置，避免被存档中的绝对位置覆盖
                        transformData.position = worldPosition;

                        // 恢复Transform（包含位置、旋转、缩放及配置项）
                        transformSavable.RestoreState(transformData);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"解析TransformSavable数据失败: {e.Message}");
                    }
                }
                break;
                
            // 可以添加其他组件类型的处理逻辑
            // case "EnemySavable":
            //     var enemySavable = targetObject.GetComponent<EnemySavable>();
            //     if (enemySavable != null)
            //     {
            //         // 应用敌人特定数据
            //     }
            //     break;
                
            default:
                Debug.LogWarning($"未知的组件类型: {componentKey}");
                break;
        }
    }
    
    /// <summary>
    /// 保存当前选中的对象到专门的选中对象存档文件夹
    /// </summary>
    /// <param name="selectedObjects">选中的对象数组</param>
    /// <param name="saveName">存档名字（与JSON文件名不同）</param>
    public void SaveSelectedObjects(GameObject[] selectedObjects, string saveName = "未命名存档")
    {
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("没有选中的对象可以保存");
            return;
        }

        Debug.Log($"准备保存 {selectedObjects.Length} 个选中对象");

        Vector3 centerPosition = Vector3.zero;
        int validObjectCount = 0;

        // 计算中心位置
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            if (selectedObjects[i] != null)
            {
                centerPosition += selectedObjects[i].transform.position;
                validObjectCount++;
            }
        }

        if (validObjectCount > 0)
        {
            centerPosition /= validObjectCount;
            Debug.Log($"选中对象的中心位置: {centerPosition}");

            // 创建选中对象的保存数据
            List<ObjectSaveData> selectedSaveDataList = new List<ObjectSaveData>();

            foreach (var obj in selectedObjects)
            {
                if (obj == null) continue;

                // 获取对象上的ISaveable组件
                ISaveable[] saveableComponents = obj.GetComponents<ISaveable>();

                if (saveableComponents.Length > 0)
                {
                    // 创建保存数据，使用相对坐标
                    ObjectSaveData saveData = new ObjectSaveData
                    {
                        prefabID = saveableComponents[0].PrefabID,
                        // 使用相对于中心位置的坐标
                        position = obj.transform.position - centerPosition,
                        rotation = obj.transform.rotation,
                        scale = obj.transform.localScale,
                        objectName = obj.name
                    };

                    // 合并所有ISaveable组件的数据
                    foreach (var saveable in saveableComponents)
                    {
                        if (!string.IsNullOrEmpty(saveable.PrefabID))
                        {
                            MergeCustomData(saveData, saveable);
                        }
                    }

                    selectedSaveDataList.Add(saveData);
                    Debug.Log($"准备保存选中对象: {obj.name} (相对位置: {saveData.position})");
                }
                else
                {
                    Debug.LogWarning($"对象 {obj.name} 没有ISaveable组件，跳过保存");
                }
            }

            if (selectedSaveDataList.Count > 0)
            {
                // 创建选中对象保存数据结构
                SelectedObjectsSaveData selectedSaveData = new SelectedObjectsSaveData
                {
                    saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    saveName = saveName,
                    objectCount = selectedSaveDataList.Count,
                    centerPosition = centerPosition,
                    objects = selectedSaveDataList
                };

                // 序列化为JSON
                string jsonData = JsonUtility.ToJson(selectedSaveData, true);

                // 保存到专门的选中对象存档文件夹
                bool success = SaveSelectedObjectsToFile("SelectedObjects_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json", jsonData);

                if (success)
                {
                    Debug.Log($"成功保存 {selectedSaveDataList.Count} 个选中对象到专门的存档文件夹");
                }
            }
            else
            {
                Debug.LogWarning("没有找到可保存的选中对象");
            }
        }
    }
    
    /// <summary>
    /// 保存选中对象数据到专门的文件夹
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="jsonData">JSON数据</param>
    /// <returns>是否保存成功</returns>
    private bool SaveSelectedObjectsToFile(string fileName, string jsonData)
    {
        try
        {
            // 获取选中对象存档文件夹路径
            string selectedObjectsFolderPath = GetSelectedObjectsFolderPath();
            string filePath = Path.Combine(selectedObjectsFolderPath, fileName);

            // 确保目录存在
            if (!Directory.Exists(selectedObjectsFolderPath))
            {
                Directory.CreateDirectory(selectedObjectsFolderPath);
            }

            // 写入文件
            File.WriteAllText(filePath, jsonData);

            Debug.Log($"成功保存选中对象数据到: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存选中对象文件失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取选中对象存档文件夹路径
    /// </summary>
    /// <returns>选中对象存档文件夹路径</returns>
    private string GetSelectedObjectsFolderPath()
    {
        // 在不同平台上使用不同的用户文件夹
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "..", "UserSaveData", "SelectedObjects");
#elif UNITY_STANDALONE_WIN
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "YourGameName", "SelectedObjects");
#elif UNITY_STANDALONE_OSX
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Library", "Application Support", "YourGameName", "SelectedObjects");
#elif UNITY_STANDALONE_LINUX
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), ".yourgamename", "SelectedObjects");
#else
            return Path.Combine(Application.persistentDataPath, "SelectedObjects");
#endif
    }
    
    /// <summary>
    /// 获取所有选中对象存档信息
    /// </summary>
    /// <returns>选中对象存档信息列表</returns>
    public List<SelectedObjectsSaveInfo> GetAllSelectedObjectsSaves()
    {
        List<SelectedObjectsSaveInfo> saveInfos = new List<SelectedObjectsSaveInfo>();
        
        try
        {
            string folderPath = GetSelectedObjectsFolderPath();
            
            if (!Directory.Exists(folderPath))
            {
                if (enableDebugLog)
                {
                    Debug.Log("选中对象存档文件夹不存在，返回空列表");
                }
                return saveInfos;
            }
            
            // 获取所有JSON文件
            string[] jsonFiles = Directory.GetFiles(folderPath, "SelectedObjects_*.json");
            
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // 获取文件信息
                FileInfo fileInfo = new FileInfo(filePath);
                
                // 尝试读取保存信息
                string saveName = fileName;
                string saveTime = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                int objectCount = 0;
                
                try
                {
                    string jsonData = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        SelectedObjectsSaveData saveData = JsonUtility.FromJson<SelectedObjectsSaveData>(jsonData);
                        if (saveData != null)
                        {
                            saveName = saveData.saveName;
                            saveTime = saveData.saveTime;
                            objectCount = saveData.objectCount;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    if (enableDebugLog)
                    {
                        Debug.LogWarning($"读取选中对象存档文件 {fileName} 的详细信息时出错: {e.Message}");
                    }
                }
                
                saveInfos.Add(new SelectedObjectsSaveInfo
                {
                    fileName = Path.GetFileName(filePath),
                    saveName = saveName,
                    saveTime = saveTime,
                    objectCount = objectCount,
                    fileSize = fileInfo.Length
                });
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"找到 {saveInfos.Count} 个选中对象存档");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取选中对象存档列表时出错: {e.Message}");
        }
        
        return saveInfos;
    }
    
    /// <summary>
    /// 根据JSON文件名删除对应的选中对象存档
    /// </summary>
    /// <param name="fileName">JSON文件名（如：SelectedObjects_20251121_105151.json）</param>
    /// <returns>是否删除成功</returns>
    public bool DeleteSelectedObjectsSave(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogWarning("文件名不能为空");
            return false;
        }
        
        try
        {
            string folderPath = GetSelectedObjectsFolderPath();
            
            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning("选中对象存档文件夹不存在");
                return false;
            }
            
            // 确保文件名包含.json扩展名
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            
            string filePath = Path.Combine(folderPath, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                
                if (enableDebugLog)
                {
                    Debug.Log($"成功删除选中对象存档文件: {fileName}");
                }
                
                return true;
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"存档文件不存在: {fileName}");
                }
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"删除选中对象存档文件 {fileName} 时出错: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 合并ISaveable组件的数据到ObjectSaveData中
    /// </summary>
    /// <param name="saveData">要合并到的目标数据</param>
    /// <param name="saveable">提供数据的ISaveable组件</param>
    private void MergeCustomData(ObjectSaveData saveData, ISaveable saveable)
    {
        var customState = new System.Collections.Generic.Dictionary<string, object>();

        // 如果customData不为空，先反序列化它
        if (!string.IsNullOrEmpty(saveData.customData))
        {
            try
            {
                SerializationHelper helper = JsonUtility.FromJson<SerializationHelper>(saveData.customData);
                if (helper != null && helper.keys != null && helper.jsonValues != null)
                {
                    for (int i = 0; i < helper.keys.Count; i++)
                    {
                        customState[helper.keys[i]] = helper.jsonValues[i];
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MergeCustomData: 反序列化旧数据时出错: {e.Message}");
            }
        }

        // 添加新的组件数据
        customState[saveable.GetType().ToString()] = saveable.CaptureState();

        // 重新序列化并保存
        saveData.customData = JsonUtility.ToJson(new SerializationHelper(customState));
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
