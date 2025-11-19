using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;


public class SelectUI : MonoBehaviour
{
    public GameObject[] delectObjects; // 改为数组支持多个对象
    public ConfigUIPanel configUIPanel;
    public Button buttonDelete;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 为按钮注册点击事件
        if (buttonDelete != null)
        {
            buttonDelete.onClick.AddListener(DeleteObject);
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

        if (configUIPanel != null && configurable != null)
        {
            configUIPanel.CreateConfigItem(configurable);
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
}
