using UnityEngine;
using YouYouTest;
using System.Collections.Generic;
using VInspector;

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
    /// 获取并记录EditorPlayer中所有正在被select的对象，并在它们的中心位置生成一个立方体
    /// </summary>
    [Button("记录选中对象")]
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
                
                Vector3 centerPosition = Vector3.zero;
                int validObjectCount = 0;
                
                // 记录每个选中对象的信息并计算中心位置
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    if (selectedObjects[i] != null)
                    {
                        Debug.Log($"选中对象 {i + 1}: {selectedObjects[i].name} (位置: {selectedObjects[i].transform.position})");
                        centerPosition += selectedObjects[i].transform.position;
                        validObjectCount++;
                    }
                }
                
                // 计算中心位置
                if (validObjectCount > 0)
                {
                    centerPosition /= validObjectCount;
                    Debug.Log($"所有选中对象的中心位置: {centerPosition}");
                    
                    // 在中心位置生成一个立方体
                    CreateCubeAtCenter(centerPosition);
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
    
    /// <summary>
    /// 在指定位置创建一个立方体
    /// </summary>
    /// <param name="position">立方体的位置</param>
    private void CreateCubeAtCenter(Vector3 position)
    {
        // 创建一个新的立方体
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "SelectObjectsCenterCube";
        cube.transform.position = position;
        
        // 设置立方体的大小（可选）
        cube.transform.localScale = Vector3.one * 0.2f;
        
        // 设置立方体的颜色为黄色以便识别
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }
        
        Debug.Log($"已在位置 {position} 创建中心立方体");
    }
}
