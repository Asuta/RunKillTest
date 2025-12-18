using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class CheckAndFixUI
{
    public static void Execute()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject canvas = null;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == "Canvas")
            {
                canvas = go;
                break;
            }
        }

        if (canvas == null)
        {
            Debug.LogError("Canvas root object not found.");
            return;
        }

        Debug.Log($"Canvas active self: {canvas.activeSelf}");
        if (!canvas.activeSelf)
        {
            canvas.SetActive(true);
            Debug.Log("Set Canvas to active.");
        }

        // Now find WebMapUI
        Transform webMapUI = canvas.transform.Find("PageContainer/Page_0/WebMapUI");
        if (webMapUI != null)
        {
            Debug.Log($"WebMapUI active self: {webMapUI.gameObject.activeSelf}");
            Debug.Log($"WebMapUI active in hierarchy: {webMapUI.gameObject.activeInHierarchy}");
            
            // Check if it has a Canvas component and if it's enabled
            Canvas c = webMapUI.GetComponent<Canvas>();
            if (c != null)
            {
                Debug.Log($"WebMapUI has Canvas component. Enabled: {c.enabled}");
            }
            
            // Check RectTransform
            RectTransform rt = webMapUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                Debug.Log($"WebMapUI Rect: {rt.rect}, Scale: {rt.localScale}");
            }
        }
        else
        {
            Debug.LogError("WebMapUI not found under Canvas.");
        }
    }
}
