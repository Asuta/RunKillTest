using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VInspector;
using System.Collections.Generic;

public class ConfigUIPanel : MonoBehaviour
{
    public GameObject configItemSample;
    public Transform itemParent;
    private List<GameObject> createdConfigItems = new List<GameObject>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 清除所有已创建的配置项UI
    private void ClearExistingConfigItems()
    {
        foreach (var item in createdConfigItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        createdConfigItems.Clear();
    }

    [Button("Add Config Item")]
    public void CreateConfigItem(GameObject hahah1)
    {
        // 示例用法：假设有一个敌人对象实现了 IConfigurable 接口
        Enemy enemy = hahah1.GetComponent<Enemy>();
        if (enemy != null)
        {
            CreateConfigItem(enemy);
        }
        else
        {
            Debug.LogWarning("No Enemy object found in the scene!");
        }
    }

        

    [Button("Add Config Item")]
    public void CreateConfigItem(IConfigurable configurable)
    {
        if (configurable == null)
        {
            Debug.LogWarning("Configurable object is null!");
            return;
        }

        if (configItemSample == null)
        {
            Debug.LogWarning("ConfigItemSample is not assigned!");
            return;
        }

        // 清除之前创建的配置项UI
        ClearExistingConfigItems();

        // 获取配置项列表
        var configItems = configurable.GetConfigItems();
        if (configItems == null || configItems.Count == 0)
        {
            Debug.LogWarning($"No config items found for {configurable.GetConfigTitle()}");
            return;
        }

        // 为每个配置项创建UI
        foreach (var item in configItems)
        {
            // 实例化配置项UI
            Transform parent = itemParent != null ? itemParent : transform;
            GameObject configItemGO = Instantiate(configItemSample, parent);
            ConfigItemUI configItemUI = configItemGO.GetComponent<ConfigItemUI>();
            
            if (configItemUI == null)
            {
                Debug.LogError("ConfigItemSample doesn't have ConfigItemUI component!");
                Destroy(configItemGO);
                continue;
            }

            // 设置配置项的显示名称
            configItemUI.keyText.text = item.Name;
            
            // 根据配置类型设置输入框的值和验证
            switch (item.Type)
            {
                case ConfigType.Int:
                    configItemUI.valueInputField.text = item.Value.ToString();
                    configItemUI.valueInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                    configItemUI.valueInputField.onEndEdit.AddListener((value) => {
                        if (int.TryParse(value, out int intValue))
                        {
                            item.OnValueChanged?.Invoke(intValue);
                        }
                        else
                        {
                            configItemUI.valueInputField.text = item.Value.ToString();
                            Debug.LogWarning($"Invalid integer value: {value}");
                        }
                    });
                    break;
                    
                case ConfigType.Float:
                    configItemUI.valueInputField.text = item.Value.ToString();
                    configItemUI.valueInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                    configItemUI.valueInputField.onEndEdit.AddListener((value) => {
                        if (float.TryParse(value, out float floatValue))
                        {
                            item.OnValueChanged?.Invoke(floatValue);
                        }
                        else
                        {
                            configItemUI.valueInputField.text = item.Value.ToString();
                            Debug.LogWarning($"Invalid float value: {value}");
                        }
                    });
                    break;
                    
                case ConfigType.String:
                    configItemUI.valueInputField.text = item.Value.ToString();
                    configItemUI.valueInputField.contentType = TMP_InputField.ContentType.Standard;
                    configItemUI.valueInputField.onEndEdit.AddListener((value) => {
                        item.OnValueChanged?.Invoke(value);
                    });
                    break;
                    
                case ConfigType.Bool:
                    // 对于布尔值，我们可以使用简单的文本输入（true/false）
                    configItemUI.valueInputField.text = item.Value.ToString();
                    configItemUI.valueInputField.contentType = TMP_InputField.ContentType.Standard;
                    configItemUI.valueInputField.onEndEdit.AddListener((value) => {
                        if (bool.TryParse(value, out bool boolValue))
                        {
                            item.OnValueChanged?.Invoke(boolValue);
                        }
                        else
                        {
                            configItemUI.valueInputField.text = item.Value.ToString();
                            Debug.LogWarning($"Invalid boolean value: {value}. Use 'true' or 'false'");
                        }
                    });
                    break;
            }
            
            // 激活配置项UI并添加到列表中
            configItemGO.SetActive(true);
            createdConfigItems.Add(configItemGO);
        }
        
        Debug.Log($"Created {configItems.Count} config items for {configurable.GetConfigTitle()}");
    }
}
