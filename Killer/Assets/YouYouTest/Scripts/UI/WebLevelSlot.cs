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

    public void Setup(LevelItem data)
    {
        // 处理空数据情况
        if (data == null)
        {
            levelID = "";
            if (levelText != null) levelText.text = "";
            if (levelImage != null) levelImage.sprite = null;
            if (levelButton != null)
            {
                levelButton.onClick.RemoveAllListeners();
                // 禁用按钮或设置为空点击
                levelButton.interactable = false;
                
                // 清除按钮文本
                TMPro.TextMeshProUGUI buttonText = levelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "";
                else
                {
                    Text legacyText = levelButton.GetComponentInChildren<Text>();
                    if (legacyText != null) legacyText.text = "";
                }
            }
            return;
        }

        if (levelButton != null) levelButton.interactable = true;
        levelID = data.level_id;
        string displayName = string.IsNullOrEmpty(data.name) ? "未命名关卡" : data.name;
        levelText.text = UnityWebRequest.UnEscapeURL(displayName);
        
        if (levelImage != null)
        {
            levelImage.sprite = null;
        }

        if (!string.IsNullOrEmpty(data.thumbnail_url))
        {
            string fullUrl = data.thumbnail_url;
            if (!fullUrl.StartsWith("http"))
            {
                fullUrl = SaveNetworkManager.Instance.ServerUrl.TrimEnd('/') + (fullUrl.StartsWith("/") ? "" : "/") + fullUrl;
            }
            SaveNetworkManager.Instance.DownloadThumbnail(fullUrl, (sprite) => {
                if (levelImage != null)
                {
                    levelImage.sprite = sprite;
                    if (sprite == null)
                    {
                        Debug.LogWarning($"关卡 {levelID} ({displayName}) 的缩略图加载失败，请检查服务器资源。");
                    }
                }
            });
        }

        // 检查本地是否已经下载过该关卡 (网络关卡存放在 WebSaveData)
        string saveFolder = Path.Combine(SaveLoadManager.Instance.GetUserFolderPath(), "WebSaveData");
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
            // 恢复按钮文本为默认（可能是 "Download"）
            TMPro.TextMeshProUGUI buttonText = levelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Download";

            levelButton.onClick.AddListener(() => {
                Debug.Log($"点击了关卡: {levelID}，开始下载...");
                SaveNetworkManager.Instance.DownloadLevel(levelID, (success) => {
                    if (success)
                    {
                        UpdateToPlayState();
                    }
                });
            });
        }
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
}
