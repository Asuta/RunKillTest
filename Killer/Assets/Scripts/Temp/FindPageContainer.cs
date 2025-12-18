using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class FindPageContainer
{
    public static void Execute()
    {
        // Find all objects including inactive
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            // Check if it belongs to the active scene to avoid assets
            if (t.gameObject.scene.name != SceneManager.GetActiveScene().name) continue;

            if (t.name == "PageContainer")
            {
                Debug.Log($"Found PageContainer! Path: {GetPath(t)}");
                Debug.Log($"Active Self: {t.gameObject.activeSelf}");
                Debug.Log($"Active In Hierarchy: {t.gameObject.activeInHierarchy}");
                
                // Check parent
                if (t.parent != null)
                {
                    Debug.Log($"Parent: {t.parent.name}, Active: {t.parent.gameObject.activeSelf}");
                }
                else
                {
                    Debug.Log("Parent is null (Root object)");
                }
            }
        }
    }

    static string GetPath(Transform t)
    {
        if (t.parent == null) return "/" + t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
