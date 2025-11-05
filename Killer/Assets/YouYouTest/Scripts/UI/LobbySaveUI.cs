using UnityEngine;
using System.Collections.Generic;
using VInspector;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class LobbySaveUI : MonoBehaviour
{
    public List<GameObject> SaveEntrys;
    public GameObject entrySample;
    public Transform entryParent;
    public Transform addButton;
    public Transform setDeleteButton;

    
    [Header("删除按钮控制")]
    public bool deleteButtonsActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        // 查找TextMeshPro组件来显示存档信息
        TMPro.TextMeshProUGUI[] textComponents = entry.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (var text in textComponents)
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

        // 查找Button组件并设置点击事件
        UnityEngine.UI.Button[] buttons = entry.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var button in buttons)
        {
            // 根据按钮名称设置不同的功能
            if (button.name.ToLower().Contains("load"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnLoadButtonClicked(slotInfo.slotName));
            }
            else if (button.name.ToLower().Contains("save"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSaveButtonClicked(slotInfo.slotName));
            }
            else if (button.name.ToLower().Contains("delete"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnDeleteButtonClicked(slotInfo.slotName));
                
                // 设置删除按钮的初始状态
                button.gameObject.SetActive(deleteButtonsActive);
            }
        }
    }

    /// <summary>
    /// 加载按钮点击事件
    /// </summary>
    /// <param name="slotName">档位名称</param>
    private void OnLoadButtonClicked(string slotName)
    {
        Debug.Log($"加载存档: {slotName}");

        // 先把要加载的存档槽名写入 GameManager，后续场景/加载逻辑会读取它
        if (GameManager.Instance != null)
        {
            GameManager.Instance.nowLoadSaveSlot = slotName;
        }
        else
        {
            Debug.LogWarning("GameManager 实例不存在，无法设置 nowLoadSaveSlot");
        }

        // 如果已经在目标场景，直接加载存档并触发相关状态
        if (SceneManager.GetActiveScene().name == "KillScene")
        {
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadSceneObjects(slotName);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetCanSwitchMode(true);
                }
                // 通知其他监听者已切换/加载存档
                GlobalEvent.OnLoadSaveChange.Invoke(slotName);
            }
            else
            {
                Debug.LogWarning("SaveLoadManager 实例不存在，无法立即加载存档，已设置 nowLoadSaveSlot 等待初始化。");
            }

            return;
        }

        // 不在 KillScene 时：在场景加载完成后执行加载逻辑
        UnityAction<Scene, LoadSceneMode> onLoaded = null;
        onLoaded = (scene, mode) =>
        {
            if (scene.name == "KillScene")
            {
                SceneManager.sceneLoaded -= onLoaded;

                if (SaveLoadManager.Instance != null)
                {
                    SaveLoadManager.Instance.LoadSceneObjects(slotName);
                    // 通知其他监听者
                    GlobalEvent.OnLoadSaveChange.Invoke(slotName);
                }
                else
                {
                    Debug.LogWarning("场景已切换到 KillScene，但 SaveLoadManager 实例尚不可用。");
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetCanSwitchMode(true);
                    // 设置为非PlayMode（保持与 Button3 行为一致）
                    GameManager.Instance.SetPlayMode(false);
                }
            }
        };

        SceneManager.sceneLoaded += onLoaded;

        // 切换到 KillScene
        SceneManager.LoadScene("KillScene");
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
    private void OnDeleteButtonClicked(string slotName)
    {
        Debug.Log($"删除存档: {slotName}");
        SaveLoadManager.Instance.DeleteSaveSlot(slotName);

        // 删除后刷新UI
        OnEnable();
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
