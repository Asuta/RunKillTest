using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using YouYouTest;


public class SelectUI : AutoCleanupBehaviour
{
    public GameObject[] delectObjects; // 改为数组支持多个对象
    public ConfigUIPanel configUIPanel;
    public GameObject configUIPanelObject;
    public Button buttonDelete;
    public Button buttonSave;
    public GameObject saveUIPanelObject;
    public Button saveConfirmButton;
    public Button saveCancelButton;
    public TMPro.TMP_InputField saveInputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 为按钮注册点击事件
        if (buttonDelete != null)
        {
            buttonDelete.onClick.AddListener(DeleteObject);
        }
        
        // 为保存按钮注册点击事件
        if (buttonSave != null)
        {
            buttonSave.onClick.AddListener(ToggleSaveUIPanel);
        }
        
        // 为保存取消按钮注册点击事件
        if (saveCancelButton != null)
        {
            saveCancelButton.onClick.AddListener(CloseSaveUIPanel);
        }
        
        // 为保存确认按钮注册点击事件
        if (saveConfirmButton != null)
        {
            saveConfirmButton.onClick.AddListener(SaveSelectedObjects);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 初始化注入方法
    public void InitializeSelectedObjects(GameObject[] selectedObjects, IConfigurable configurable = null)
    {
        delectObjects = selectedObjects;

        // 当传进来的东西没有对应的config接口，或者传进来的是多个东西的时候，让configUIPanelObject的active设置为false
        if (configUIPanelObject != null)
        {
            bool shouldShowConfigPanel = (selectedObjects.Length == 1 && configurable != null);
            configUIPanelObject.SetActive(shouldShowConfigPanel);
            
            if (shouldShowConfigPanel && configUIPanel != null)
            {
                configUIPanel.CreateConfigItem(configurable);
            }
        }
    }
    
    // 删除对象的方法
    void DeleteObject()
    {
        if (delectObjects != null && delectObjects.Length > 0)
        {
            foreach (var obj in delectObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        Destroy(this.gameObject); // 删除UI自身
    }
    
    // 切换保存UI面板的显示状态
    void ToggleSaveUIPanel()
    {
        if (saveUIPanelObject != null)
        {
            bool currentState = saveUIPanelObject.activeSelf;
            saveUIPanelObject.SetActive(!currentState);
        }
    }
    
    // 关闭保存UI面板
    void CloseSaveUIPanel()
    {
        if (saveUIPanelObject != null)
        {
            saveUIPanelObject.SetActive(false);
        }
    }
    
    // 保存选中的对象
    void SaveSelectedObjects()
    {
        // 获取存档名称，如果输入框为空则使用默认名称
        string saveName = "未命名存档";
        if (saveInputField != null && !string.IsNullOrEmpty(saveInputField.text))
        {
            saveName = saveInputField.text;
        }
        
        // 通过全局事件触发保存操作
        GlobalEvent.OnSaveSelectedObjects.Invoke(saveName);
        
        // 关闭保存UI面板
        CloseSaveUIPanel();
        
        Debug.Log($"已触发保存选中对象事件，存档名称: {saveName}");
    }
}
