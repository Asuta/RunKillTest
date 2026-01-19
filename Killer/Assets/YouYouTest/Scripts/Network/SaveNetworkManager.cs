using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;

[System.Serializable]
public class LevelItem
{
    public string tid;
    public string uid;
    public long p_size;
    public long j_size;
    public long save_time;
    public int ob_num;
    public string name;
    public string desc;

    // 兼容旧代码的属性
    public string level_id => tid;
    public int object_count => ob_num;
    public string thumbnail_url => "get_image?id=" + tid; 
}

[System.Serializable]
public class LevelListResponse
{
    public int total_num;
    public LevelItem[] tasks;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}

public class SaveNetworkManager : MonoBehaviour
{
    private static SaveNetworkManager _instance;
    public static SaveNetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveNetworkManager");
                _instance = go.AddComponent<SaveNetworkManager>();
                // DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// 上传存档到服务器
    /// </summary>
    public void UploadLevel(SaveSlotInfo slotInfo, string serverUrl, string description = "", UnityAction<bool, string> callback = null)
    {
        string userFolderPath = SaveLoadManager.Instance.GetUserFolderPath();
        
        // 1. 准备 JSON 文件
        string jsonPath = Path.GetFullPath(Path.Combine(userFolderPath, slotInfo.subFolder, slotInfo.fileName));
        if (!File.Exists(jsonPath))
        {
            string error = $"上传失败，找不到JSON文件: {jsonPath}";
            Debug.LogError(error);
            callback?.Invoke(false, error);
            return;
        }
        byte[] jsonBytes = File.ReadAllBytes(jsonPath);

        // 2. 准备图片文件
        string imageFileName = slotInfo.fileName.Replace(".json", ".png");
        string imagePath = Path.GetFullPath(Path.Combine(userFolderPath, slotInfo.subFolder, imageFileName));

        if (!File.Exists(imagePath) && imageFileName.EndsWith("_SceneObjects.png"))
        {
            string fallbackFileName = imageFileName.Replace("_SceneObjects.png", ".png");
            string fallbackPath = Path.GetFullPath(Path.Combine(userFolderPath, slotInfo.subFolder, fallbackFileName));
            if (File.Exists(fallbackPath))
            {
                imagePath = fallbackPath;
            }
        }

        if (!File.Exists(imagePath))
        {
            string error = $"上传失败，找不到图片文件: {imagePath}";
            Debug.LogError(error);
            callback?.Invoke(false, error);
            return;
        }
        byte[] imageBytes = File.ReadAllBytes(imagePath);

        UploadLevelRaw(slotInfo.LevelName, slotInfo.fileName, jsonBytes, Path.GetFileName(imagePath), imageBytes, serverUrl, slotInfo.objectCount, description, callback);
    }

    /// <summary>
    /// 通用上传接口
    /// </summary>
    public void UploadLevelRaw(string levelName, string jsonFileName, byte[] jsonBytes, string imageFileName, byte[] imageBytes, string serverUrl, int objectCount, string description, UnityAction<bool, string> callback = null)
    {
        StartCoroutine(UploadRawRoutine(levelName, jsonFileName, jsonBytes, imageFileName, imageBytes, serverUrl, objectCount, description, callback));
    }

