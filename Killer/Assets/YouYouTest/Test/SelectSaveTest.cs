using UnityEngine;
using YouYouTest;
using System.Collections.Generic;

public class SelectSaveTest : MonoBehaviour
{
    private EditorPlayer editorPlayer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取EditorPlayer实例
        editorPlayer = FindObjectOfType<EditorPlayer>();
        if (editorPlayer == null)
        {
            Debug.LogError("未找到EditorPlayer实例");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 按下空格键时记录选中的对象
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LogSelectedObjects();
        }
    }
    
    /// <summary>
    /// 获取并记录EditorPlayer中所有正在被select的对象
    /// </summary>
    public void LogSelectedObjects()
    {
        if (editorPlayer == null)
        {
            Debug.LogError("EditorPlayer实例为空，无法获取选中对象");
            return;
        }
        
        // 使用反射获取私有的GetSelectedObjects方法
        var getSelectedObjectsMethod = typeof(EditorPlayer).GetMethod("GetSelectedObjects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (getSelectedObjectsMethod != null)
        {
            var selectedObjects = getSelectedObjectsMethod.Invoke(editorPlayer, null) as GameObject[];
            
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                Debug.Log($"当前选中的对象数量: {selectedObjects.Length}");
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    if (selectedObjects[i] != null)
                    {
                        Debug.Log($"选中对象 {i + 1}: {selectedObjects[i].name} (位置: {selectedObjects[i].transform.position})");
                    }
                }
            }
            else
            {
                Debug.Log("当前没有选中的对象");
            }
        }
        else
        {
            Debug.LogError("无法找到EditorPlayer的GetSelectedObjects方法");
        }
    }
}
