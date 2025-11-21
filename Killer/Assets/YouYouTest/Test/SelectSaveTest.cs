using UnityEngine;
using YouYouTest;
using VInspector;

public class SelectSaveTest : MonoBehaviour
{
    #region 字段和属性
    private EditorPlayer editorPlayer;
    #endregion

    #region Unity生命周期
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
    #endregion

    #region 加载选中对象
    [Button("读取第一个select存档")]
    public void LoadFirstSelectInfo(Transform createPosition)
    {
        SaveLoadManager.Instance.LoadFirstSelectedObjects(createPosition);
    }
    
    /// <summary>
    /// 根据JSON文件名加载选中对象到指定位置
    /// </summary>
    /// <param name="jsonFileName">JSON文件名</param>
    /// <param name="createPosition">创建位置</param>
    public void LoadSelectedObjectsByJsonName(string jsonFileName, Transform createPosition)
    {
        SaveLoadManager.Instance.LoadSelectedObjectsByFileName(jsonFileName, createPosition);
    }
    #endregion

    #region 记录和显示选中对象
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

        // 直接调用公共的GetSelectedObjects方法
        var selectedObjects = editorPlayer.GetSelectedObjects();

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
    #endregion

    #region 保存选中对象
    /// <summary>
    /// 保存当前选中的对象到专门的选中对象存档文件夹
    /// </summary>
    [Button("保存选中对象(没有名字)")]
    public void SaveSelectedObjects()
    {
        SaveSelectedObjectsWithCustomName("未命名存档");
    }

    /// <summary>
    /// 保存当前选中的对象到专门的选中对象存档文件夹，并指定存档名字
    /// </summary>
    /// <param name="saveName">存档名字（与JSON文件名不同）</param>
    [Button("保存选中对象并指定存档名")]
    public void SaveSelectedObjectsWithCustomName(string saveName)
    {
        if (editorPlayer == null)
        {
            Debug.LogError("EditorPlayer实例为空，无法获取选中对象");
            return;
        }

        // 直接调用公共的GetSelectedObjects方法
        var selectedObjects = editorPlayer.GetSelectedObjects();
            SaveLoadManager.Instance.SaveSelectedObjects(selectedObjects, saveName);
    }
    #endregion
}