    private IEnumerator UploadRawRoutine(string levelName, string jsonFileName, byte[] jsonBytes, string imageFileName, byte[] imageBytes, string serverUrl, int objectCount, string description, UnityAction<bool, string> callback)
    {
        // 1. 构建 URL 和参数
        string uid = DeviceIDManager.GetDeviceID();
        int pLength = imageBytes.Length;
        int jLength = jsonBytes.Length;
        string encodedName = UnityWebRequest.EscapeURL(levelName);
        string encodedDesc = UnityWebRequest.EscapeURL(description);

        // 接口格式: /up?uid=%s&PLength=%d&JLength=%d&Name=%s&ObNum=%d&Desc=%s
        string url = string.Format("{0}/up?uid={1}&PLength={2}&JLength={3}&Name={4}&ObNum={5}&Desc={6}",
            serverUrl.TrimEnd('/'), uid, pLength, jLength, encodedName, objectCount, encodedDesc);

        // 2. 构建二进制包体：Json 字节流 + 图片字节流
        byte[] bodyData = new byte[jsonBytes.Length + imageBytes.Length];
        System.Buffer.BlockCopy(jsonBytes, 0, bodyData, 0, jsonBytes.Length);
        System.Buffer.BlockCopy(imageBytes, 0, bodyData, jsonBytes.Length, imageBytes.Length);

        // 3. 发送 POST 请求
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyData);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"上传失败: {www.error}");
                callback?.Invoke(false, www.error);
            }
            else
            {
                Debug.Log($"<color=green>上传成功!</color> 服务器返回: {www.downloadHandler.text}");
                callback?.Invoke(true, www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// 获取服务器关卡列表
    /// </summary>
    public void GetLevelList(string serverUrl, int page, int pageSize, UnityAction<LevelListResponse> callback)
    {
        StartCoroutine(GetListRoutine(serverUrl, page, pageSize, callback));
    }

    private IEnumerator GetListRoutine(string serverUrl, int page, int pageSize, UnityAction<LevelListResponse> callback)
    {
        string url = $"{serverUrl.TrimEnd('/')}/get_all_tasks/?page={page}&size={pageSize}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("获取列表失败: " + www.error);
                callback?.Invoke(null);
                yield break;
            }

            string jsonString = www.downloadHandler.text;
            LevelListResponse response = null;
            try
            {
                response = JsonUtility.FromJson<LevelListResponse>(jsonString);
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON解析出错: " + e.Message);
            }
            callback?.Invoke(response);
        }
    }

    /// <summary>
    /// 下载关卡到本地
    /// </summary>
    public void DownloadLevel(string serverUrl, string levelId, UnityAction<bool> callback = null)
    {
        StartCoroutine(DownloadLevelRoutine(serverUrl, levelId, callback));
    }

    private IEnumerator DownloadLevelRoutine(string serverUrl, string id, UnityAction<bool> callback)
    {
        string saveFolder = Path.Combine(Application.dataPath, "..", "UserSaveData", "WebSaveData");
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        bool jsonSuccess = false;
        // 1. 下载 JSON
        string jsonUrl = $"{serverUrl.TrimEnd('/')}/get_json?id={id}";
        using (UnityWebRequest wwwJson = UnityWebRequest.Get(jsonUrl))
        {
            yield return wwwJson.SendWebRequest();
            if (wwwJson.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = wwwJson.downloadHandler.text;
                string jsonPath = Path.Combine(saveFolder, $"{id}_SceneObjects.json");
                File.WriteAllText(jsonPath, jsonContent);
                Debug.Log($"<color=cyan>JSON 已保存至: {Path.GetFullPath(jsonPath)}</color>");
                jsonSuccess = true;
            }
            else
            {
                Debug.LogError("JSON下载失败: " + wwwJson.error);
            }
        }

        if (!jsonSuccess)
        {
            callback?.Invoke(false);
            yield break;
        }

        // 2. 下载 图片
        string imgUrl = $"{serverUrl.TrimEnd('/')}/get_image?id={id}";
        using (UnityWebRequest wwwImg = UnityWebRequestTexture.GetTexture(imgUrl))
        {
            yield return wwwImg.SendWebRequest();
            if (wwwImg.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(wwwImg);
                string imagePath = Path.Combine(saveFolder, $"{id}.png");
                byte[] imageBytes = texture.EncodeToPNG();
                File.WriteAllBytes(imagePath, imageBytes);
                Debug.Log($"<color=cyan>图片 已保存至: {Path.GetFullPath(imagePath)}</color>");
            }
        }

        callback?.Invoke(true);
    }

    /// <summary>
    /// 下载缩略图
    /// </summary>
    public void DownloadThumbnail(string url, UnityAction<Sprite> callback)
    {
        StartCoroutine(DownloadThumbnailRoutine(url, callback));
    }

    private IEnumerator DownloadThumbnailRoutine(string url, UnityAction<Sprite> callback)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                callback?.Invoke(sprite);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }
}
