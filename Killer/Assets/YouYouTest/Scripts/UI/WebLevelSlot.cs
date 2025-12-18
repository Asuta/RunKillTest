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

    public void Setup(LevelItem data, string serverUrl)
    {
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
            Debug.Log($"点击了关卡: {levelID}");
        });
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
