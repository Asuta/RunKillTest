using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YouYouTest;

public class SelectLoaderUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject selectLoadButtonSample;
    public List<SelectLoadButtonUI> selectLoadButtonUIs = new List<SelectLoadButtonUI>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GlobalEvent.OnSelectDelete.AddListener(OnSelectDeleteHandler);
    }

    private void OnSelectDeleteHandler()
    {
        //选中对象被删除后，刷新加载按钮列表
        OnEnable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        // 清空现有的按钮
        ClearExistingButtons();

        // 获取所有选中对象存档信息
        List<SelectedObjectsSaveInfo> selectedSaves = SaveLoadManager.Instance.GetAllSelectedObjectsSaves();

        // 为每个存档创建加载按钮
        foreach (SelectedObjectsSaveInfo saveInfo in selectedSaves)
        {
            CreateLoadButton(saveInfo);
        }
        
        // 清理没有对应JSON存档的多余图片
        CleanupOrphanedImages(selectedSaves);
    }
    
    /// <summary>
    /// 清空现有的按钮
    /// </summary>
    private void ClearExistingButtons()
    {
        // 清空selectLoadButtonUIs列表
        selectLoadButtonUIs.Clear();

        // 销毁contentParent下的所有子物体（除了selectLoadButtonSample）
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Transform child = contentParent.GetChild(i);
            if (child.gameObject != selectLoadButtonSample)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 创建加载按钮
    /// </summary>
    /// <param name="saveInfo">选中对象存档信息</param>
    private void CreateLoadButton(SelectedObjectsSaveInfo saveInfo)
    {
        // 实例化selectLoadButtonSample
        GameObject buttonInstance = Instantiate(selectLoadButtonSample, contentParent);

        // 添加到selectLoadButtonUIs列表
        SelectLoadButtonUI buttonUI = buttonInstance.GetComponent<SelectLoadButtonUI>();
        if (buttonUI != null)
        {
            selectLoadButtonUIs.Add(buttonUI);
            
            // 设置按钮的JsonName为文件名（不包含.json扩展名）
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(saveInfo.fileName);
            buttonUI.JsonName = fileNameWithoutExtension;
            
            // 加载对应的图片
            LoadSnapshotImage(buttonUI, fileNameWithoutExtension);
            
            // 设置按钮的ButtonName为存档名称
            buttonUI.ButtonName = saveInfo.saveName;
            
            Debug.Log($"创建加载按钮: {saveInfo.saveName} ({saveInfo.fileName})");
        }
        else
        {
            Debug.LogError("生成的按钮实例上没有找到SelectLoadButtonUI组件");
        }

        // 设置按钮为激活状态
        buttonInstance.SetActive(true);

        // 设置按钮UI组件的内容
        SetupButtonComponents(buttonInstance, saveInfo);
    }
    
    /// <summary>
    /// 设置按钮组件的内容
    /// </summary>
    /// <param name="button">按钮GameObject</param>
    /// <param name="saveInfo">选中对象存档信息</param>
    private void SetupButtonComponents(GameObject button, SelectedObjectsSaveInfo saveInfo)
    {
        // 查找TextMeshPro组件来显示存档信息
        TMPro.TextMeshProUGUI[] textComponents = button.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (var text in textComponents)
        {
            // 根据文本名称或内容来设置不同的信息
            if (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("title"))
            {
                text.text = saveInfo.saveName;
            }
            else if (text.name.ToLower().Contains("time") || text.name.ToLower().Contains("date"))
            {
                text.text = saveInfo.saveTime;
            }
            else if (text.name.ToLower().Contains("count") || text.name.ToLower().Contains("object"))
            {
                text.text = $"对象数量: {saveInfo.objectCount}";
            }
            else if (text.name.ToLower().Contains("size"))
            {
                text.text = $"文件大小: {saveInfo.fileSize} 字节";
            }
            // 如果没有特定的名称标识，使用第一个TextMeshPro组件显示存档名称
            else if (string.IsNullOrEmpty(text.text) || text.text == "New Text")
            {
                text.text = saveInfo.saveName;
            }
        }

        // 同时也查找普通Text组件（以防万一）
        UnityEngine.UI.Text[] legacyTextComponents = button.GetComponentsInChildren<UnityEngine.UI.Text>();
        foreach (var text in legacyTextComponents)
        {
            // 根据文本名称或内容来设置不同的信息
            if (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("title"))
            {
                text.text = saveInfo.saveName;
            }
            else if (text.name.ToLower().Contains("time") || text.name.ToLower().Contains("date"))
            {
                text.text = saveInfo.saveTime;
            }
            else if (text.name.ToLower().Contains("count") || text.name.ToLower().Contains("object"))
            {
                text.text = $"对象数量: {saveInfo.objectCount}";
            }
            else if (text.name.ToLower().Contains("size"))
            {
                text.text = $"文件大小: {saveInfo.fileSize} 字节";
            }
            // 如果没有特定的名称标识，使用第一个Text组件显示存档名称
            else if (string.IsNullOrEmpty(text.text) || text.text == "New Text")
            {
                text.text = saveInfo.saveName;
            }
        }
    }
    
    /// <summary>
    /// 加载对应的快照图片
    /// </summary>
    /// <param name="buttonUI">按钮UI组件</param>
    /// <param name="fileNameWithoutExtension">文件名（不包含扩展名）</param>
    private void LoadSnapshotImage(SelectLoadButtonUI buttonUI, string fileNameWithoutExtension)
    {
        try
        {
            // 构建图片文件路径（保存在SelectImages文件夹中）
            string imagePath = Path.Combine(Application.persistentDataPath, "SelectImages", fileNameWithoutExtension + ".png");
            
            // 检查文件是否存在
            if (File.Exists(imagePath))
            {
                // 读取图片数据
                byte[] imageData = File.ReadAllBytes(imagePath);
                
                // 创建Texture2D
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
                
                // 设置到Image组件
                if (buttonUI.selectUIImage != null)
                {
                    buttonUI.selectUIImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    Debug.Log($"成功加载快照图片: {imagePath}");
                }
                else
                {
                    Debug.LogWarning($"按钮 {buttonUI.ButtonName} 的selectUIImage组件为空，无法设置图片");
                }
            }
            else
            {
                Debug.LogWarning($"快照图片文件不存在: {imagePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载快照图片时出错: {e.Message}");
        }
    }
    
    /// <summary>
    /// 清理没有对应JSON存档的多余图片
    /// </summary>
    /// <param name="selectedSaves">当前存在的存档信息列表</param>
    private void CleanupOrphanedImages(List<SelectedObjectsSaveInfo> selectedSaves)
    {
        try
        {
            // 获取图片文件夹路径
            string imageFolderPath = Path.Combine(Application.persistentDataPath, "SelectImages");
            
            // 如果文件夹不存在，直接返回
            if (!Directory.Exists(imageFolderPath))
            {
                return;
            }
            
            // 获取所有有效的JSON文件名（不包含扩展名）
            HashSet<string> validFileNames = new HashSet<string>();
            foreach (var save in selectedSaves)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(save.fileName);
                validFileNames.Add(fileNameWithoutExt);
            }
            
            // 获取所有PNG文件
            string[] pngFiles = Directory.GetFiles(imageFolderPath, "*.png");
            
            int deletedCount = 0;
            foreach (string pngFile in pngFiles)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(pngFile);
                
                // 如果这个PNG文件没有对应的JSON存档，则删除
                if (!validFileNames.Contains(fileNameWithoutExt))
                {
                    File.Delete(pngFile);
                    deletedCount++;
                    Debug.Log($"删除多余的图片文件: {pngFile}");
                }
            }
            
            if (deletedCount > 0)
            {
                Debug.Log($"清理完成，共删除了 {deletedCount} 个多余的图片文件");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"清理多余图片时出错: {e.Message}");
        }
    }
}
