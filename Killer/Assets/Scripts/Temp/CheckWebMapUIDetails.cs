using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class CheckWebMapUIDetails
{
    public static void Execute()
    {
        // Find the correct Canvas again
        GameObject correctCanvas = null;
        foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == "Canvas" && go.transform.Find("PageContainer") != null)
            {
                correctCanvas = go;
                break;
            }
        }

        if (correctCanvas == null) return;

        Transform webMapUI = correctCanvas.transform.Find("PageContainer/Page_0/WebMapUI");
        if (webMapUI != null)
        {
            // Check Image
            Image img = webMapUI.GetComponent<Image>();
            if (img != null)
            {
                Debug.Log($"Image found. Enabled: {img.enabled}, Color: {img.color}, Sprite: {(img.sprite != null ? img.sprite.name : "null")}");
            }
            else
            {
                Debug.Log("No Image component found.");
            }

            // Check Canvas
            Canvas c = webMapUI.GetComponent<Canvas>();
            if (c != null)
            {
                Debug.Log($"Canvas component found. Enabled: {c.enabled}");
                Debug.Log($"Render Mode: {c.renderMode}");
                Debug.Log($"Override Sorting: {c.overrideSorting}");
                Debug.Log($"Sorting Order: {c.sortingOrder}");
                Debug.Log($"Sorting Layer: {c.sortingLayerName} ({c.sortingLayerID})");
            }

            // Check CanvasRenderer
            CanvasRenderer cr = webMapUI.GetComponent<CanvasRenderer>();
            if (cr != null)
            {
                Debug.Log($"CanvasRenderer found. Cull: {cr.cull}");
                Debug.Log($"CanvasRenderer Alpha: {cr.GetAlpha()}");
            }
        }
    }
}
