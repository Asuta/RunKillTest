using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class WebLevelSlot : MonoBehaviour
{
    public Image levelImage;
    public TMPro.TextMeshProUGUI levelText;
    public Button levelButton;
    public string levelID;
    private string serverUrl;

    public void Setup(LevelItem data, string serverUrl)
    {
        this.serverUrl = serverUrl;
        levelID = data.level_id;
        levelText.text = string.IsNullOrEmpty(data.name) ? "未命名关卡" : data.name;
        
        if (levelImage != null)
        {
            levelImage.sprite = null;
        }

        if (!string.IsNullOrEmpty(data.thumbnail_url))
        {
            string fullUrl = data.thumbnail_url;
            if (!fullUrl.StartsWith("http"))
            {
                fullUrl = serverUrl.TrimEnd('/') + (fullUrl.StartsWith("/") ? "" : "/") + fullUrl;
            }
            StartCoroutine(DownloadThumbnail(fullUrl));
        }

        levelButton.onClick.RemoveAllListeners();
        levelButton.onClick.AddListener(() => {
            Debug.Log($"点击了关卡: {levelID}，开始下载...");
            StartCoroutine(DownloadLevelRoutine(levelID));
        });
    }

    IEnumerator DownloadLevelRoutine(string id)
    {
        string saveFolder = Application.dataPath + "/YouYouTest/Test/WEBTest/SaveLevel";
        if (!System.IO.Directory.Exists(saveFolder))
        {
            System.IO.Directory.CreateDirectory(saveFolder);
        }

        // 1. 下载 JSON
        string jsonUrl = $"{serverUrl.TrimEnd('/')}/download_json/{id}";
        using (UnityWebRequest wwwJson = UnityWebRequest.Get(jsonUrl))
        {
            yield return wwwJson.SendWebRequest();
            if (wwwJson.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = wwwJson.downloadHandler.text;
                string jsonPath = System.IO.Path.Combine(saveFolder, $"{id}.json");
                System.IO.File.WriteAllText(jsonPath, jsonContent);
                Debug.Log($"<color=green>JSON已保存到: {jsonPath}</color>");
            }
            else
            {
                Debug.LogError("JSON下载失败: " + wwwJson.error);
            }
        }

        // 2. 下载 图片
        string imgUrl = $"{serverUrl.TrimEnd('/')}/static/{id}.png";
        using (UnityWebRequest wwwImg = UnityWebRequestTexture.GetTexture(imgUrl))
        {
            yield return wwwImg.SendWebRequest();
            if (wwwImg.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(wwwImg);
                string imagePath = System.IO.Path.Combine(saveFolder, $"{id}.png");
                byte[] imageBytes = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(imagePath, imageBytes);
                Debug.Log($"<color=green>图片已保存到: {imagePath}</color>");
            }
            else
            {
                Debug.LogError("图片下载失败: " + wwwImg.error);
            }
        }
    }

    IEnumerator DownloadThumbnail(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (levelImage != null)
                {
                    levelImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
    }
}
