using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

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

        // 检查本地是否已经下载过该关卡 (网络关卡存放在 WebSaveData)
        string saveFolder = Path.Combine(Application.dataPath, "..", "UserSaveData", "WebSaveData");
        string jsonPath = Path.Combine(saveFolder, $"{levelID}_SceneObjects.json");

        if (File.Exists(jsonPath))
        {
            // 如果已存在，直接设置为 Play 状态
            UpdateToPlayState();
        }
        else
        {
            // 如果不存在，设置为下载逻辑
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(() => {
                Debug.Log($"点击了关卡: {levelID}，开始下载...");
                StartCoroutine(DownloadLevelRoutine(levelID));
            });
        }
    }

    IEnumerator DownloadLevelRoutine(string id)
    {
        // 网络下载的关卡存放在 WebSaveData 子文件夹
        string saveFolder = Path.Combine(Application.dataPath, "..", "UserSaveData", "WebSaveData");
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // 1. 下载 JSON
        string jsonUrl = $"{serverUrl.TrimEnd('/')}/download_json/{id}";
        using (UnityWebRequest wwwJson = UnityWebRequest.Get(jsonUrl))
        {
            yield return wwwJson.SendWebRequest();
            if (wwwJson.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = wwwJson.downloadHandler.text;
                // 关键：文件名必须符合 SaveLoadManager 的 GenerateSaveFileName 规则 (slotName + "_SceneObjects.json")
                string jsonPath = Path.Combine(saveFolder, $"{id}_SceneObjects.json");
                File.WriteAllText(jsonPath, jsonContent);
                Debug.Log($"<color=green>JSON已保存到存档目录: {jsonPath}</color>");
            }
            else
            {
                Debug.LogError("JSON下载失败: " + wwwJson.error);
                yield break; // 下载失败则不继续
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
                string imagePath = Path.Combine(saveFolder, $"{id}.png");
                byte[] imageBytes = texture.EncodeToPNG();
                File.WriteAllBytes(imagePath, imageBytes);
                Debug.Log($"<color=green>图片已保存到存档目录: {imagePath}</color>");
            }
            else
            {
                Debug.LogWarning("图片下载失败（非致命）: " + wwwImg.error);
            }
        }

        // 下载完成后，修改按钮逻辑为“进入场景”
        UpdateToPlayState();
    }

    private void UpdateToPlayState()
    {
        // 修改按钮文本为 "Play"
        TMPro.TextMeshProUGUI buttonText = levelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "Play";
        }
        else
        {
            // 兼容普通 Text 组件
            Text legacyText = levelButton.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = "Play";
        }

        // 切换按钮点击事件
        levelButton.onClick.RemoveAllListeners();
        levelButton.onClick.AddListener(() => OnPlayButtonClicked(levelID));
    }

    private void OnPlayButtonClicked(string slotName)
    {
        Debug.Log($"准备进入场景并加载存档: {slotName}");

        // Web 关卡必须从 WebSaveData 精确加载，避免与本地同名副本产生二义性
        string webJsonFileName = $"{slotName}_SceneObjects.json";

        // 设置 GameManager 中的存档名
        if (GameManager.Instance != null)
        {
            GameManager.Instance.nowLoadSaveSlot = slotName;
        }

        // 如果已经在 KillScene，直接加载
        if (SceneManager.GetActiveScene().name == "KillScene")
        {
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadSceneObjectsByFileName(webJsonFileName, "WebSaveData");
                if (GameManager.Instance != null) GameManager.Instance.SetCanSwitchMode(true);
                GlobalEvent.OnLoadSaveChange.Invoke(SaveLoadManager.ComposeSaveKey("WebSaveData", slotName));
            }
            return;
        }

        // 不在 KillScene 时，注册加载完成回调并切换场景
        UnityAction<Scene, LoadSceneMode> onLoaded = null;
        onLoaded = (scene, mode) =>
        {
            if (scene.name == "KillScene")
            {
                SceneManager.sceneLoaded -= onLoaded;
                if (SaveLoadManager.Instance != null)
                {
                    SaveLoadManager.Instance.LoadSceneObjectsByFileName(webJsonFileName, "WebSaveData");
                    GlobalEvent.OnLoadSaveChange.Invoke(SaveLoadManager.ComposeSaveKey("WebSaveData", slotName));
                }
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetCanSwitchMode(true);
                    GameManager.Instance.SetPlayMode(false);
                }
            }
        };

        SceneManager.sceneLoaded += onLoaded;
        SceneManager.LoadScene("KillScene");
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
