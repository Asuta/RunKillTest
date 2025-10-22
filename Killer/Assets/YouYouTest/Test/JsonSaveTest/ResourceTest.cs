using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ResourceTest : MonoBehaviour
{
    [Header("资源列表设置")]
    [SerializeField] private bool autoListOnStart = true;
    [SerializeField] private bool showInGUI = false;
    
    private List<string> resourceNames = new List<string>();
    private Vector2 scrollPosition = Vector2.zero;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (autoListOnStart)
        {
            ListAllResourceFiles();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 按R键刷新资源列表
        if (Input.GetKeyDown(KeyCode.R))
        {
            ListAllResourceFiles();
        }
    }
    
    private void OnGUI()
    {
        if (!showInGUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("Resources文件夹内容", GUI.skin.box);
        
        if (GUILayout.Button("刷新资源列表"))
        {
            ListAllResourceFiles();
        }
        
        if (GUILayout.Button("列出所有AudioClip"))
        {
            ListResourcesByType<AudioClip>();
        }
        
        if (GUILayout.Button("列出所有Texture2D"))
        {
            ListResourcesByType<Texture2D>();
        }
        
        GUILayout.Label($"找到 {resourceNames.Count} 个资源:");
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        foreach (string name in resourceNames)
        {
            GUILayout.Label(name);
        }
        
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>
    /// 遍历Resources文件夹中的所有文件并打印文件名
    /// </summary>
    void ListAllResourceFiles()
    {
        Debug.Log("开始遍历Resources文件夹中的所有文件...");
        resourceNames.Clear();
        
        // 方法1：使用Resources.LoadAll加载所有资源
        object[] allResources = Resources.LoadAll("");
        Debug.Log($"方法1 - Resources.LoadAll 找到 {allResources.Length} 个资源:");
        
        foreach (var resource in allResources)
        {
            if (resource != null)
            {
                // 将resource转换为Object以访问name属性
                Object unityResource = resource as Object;
                string resourceName = unityResource != null ? unityResource.name : "未知资源";
                string resourceInfo = $"资源名称: {resource.GetType().Name} - {resourceName}";
                Debug.Log(resourceInfo);
                resourceNames.Add(resourceInfo);
            }
        }

        // 方法2：使用Directory.GetFiles直接访问文件系统（仅在编辑器中有效）
        #if UNITY_EDITOR
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        if (Directory.Exists(resourcesPath))
        {
            string[] allFiles = Directory.GetFiles(resourcesPath, "*", SearchOption.AllDirectories);
            Debug.Log($"方法2 - Directory.GetFiles 找到 {allFiles.Length} 个文件:");
            
            foreach (string filePath in allFiles)
            {
                // 获取相对于Resources文件夹的路径
                string relativePath = filePath.Substring(resourcesPath.Length + 1);
                // 移除.meta文件
                if (!relativePath.EndsWith(".meta"))
                {
                    string fileInfo = $"文件路径: {relativePath}";
                    Debug.Log(fileInfo);
                    resourceNames.Add(fileInfo);
                }
            }
        }
        else
        {
            Debug.LogError("Resources文件夹不存在!");
        }
        #endif
        
        Debug.Log($"遍历完成! 总共找到 {resourceNames.Count} 个资源/文件");
    }
    
    /// <summary>
    /// 列出指定类型的所有资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    void ListResourcesByType<T>() where T : Object
    {
        Debug.Log($"开始遍历类型为 {typeof(T).Name} 的资源...");
        resourceNames.Clear();
        
        T[] resources = Resources.LoadAll<T>("");
        Debug.Log($"找到 {resources.Length} 个 {typeof(T).Name} 资源:");
        
        foreach (T resource in resources)
        {
            if (resource != null)
            {
                string resourceInfo = $"{typeof(T).Name}: {resource.name}";
                Debug.Log(resourceInfo);
                resourceNames.Add(resourceInfo);
            }
        }
        
        Debug.Log($"遍历完成! 总共找到 {resources.Length} 个 {typeof(T).Name} 资源");
    }
}
