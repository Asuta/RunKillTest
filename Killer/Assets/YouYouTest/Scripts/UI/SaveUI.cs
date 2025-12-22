using UnityEngine;
using System.Collections.Generic;
using System.IO;
using VInspector;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class SaveUI : AutoCleanupBehaviour
{
    public List<GameObject> SaveEntrys;
    public GameObject entrySample;
    public Transform entryParent;
    public Transform addButton;
    public Transform setDeleteButton;

    [Header("服务器设置")]
    public string serverUrl = "http://127.0.0.1:8000";

    
    [Header("删除按钮控制")]
    public bool deleteButtonsActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 注册全局事件监听器
        RegisterEvent(GlobalEvent.OnSaveComplete, OnSaveComplete);
        GlobalEvent.OnLoadSaveChange.AddListener(OnLoadSaveChangeInternal);
        
        // 为addButton添加点击事件监听器
        if (addButton != null)
        {
            UnityEngine.UI.Button button = addButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnAddButtonClicked);
            }
            else
            {
                Debug.LogError("addButton上没有找到Button组件");
            }
        }
        else
        {
            Debug.LogError("addButton未设置");
        }
        
        // 为setDeleteButton添加点击事件监听器
        if (setDeleteButton != null)
        {
            UnityEngine.UI.Button button = setDeleteButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ChangeActiveOfDeleteButton);
            }
            else
            {
                Debug.LogError("setDeleteButton上没有找到Button组件");
            }
        }
        else
        {
            Debug.LogError("setDeleteButton未设置");
        }
    }

    /// <summary>
    /// 保存完成事件处理
    /// </summary>
    /// <param name="slotName">保存的档位名称</param>
    private void OnSaveComplete(string slotName)
    {
        Debug.Log($"收到保存完成事件，档位: {slotName}，刷新UI列表");
        // 刷新存档列表显示
        OnEnable();
    }

    /// <summary>
    /// 存档加载变更事件处理
    /// </summary>
    /// <param name="saveKey">存档Key</param>
    private void OnLoadSaveChangeInternal(string saveKey)
    {
        Debug.Log($"收到存档加载变更事件: {saveKey}，刷新UI列表以更新高亮状态");
        // 刷新存档列表显示以更新高亮
        OnEnable();
    }

    private void OnDestroy()
    {
        GlobalEvent.OnLoadSaveChange.RemoveListener(OnLoadSaveChangeInternal);
    }

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        // 清空现有的存档条目
        ClearExistingEntries();

        // 获取所有存档档位信息
        List<SaveSlotInfo> saveSlots = SaveLoadManager.Instance.GetAllSaveSlots();

        // 为每个存档档位创建UI条目
        foreach (SaveSlotInfo slotInfo in saveSlots)
        {
            CreateSaveEntry(slotInfo);
        }
    }

    /// <summary>
    /// 清空现有的存档条目
    /// </summary>
    private void ClearExistingEntries()
    {
        // 清空SaveEntrys列表
        SaveEntrys.Clear();

        // 销毁entryParent下的所有子物体（除了entrySample）
        for (int i = entryParent.childCount - 1; i >= 0; i--)
        {
            Transform child = entryParent.GetChild(i);
            if (child.gameObject != entrySample)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 创建存档条目UI
    /// </summary>
    /// <param name="slotInfo">存档档位信息</param>
    private void CreateSaveEntry(SaveSlotInfo slotInfo)
    {
        // 实例化entrySample
        GameObject entryInstance = Instantiate(entrySample, entryParent);

        // 添加到SaveEntrys列表
        SaveEntrys.Add(entryInstance);

        // 设置entry为激活状态
        entryInstance.SetActive(true);

        // 查找并设置UI组件
        SetupEntryComponents(entryInstance, slotInfo);
    }

    /// <summary>
    /// 设置条目组件的内容
    /// </summary>
    /// <param name="entry">条目GameObject</param>
    /// <param name="slotInfo">存档档位信息</param>
    private void SetupEntryComponents(GameObject entry, SaveSlotInfo slotInfo)
    {
        bool isWebLevel = slotInfo.subFolder != null && slotInfo.subFolder.ToLower().Contains("web");

        // 根据关卡来源设置底色
        // 如果是网络下载的关卡（subFolder 包含 "Web"），将底色设置为紫色
        UnityEngine.UI.Image backgroundImage = entry.GetComponent<UnityEngine.UI.Image>();
        if (backgroundImage != null)
        {
            if (isWebLevel)
            {
                // 设置为紫色 (Purple)
                backgroundImage.color = new Color(0.6f, 0.2f, 0.8f, 1f);
            }
            else
            {
                // 保持原色（通常是白色或默认色）
                backgroundImage.color = Color.white;
            }
        }

        // 设置外描边高亮（如果当前是加载的关卡）
        UnityEngine.UI.Outline outline = entry.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
        {
            // 如果没有Outline组件，尝试添加一个
            outline = entry.AddComponent<UnityEngine.UI.Outline>();
            outline.effectDistance = new Vector2(5, -5);
        }

        if (outline != null)
        {
            string currentKey = SaveLoadManager.ComposeSaveKey(slotInfo.subFolder, slotInfo.slotName);
            bool isCurrent = currentKey == GameManager.Instance.nowLoadSaveSlot;
            
            outline.enabled = isCurrent;
            outline.effectColor = Color.yellow; // 高亮颜色设为黄色
        }

        // 查找TextMeshPro组件来显示存档信息
        TMPro.TextMeshProUGUI[] textComponents = entry.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (var text in textComponents)
        {
            // 根据文本名称或内容来设置不同的信息
            if (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("slot"))
            {
                // 检查这个Text组件是否属于InputField
                TMPro.TMP_InputField inputField = text.GetComponentInParent<TMPro.TMP_InputField>();
                if (inputField != null && inputField.textComponent == text)
                {
                    // 如果是InputField的文本组件，通过InputField来设置文本
                    inputField.text = slotInfo.LevelName;
                    LevelNameInput levelNameInput = inputField.GetComponent<LevelNameInput>();
                    if (levelNameInput != null)
                    {
                        levelNameInput.levelJsonName = slotInfo.fileName;
                        levelNameInput.subFolder = slotInfo.subFolder;
                    }
                    Debug.LogError("找到InputField啦，通过InputField设置文本: " + inputField.name);
                }
                else
                {
                    // 如果是普通的Text组件，直接设置
                    text.text = slotInfo.LevelName;
                    Debug.LogError("找到普通Text啦，名字是: " + text.name);
                }
            }
            else if (text.name.ToLower().Contains("time") || text.name.ToLower().Contains("date"))
            {
                text.text = slotInfo.saveTime;
            }
            else if (text.name.ToLower().Contains("count") || text.name.ToLower().Contains("object"))
            {
                text.text = $"对象数量: {slotInfo.objectCount}";
            }
            else if (text.name.ToLower().Contains("size"))
            {
                text.text = $"文件大小: {slotInfo.fileSize} 字节";
            }
            // 如果没有特定的名称标识，使用第一个TextMeshPro组件显示存档名称
            else if (string.IsNullOrEmpty(text.text) || text.text == "New Text")
            {
                text.text = slotInfo.slotName;
            }
        }

        // 同时也查找普通Text组件（以防万一）
        UnityEngine.UI.Text[] legacyTextComponents = entry.GetComponentsInChildren<UnityEngine.UI.Text>();
        foreach (var text in legacyTextComponents)
        {
            // 根据文本名称或内容来设置不同的信息
            if (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("slot"))
            {
                text.text = slotInfo.slotName;
            }
            else if (text.name.ToLower().Contains("time") || text.name.ToLower().Contains("date"))
            {
                text.text = slotInfo.saveTime;
            }
            else if (text.name.ToLower().Contains("count") || text.name.ToLower().Contains("object"))
            {
                text.text = $"对象数量: {slotInfo.objectCount}";
            }
            else if (text.name.ToLower().Contains("size"))
            {
                text.text = $"文件大小: {slotInfo.fileSize} 字节";
            }
            // 如果没有特定的名称标识，使用第一个Text组件显示存档名称
            else if (string.IsNullOrEmpty(text.text) || text.text == "New Text")
            {
                text.text = slotInfo.slotName;
            }
        }

        // 查找Image组件并设置截图
        SetupEntryImage(entry, slotInfo);

        // 查找Button组件并设置点击事件
        UnityEngine.UI.Button[] buttons = entry.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var button in buttons)
        {
            string btnName = button.name.ToLower();
            // 根据按钮名称设置不同的功能
            if (btnName.Contains("upload"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnUploadButtonClicked(slotInfo));

                // 如果是Web关卡，隐藏上传按钮
                button.gameObject.SetActive(!isWebLevel);
            }
            else if (btnName.Contains("load"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnLoadButtonClicked(slotInfo));
            }
            else if (btnName.Contains("save"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSaveButtonClicked(slotInfo.slotName));
            }
            else if (btnName.Contains("delete"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnDeleteButtonClicked(slotInfo));
                
                // 设置删除按钮的初始状态
                button.gameObject.SetActive(deleteButtonsActive);
            }
        }
    }

    /// <summary>
    /// 设置条目的截图图片
    /// </summary>
    /// <param name="entry">条目GameObject</param>
    /// <param name="slotInfo">存档档位信息</param>
    private void SetupEntryImage(GameObject entry, SaveSlotInfo slotInfo)
    {
        // 查找名为"Image"的子物体
        Transform imageTransform = entry.transform.Find("Image");
        if (imageTransform == null)
        {
            // 如果直接找不到，尝试在所有子物体中查找
            Transform[] childTransforms = entry.GetComponentsInChildren<Transform>();
            foreach (var child in childTransforms)
            {
                if (child.name == "Image")
                {
                    imageTransform = child;
                    break;
                }
            }
        }

        if (imageTransform != null)
        {
            UnityEngine.UI.Image imageComponent = imageTransform.GetComponent<UnityEngine.UI.Image>();
            if (imageComponent != null)
            {
                // 构建图片文件路径
                // 逻辑：优先尝试与 JSON 同名的 .png，如果找不到（如 Web 关卡），尝试去掉 _SceneObjects 后缀的 .png
                string imageFileName = slotInfo.fileName.Replace(".json", ".png");
                string userFolderPath = SaveLoadManager.Instance.GetUserFolderPath();
                string imagePath = Path.GetFullPath(Path.Combine(userFolderPath, slotInfo.subFolder, imageFileName));

                // 如果主路径不存在，尝试备选路径（去掉 _SceneObjects 后缀）
                if (!File.Exists(imagePath) && imageFileName.EndsWith("_SceneObjects.png"))
                {
                    string fallbackFileName = imageFileName.Replace("_SceneObjects.png", ".png");
                    string fallbackPath = Path.GetFullPath(Path.Combine(userFolderPath, slotInfo.subFolder, fallbackFileName));
                    if (File.Exists(fallbackPath))
                    {
                        imagePath = fallbackPath;
                    }
                }

                // 检查图片文件是否存在
                if (File.Exists(imagePath))
                {
                    // 读取图片文件
                    byte[] fileData = File.ReadAllBytes(imagePath);
                    Texture2D texture = new Texture2D(2, 2);
                    
                    if (texture.LoadImage(fileData))
                    {
                        // 创建Sprite并赋值给Image组件
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        imageComponent.sprite = sprite;
                        
                        Debug.Log($"成功加载截图: {Path.GetFileName(imagePath)} (来自: {slotInfo.subFolder})");
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载图片数据: {imagePath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"截图文件不存在: {imagePath}");
                    // 可以设置一个默认图片或者保持空白
                    imageComponent.sprite = null;
                }
            }
            else
            {
                Debug.LogWarning("找到名为'Image'的对象，但没有Image组件");
            }
        }
        else
        {
            Debug.LogWarning("在条目中未找到名为'Image'的子物体");
        }
    }

    /// <summary>
    /// 加载按钮点击事件
    /// </summary>
    /// <param name="slotName">档位名称</param>
    private void OnLoadButtonClicked(SaveSlotInfo slotInfo)
    {
        if (slotInfo == null) return;

        Debug.Log($"加载存档(精确): {slotInfo.subFolder}/{slotInfo.fileName}");
        SaveLoadManager.Instance.LoadSceneObjectsByFileName(slotInfo.fileName, slotInfo.subFolder);
        GlobalEvent.OnLoadSaveChange.Invoke(SaveLoadManager.ComposeSaveKey(slotInfo.subFolder, slotInfo.slotName));
    }

    /// <summary>
    /// 保存按钮点击事件
    /// </summary>
    /// <param name="slotName">档位名称</param>
    private void OnSaveButtonClicked(string slotName)
    {
        Debug.Log($"保存到档位: {slotName}");
        SaveLoadManager.Instance.SaveSceneObjects(slotName);

        // 保存后刷新UI
        OnEnable();
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    /// <param name="slotName">档位名称</param>
    private void OnDeleteButtonClicked(SaveSlotInfo slotInfo)
    {
        if (slotInfo == null) return;

        Debug.Log($"删除存档(精确): {slotInfo.subFolder}/{slotInfo.fileName}");
        
        // 精确删除存档数据（同时删除对应的图片）
        SaveLoadManager.Instance.DeleteSaveSlotByFileName(slotInfo.fileName, slotInfo.subFolder);

        // 删除后刷新UI
        OnEnable();
    }

    /// <summary>
    /// 上传按钮点击事件
    /// </summary>
    private void OnUploadButtonClicked(SaveSlotInfo slotInfo)
    {
        if (slotInfo == null) return;
        Debug.Log($"开始上传存档: {slotInfo.LevelName}");
        SaveNetworkManager.Instance.UploadLevel(slotInfo, serverUrl, (success, message) => {
            if (success)
            {
                Debug.Log($"<color=green>上传成功!</color> {message}");
            }
            else
            {
                Debug.LogError($"上传失败: {message}");
            }
        });
    }

    /// <summary>
    /// 添加按钮点击事件
    /// </summary>
    private void OnAddButtonClicked()
    {
        Debug.Log("添加新档位按钮被点击");
        
        // 生成新的档位名称
        string newSlotName = GenerateNewSlotName();
        
        // 创建空档位（不包含任何对象）
        SaveLoadManager.Instance.CreateEmptySaveSlot(newSlotName);
        
        // 刷新列表显示
        OnEnable();
    }

    /// <summary>
    /// 生成新的档位名称
    /// </summary>
    /// <returns>新的档位名称</returns>
    private string GenerateNewSlotName()
    {
        // 获取所有现有存档档位
        List<SaveSlotInfo> existingSlots = SaveLoadManager.Instance.GetAllSaveSlots();
        
        // 基础名称
        string baseName = "slot One";
        
        // 如果没有存档，直接返回基础名称
        if (existingSlots.Count == 0)
        {
            return baseName;
        }
        
        // 检查基础名称是否已存在
        bool baseNameExists = false;
        foreach (var slot in existingSlots)
        {
            if (slot.slotName == baseName)
            {
                baseNameExists = true;
                break;
            }
        }
        
        // 如果基础名称不存在，直接返回基础名称
        if (!baseNameExists)
        {
            return baseName;
        }
        
        // 基础名称已存在，查找最大的数字后缀
        int maxNumber = 1;
        foreach (var slot in existingSlots)
        {
            if (slot.slotName.StartsWith(baseName))
            {
                // 尝试提取数字后缀
                string suffix = slot.slotName.Substring(baseName.Length).Trim();
                if (!string.IsNullOrEmpty(suffix))
                {
                    if (int.TryParse(suffix, out int number))
                    {
                        maxNumber = Mathf.Max(maxNumber, number);
                    }
                }
            }
        }
        
        // 返回新的名称（数字+1）
        return $"{baseName} {maxNumber + 1}";
    }

    // Update is called once per frame
    void Update()
    {

    }

    [Button("切换删除按钮的active状态")]
    public void ChangeActiveOfDeleteButton()
    {
        // 如果SaveEntrys为空，尝试重新初始化
        if (SaveEntrys.Count == 0)
        {
            OnEnable();
        }
        
        // 切换删除按钮状态
        deleteButtonsActive = !deleteButtonsActive;
        
        // 遍历所有存档条目，设置删除按钮状态
        foreach (GameObject entry in SaveEntrys)
        {
            if (entry != null)
            {
                UnityEngine.UI.Button[] buttons = entry.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                
                foreach (UnityEngine.UI.Button button in buttons)
                {
                    // 检查是否为删除按钮
                    if (button.name.ToLower().Contains("delete"))
                    {
                        button.gameObject.SetActive(deleteButtonsActive);
                    }
                }
            }
        }
    }
}