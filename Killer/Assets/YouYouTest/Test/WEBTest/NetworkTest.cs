using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using UnityEngine.UI;

// 定义一个简单的类来接收服务器返回的列表数据
[System.Serializable]
public class LevelItem
{
    public string level_id;
    public string json_url;
    public string thumbnail_url;
}

// 帮助类：用来解析JSON数组 (因为JsonUtility默认不支持根节点是数组)
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

public class NetworkTest : MonoBehaviour
{
    [Header("1. 服务器设置")]
    public string serverUrl = "http://127.0.0.1:8000";

    [Header("2. 上传测试数据")]
    public string levelName = "我的测试关卡";
    public TextAsset uploadJson; // 拖入你的JSON文件
    public Texture2D uploadImage; // 拖入你的缩略图 (记得开启Read/Write)

    [Header("3. 下载测试参数")]
    public string testDownloadId; // 从获取列表的结果里复制一个ID填到这里

    [Header("4. 分页设置")]
    public int currentPage = 1; // 当前页码
    public int pageSize = 10; // 每页条目数

    [Header("5. ImageList")]
    public Image[] imageList; // 用于存放下载下来的图片

    // ---------------------------------------------------------
    // 右键点击组件标题，选择 "1. Test Upload" 即可运行
    // ---------------------------------------------------------
    
    [ContextMenu("1. Test Upload (上传)")]
    public void TestUpload()
    {
        if (uploadJson == null || uploadImage == null)
        {
            Debug.LogError("请先在Inspector面板拖入 Json文件 和 图片！");
            return;
        }
        StartCoroutine(UploadRoutine());
    }

    [ContextMenu("2. Test Get List (获取列表)")]
    public void TestGetList()
    {
        StartCoroutine(GetListRoutine());
    }

    [ContextMenu("2.1 Test Get List (获取指定页)")]
    public void TestGetListWithParams()
    {
        StartCoroutine(GetListRoutine(currentPage, pageSize));
    }

    [ContextMenu("2.2 Test Get List (获取第1页)")]
    public void TestGetListPage1()
    {
        StartCoroutine(GetListRoutine(1, 10));
    }

    [ContextMenu("2.3 Test Get List (获取第2页)")]
    public void TestGetListPage2()
    {
        StartCoroutine(GetListRoutine(2, 10));
    }

    [ContextMenu("2.4 Test Get List (获取第3页)")]
    public void TestGetListPage3()
    {
        StartCoroutine(GetListRoutine(3, 10));
    }

    [ContextMenu("3. Test Download (下载指定ID)")]
    public void TestDownload()
    {
        if (string.IsNullOrEmpty(testDownloadId))
        {
            Debug.LogError("请先在Inspector里填入 testDownloadId");
            return;
        }
        StartCoroutine(DownloadRoutine(testDownloadId));
    }

    // --- 具体实现协程 ---

