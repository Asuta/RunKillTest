using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using System.Security.Cryptography;
using System.Text;

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
    public string ServerUrl = "http://akashic.funshion.com:8080";
    public string PrivateKey = "M8^cV1*nJ4";

    private ulong GetTimestamp()
    {
        // 毫秒级时间戳 + 5分钟 (300,000 毫秒)
        return (ulong)(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 300000);
    }

    private string GenerateToken(ulong ts, string parameter)
    {
        // Token 生成规则: MD5(TS + PrivateKey + Parameter)
        string input = ts.ToString() + PrivateKey + parameter;
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }

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
    public void UploadLevel(SaveSlotInfo slotInfo, string description = "", UnityAction<bool, string> callback = null)
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

        UploadLevelRaw(slotInfo.LevelName, slotInfo.fileName, jsonBytes, Path.GetFileName(imagePath), imageBytes, slotInfo.objectCount, description, callback);
    }

    /// <summary>
    /// 通用上传接口
    /// </summary>
    public void UploadLevelRaw(string levelName, string jsonFileName, byte[] jsonBytes, string imageFileName, byte[] imageBytes, int objectCount, string description, UnityAction<bool, string> callback = null)
    {
        StartCoroutine(UploadRawRoutine(levelName, jsonFileName, jsonBytes, imageFileName, imageBytes, objectCount, description, callback));
    }

    private IEnumerator UploadRawRoutine(string levelName, string jsonFileName, byte[] jsonBytes, string imageFileName, byte[] imageBytes, int objectCount, string description, UnityAction<bool, string> callback)
    {
        // 1. 构建 URL 和参数
        string uid = DeviceIDManager.GetDeviceID();
        int pLength = imageBytes.Length;
        int jLength = jsonBytes.Length;
        string encodedName = UnityWebRequest.EscapeURL(levelName).Replace("+", "%20");
        string encodedDesc = UnityWebRequest.EscapeURL(description).Replace("+", "%20");

        ulong ts = GetTimestamp();
        string token = GenerateToken(ts, uid);

        // 接口格式: /up?uid=%s&PLength=%d&JLength=%d&Name=%s&ObNum=%d&Desc=%s&ts=%llu&token=%s
        string url = string.Format("{0}/up?uid={1}&PLength={2}&JLength={3}&Name={4}&ObNum={5}&Desc={6}&ts={7}&token={8}",
            ServerUrl.TrimEnd('/'), uid, pLength, jLength, encodedName, objectCount, encodedDesc, ts, token);

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
    public void GetLevelList(int page, int pageSize, UnityAction<LevelListResponse> callback)
    {
        StartCoroutine(GetListRoutine(page, pageSize, callback));
    }

    private IEnumerator GetListRoutine(int page, int pageSize, UnityAction<LevelListResponse> callback)
    {
        ulong ts = GetTimestamp();
        string token = GenerateToken(ts, page.ToString());
        string url = $"{ServerUrl.TrimEnd('/')}/get_all_tasks/?page={page}&size={pageSize}&ts={ts}&token={token}";
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
    public void DownloadLevel(string levelId, UnityAction<bool> callback = null)
    {
        StartCoroutine(DownloadLevelRoutine(levelId, callback));
    }

    private IEnumerator DownloadLevelRoutine(string id, UnityAction<bool> callback)
    {
        string saveFolder = Path.Combine(SaveLoadManager.Instance.GetUserFolderPath(), "WebSaveData");
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        bool jsonSuccess = false;
        ulong ts = GetTimestamp();
        string token = GenerateToken(ts, id);

        // 1. 下载 JSON
        string jsonUrl = $"{ServerUrl.TrimEnd('/')}/get_json?id={id}&ts={ts}&token={token}";
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
        string imgUrl = $"{ServerUrl.TrimEnd('/')}/get_image?id={id}&ts={ts}&token={token}";
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

    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, List<UnityAction<Sprite>>> pendingRequests = new Dictionary<string, List<UnityAction<Sprite>>>();

    /// <summary>
    /// 下载缩略图
    /// </summary>
    public void DownloadThumbnail(string url, UnityAction<Sprite> callback)
    {
        if (spriteCache.ContainsKey(url))
        {
            callback?.Invoke(spriteCache[url]);
            return;
        }

        if (pendingRequests.ContainsKey(url))
        {
            pendingRequests[url].Add(callback);
            return;
        }

        pendingRequests[url] = new List<UnityAction<Sprite>> { callback };
        StartCoroutine(DownloadThumbnailRoutine(url));
    }

    private IEnumerator DownloadThumbnailRoutine(string url)
    {
        string finalUrl = url;
        if (!finalUrl.StartsWith("http"))
        {
            finalUrl = $"{ServerUrl.TrimEnd('/')}/{finalUrl.TrimStart('/')}";
        }

        // 提取 ID 用于生成 Token
        string id = "";
        if (finalUrl.Contains("id="))
        {
            int startIndex = finalUrl.IndexOf("id=") + 3;
            int endIndex = finalUrl.IndexOf('&', startIndex);
            if (endIndex == -1) endIndex = finalUrl.Length;
            id = finalUrl.Substring(startIndex, endIndex - startIndex);
        }

        ulong ts = GetTimestamp();
        string token = GenerateToken(ts, id);

        if (finalUrl.Contains("?"))
            finalUrl += $"&ts={ts}&token={token}";
        else
            finalUrl += $"?ts={ts}&token={token}";

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(finalUrl))
        {
            www.timeout = 5; // 增加到 5 秒超时
            yield return www.SendWebRequest();

            Sprite resultSprite = null;
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (texture != null && texture.width > 2 && texture.height > 2)
                {
                    resultSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    spriteCache[url] = resultSprite;
                }
                else
                {
                    Debug.LogError($"下载成功但纹理无效: {url}");
                }
            }
            else
            {
                // 详细记录错误类型和状态码
                Debug.LogError($"下载缩略图失败! URL: {url}, Result: {www.result}, HttpCode: {www.responseCode}, Error: {www.error}");
            }

            if (pendingRequests.TryGetValue(url, out var callbacks))
            {
                foreach (var cb in callbacks)
                {
                    cb?.Invoke(resultSprite);
                }
                pendingRequests.Remove(url);
            }
        }
    }
}
