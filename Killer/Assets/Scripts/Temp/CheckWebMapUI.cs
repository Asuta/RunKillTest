using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class CheckWebMapUI
{
    public static void Execute()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject correctCanvas = null;
        
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == "Canvas")
            {
                if (go.transform.Find("PageContainer") != null)
                {
                    correctCanvas = go;
                    break;
                }
            }
        }

        if (correctCanvas == null)
        {
            Debug.LogError("Could not find Canvas with PageContainer.");
            return;
        }

        Debug.Log($"Found correct Canvas. Active: {correctCanvas.activeSelf}");
        
        Transform webMapUI = correctCanvas.transform.Find("PageContainer/Page_0/WebMapUI");
        if (webMapUI != null)
        {
            Debug.Log($"WebMapUI found!");
            Debug.Log($"Active Self: {webMapUI.gameObject.activeSelf}");
            Debug.Log($"Active In Hierarchy: {webMapUI.gameObject.activeInHierarchy}");
            
            RectTransform rt = webMapUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                Debug.Log($"Rect: {rt.rect}");
                Debug.Log($"Anchored Position: {rt.anchoredPosition}");
                Debug.Log($"Size Delta: {rt.sizeDelta}");
                Debug.Log($"Scale: {rt.localScale}");
            }
            
            CanvasGroup cg = webMapUI.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"CanvasGroup Alpha: {cg.alpha}");
            }

            // Check parent Page_0
            Transform page0 = webMapUI.parent;
            Debug.Log($"Page_0 Active: {page0.gameObject.activeSelf}");
            
            // Check PageContainer
            Transform pageContainer = page0.parent;
            Debug.Log($"PageContainer Active: {pageContainer.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogError("WebMapUI not found in the correct Canvas.");
        }
    }
}
