using UnityEngine;
using System.Collections.Generic;
using VInspector;

public class SaveUI : MonoBehaviour
{
    public List<GameObject> SaveEntrys;
    public GameObject entrySample;
    public Transform entryParent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
        UnityEngine.UI.Button[] buttons = entry.GetComponentsInChildren<UnityEngine.UI.Button>();
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
        SaveLoadManager.Instance.LoadSceneObjects(slotName);
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