    IEnumerator UploadRoutine()
    {
        // 1. 准备表单
        WWWForm form = new WWWForm();
        
        // 添加普通字段 (名字)
        form.AddField("name", levelName);

        // 添加文件1: JSON (从TextAsset读取字节)
        // 参数: 字段名(服务端对应), 数据, 文件名, MimeType
        form.AddBinaryData("json_file", uploadJson.bytes, "level.json", "application/json");

        // 添加文件2: 图片 (将Texture转为PNG字节流)
        byte[] imageBytes = uploadImage.EncodeToPNG();
        if (imageBytes == null)
        {
            Debug.LogError("图片转换失败！请检查图片的 Import Settings 是否开启了 Read/Write。");
            yield break;
        }
        form.AddBinaryData("image_file", imageBytes, "thumb.png", "image/png");

        // 2. 发送请求
        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl + "/upload_level/", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("上传失败: " + www.error);
            }
            else
            {
                Debug.Log("上传成功! 服务器返回: " + www.downloadHandler.text);
            }
        }
    }

    IEnumerator GetListRoutine()
    {
        StartCoroutine(GetListRoutine(1, 10));
        yield break;
    }

    IEnumerator GetListRoutine(int page, int pageSize)
    {
        string url = serverUrl + $"/get_levels/?page={page}&page_size={pageSize}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("获取列表失败: " + www.error);
                yield break;
            }

            string jsonString = www.downloadHandler.text;
            Debug.Log("获取列表原始JSON: " + jsonString);

            LevelItem[] levels = null;
            
            // 尝试解析JSON
            try
            {
                levels = JsonHelper.FromJson<LevelItem>(jsonString);
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON解析出错: " + e.Message);
                yield break;
            }

            if (levels == null || levels.Length == 0)
            {
                Debug.LogWarning("列表是空的，请先上传一个关卡。");
                yield break;
            }

            Debug.Log($"解析成功! 找到了 {levels.Length} 个关卡。");
            Debug.Log($"<color=green>建议测试用的 ID: {levels[0].level_id}</color> (已为你自动填入Inspector)");
            
            // 自动帮你填入下载测试的ID框，方便你马上测下一步
            testDownloadId = levels[0].level_id;
            
            // 开始批量下载缩略图
            yield return StartCoroutine(DownloadThumbnailsRoutine(levels));
        }
    }

    IEnumerator DownloadThumbnailsRoutine(LevelItem[] levels)
    {
        Debug.Log($"开始下载 {levels.Length} 个缩略图...");
        
        // 确保imageList数组有足够的空间
        if (imageList == null || imageList.Length < levels.Length)
        {
            Debug.LogWarning($"imageList数组大小不足。当前大小: {(imageList == null ? 0 : imageList.Length)}, 需要: {levels.Length}");
            yield break;
        }

        // 逐个下载缩略图
        for (int i = 0; i < levels.Length && i < imageList.Length; i++)
        {
            string thumbnailUrl = levels[i].thumbnail_url;
            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                Debug.LogWarning($"关卡 {levels[i].level_id} 没有缩略图URL");
                continue;
            }

            // 检查URL是否是完整的，如果不是则添加服务器地址
            if (!thumbnailUrl.StartsWith("http://") && !thumbnailUrl.StartsWith("https://"))
            {
                // 如果URL以/开头，去掉多余的/
                if (thumbnailUrl.StartsWith("/"))
                {
                    thumbnailUrl = serverUrl + thumbnailUrl;
                }
                else
                {
                    thumbnailUrl = serverUrl + "/" + thumbnailUrl;
                }
            }

            Debug.Log($"正在下载缩略图 {i + 1}/{levels.Length}: {thumbnailUrl}");
            
            using (UnityWebRequest wwwImg = UnityWebRequestTexture.GetTexture(thumbnailUrl))
            {
                yield return wwwImg.SendWebRequest();
                
                if (wwwImg.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(wwwImg);
                    Debug.Log($"<color=cyan>缩略图下载完成!</color> 关卡ID: {levels[i].level_id}, 尺寸: {texture.width}x{texture.height}");
                    
                    // 将下载的图片显示到imageList中
                    if (imageList[i] != null)
                    {
                        // 创建Sprite并设置到Image组件
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        imageList[i].sprite = sprite;
                        imageList[i].enabled = true; // 确保Image组件是启用的
                    }
                    else
                    {
                        Debug.LogWarning($"imageList[{i}] 为空，无法显示图片");
                    }
                }
                else
                {
                    Debug.LogError($"缩略图下载失败: {wwwImg.error}, URL: {thumbnailUrl}");
                }
            }
        }
        
        Debug.Log("所有缩略图下载完成!");
    }

    IEnumerator DownloadRoutine(string id)
    {
        // 创建保存目录
        string saveFolder = Application.dataPath + "/YouYouTest/Test/WEBTest/SaveLevel";
        if (!System.IO.Directory.Exists(saveFolder))
        {
            System.IO.Directory.CreateDirectory(saveFolder);
        }

        // 1. 下载 JSON
        string jsonUrl = $"{serverUrl}/download_json/{id}";
        Debug.Log("开始下载JSON: " + jsonUrl);
        
        using (UnityWebRequest wwwJson = UnityWebRequest.Get(jsonUrl))
        {
            yield return wwwJson.SendWebRequest();
            if (wwwJson.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = wwwJson.downloadHandler.text;
                Debug.Log($"<color=cyan>JSON下载完成!</color> 内容预览: {jsonContent}");
                
                // 保存JSON到本地
                string jsonPath = System.IO.Path.Combine(saveFolder, $"{id}.json");
                System.IO.File.WriteAllText(jsonPath, jsonContent);
                Debug.Log($"<color=green>JSON已保存到: {jsonPath}</color>");
            }
            else
            {
                Debug.LogError("JSON下载失败: " + wwwJson.error);
            }
        }

        // 2. 下载 图片 (注意这里的路径逻辑要和服务端一致)
        // 我们的服务端是 /static/{id}.png
        string imgUrl = $"{serverUrl}/static/{id}.png";
        Debug.Log("开始下载图片: " + imgUrl);

        using (UnityWebRequest wwwImg = UnityWebRequestTexture.GetTexture(imgUrl))
        {
            yield return wwwImg.SendWebRequest();
            if (wwwImg.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(wwwImg);
                Debug.Log($"<color=cyan>图片下载完成!</color> 尺寸: {texture.width}x{texture.height}");
                
                // 保存图片到本地
                string imagePath = System.IO.Path.Combine(saveFolder, $"{id}.png");
                byte[] imageBytes = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(imagePath, imageBytes);
                Debug.Log($"<color=green>图片已保存到: {imagePath}</color>");
                
                // 为了直观，我们可以把下载下来的图显示在 Inspector 的材质球或者用来替换上传的那张图看效果
                // 这里我们建一个临时的 Sprite 展示在场景里（如果有 SpriteRenderer 的话）
                // 简单起见，我只打印成功日志。
            }
            else
            {
                Debug.LogError("图片下载失败: " + wwwImg.error);
            }
        }
    }
}