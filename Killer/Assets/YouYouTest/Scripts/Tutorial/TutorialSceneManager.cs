using UnityEngine;
using YouYouTest;
using YouYouTest.VRMove2;

public class TutorialSceneManager : MonoBehaviour
{
    public TextAsset jsonFile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (jsonFile == null)
        {
            Debug.LogError("TutorialSceneManager: jsonFile 未设置，无法加载存档");
            return;
        }

        LoadLevelFromJsonAsset();
    }

    /// <summary>
    /// 从 TextAsset 加载关卡存档
    /// </summary>
    private void LoadLevelFromJsonAsset()
    {
        try
        {
            // 从 TextAsset 读取 JSON 数据
            string jsonData = jsonFile.text;

            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("TutorialSceneManager: JSON 数据为空");
                return;
            }

            // 反序列化 JSON 数据
            SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(jsonData);

            if (sceneData == null || sceneData.objects == null)
            {
                Debug.LogError("TutorialSceneManager: 无法解析 JSON 数据");
                return;
            }

            Debug.Log($"TutorialSceneManager: 开始加载关卡，共 {sceneData.objectCount} 个对象");

            // 记录当前加载的关卡名称
            if (GameManager.Instance != null)
            {
                GameManager.Instance.nowLoadLevelName = sceneData.name;
            }

            // 清理场景中的现有存档对象
            CleanupSceneBeforeLoad();

            // 加载所有对象
            int loadedCount = 0;
            foreach (ObjectSaveData objectData in sceneData.objects)
            {
                if (LoadSingleObject(objectData))
                {
                    loadedCount++;
                }
            }

            Debug.Log($"TutorialSceneManager: 成功加载 {loadedCount}/{sceneData.objectCount} 个对象");


            // 查找 VRPlayerDrag 对象并切换 UI 画布
            GameObject vrPlayerDrag = GameObject.Find("VRPlayerDrag(Clone)");
            if (vrPlayerDrag != null)
            {
                Transform uiCanvas = FindChildRecursive(vrPlayerDrag.transform, "UICanvas");
                if (uiCanvas != null)
                {
                    uiCanvas.gameObject.SetActive(false);
                }

                Transform uiCanvasTutorial = FindChildRecursive(vrPlayerDrag.transform, "UICanvas (Tutorial)");
                if (uiCanvasTutorial != null)
                {
                    uiCanvasTutorial.gameObject.SetActive(true);
                }

                NewVRMove2 vrMove = vrPlayerDrag.GetComponent<NewVRMove2>();
                if (vrMove != null)
                {
                    vrMove.enableNormalJump = false;
                    Debug.Log("TutorialSceneManager: 已关闭普通跳跃功能");
                }
            }
            else
            {
                Debug.LogWarning("TutorialSceneManager: 未找到名为 VRPlayerDrag 的对象");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TutorialSceneManager: 加载关卡时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 清理场景中的存档对象
    /// </summary>
    private void CleanupSceneBeforeLoad()
    {
        Debug.Log("TutorialSceneManager: 开始清理场景中的存档对象...");

        // 查找场景中所有实现了存档接口的对象
        var savableObjects = new System.Collections.Generic.List<MonoBehaviour>();

        MonoBehaviour[] allMonoBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in allMonoBehaviours)
        {
            if (mb is ISaveable)
            {
                savableObjects.Add(mb);
            }
        }

        Debug.Log($"TutorialSceneManager: 找到 {savableObjects.Count} 个存档对象需要清理");

        // 销毁所有存档对象
        foreach (var savable in savableObjects)
        {
            if (savable != null && savable.gameObject != null)
            {
                DestroyImmediate(savable.gameObject);
            }
        }

        // 清空命令历史
        if (YouYouTest.CommandFramework.CommandHistory.Instance != null)
        {
            YouYouTest.CommandFramework.CommandHistory.Instance.Clear();
        }

        Debug.Log("TutorialSceneManager: 场景清理完成");
    }

    /// <summary>
    /// 加载单个对象
    /// </summary>
    /// <param name="objectData">对象数据</param>
    /// <returns>是否加载成功</returns>
    private bool LoadSingleObject(ObjectSaveData objectData)
    {
        if (string.IsNullOrEmpty(objectData.prefabID))
        {
            Debug.LogWarning($"TutorialSceneManager: 对象数据的 prefabID 为空，跳过加载");
            return false;
        }

        GameObject prefab = null;

        // 尝试多种路径来加载预制体
        string[] possiblePaths = {
            objectData.prefabID,
            $"Prefabs/{objectData.prefabID}",
            $"Prefabs/EditorElement/{objectData.prefabID}",
            $"EditorElement/{objectData.prefabID}"
        };

        foreach (string path in possiblePaths)
        {
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                break;
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning($"TutorialSceneManager: 无法找到预制体: {objectData.prefabID}");
            return false;
        }

        // 实例化预制体
        GameObject newObj = Instantiate(prefab);
        newObj.name = objectData.objectName;

        // 设置 Transform
        newObj.transform.position = objectData.position;
        newObj.transform.rotation = objectData.rotation;
        newObj.transform.localScale = objectData.scale;

        // 恢复自定义组件数据
        if (!string.IsNullOrEmpty(objectData.customData))
        {
            try
            {
                SerializationHelper helper = JsonUtility.FromJson<SerializationHelper>(objectData.customData);
                if (helper != null)
                {
                    var customState = helper.ToDictionary(newObj);

                    foreach (var stateEntry in customState)
                    {
                        System.Type componentType = System.Type.GetType(stateEntry.Key);
                        if (componentType != null)
                        {
                            var component = newObj.GetComponent(componentType) as ISaveable;
                            if (component != null && stateEntry.Value != null)
                            {
                                component.RestoreState(stateEntry.Value);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TutorialSceneManager: 恢复自定义数据时出错: {e.Message}");
            }
        }

        return true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 递归查找子物体
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}
