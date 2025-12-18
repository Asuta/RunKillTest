using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class DebugHierarchy
{
    public static void Execute()
    {
        Scene scene = SceneManager.GetActiveScene();
        Debug.Log($"Scene: {scene.name}, Path: {scene.path}");
        
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == "Canvas")
            {
                Debug.Log($"Found Canvas (InstanceID: {go.GetInstanceID()}). Active: {go.activeSelf}");
                Debug.Log($"  Child count: {go.transform.childCount}");
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform child = go.transform.GetChild(i);
                    if (child.name == "PageContainer")
                    {
                        Debug.Log($"  -> Found PageContainer in this Canvas!");
                    }
                }
            }
        }
    }
}
