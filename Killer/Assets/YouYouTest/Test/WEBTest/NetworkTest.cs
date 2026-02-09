using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using UnityEngine.UI;
using VInspector;

public class NetworkTest : MonoBehaviour
{
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
    
    [Button]
    [ContextMenu("1. Test Upload (上传)")]
    public void TestUpload()
    {
        if (uploadJson == null || uploadImage == null)
        {
            Debug.LogError("请先在Inspector面板拖入 Json文件 和 图片！");
            return;
        }
        
        byte[] imageBytes = uploadImage.EncodeToPNG();
        if (imageBytes == null)
        {
            Debug.LogError("图片转换失败！请检查图片的 Import Settings 是否开启了 Read/Write。");
            return;
        }

        SaveNetworkManager.Instance.UploadLevelRaw(
            levelName, 
            "level.json", 
            uploadJson.bytes, 
            "thumb.png", 
            imageBytes, 
            0, // 测试数据默认传 0
            "这是通过 NetworkTest 发送的测试描述", 
            (result, message) => {
                if (result == UploadResult.Success) Debug.Log("上传成功! 服务器返回: " + message);
                else Debug.LogError($"上传失败 [{result}]: " + message);
            }
        );
    }

    [ContextMenu("2. Test Get List (获取列表)")]
    public void TestGetList()
    {
        TestGetListWithParams();
    }

    [ContextMenu("2.1 Test Get List (获取指定页)")]
    public void TestGetListWithParams()
    {
        SaveNetworkManager.Instance.GetLevelList(currentPage, pageSize, (response) => {
            if (response == null || response.tasks == null || response.tasks.Length == 0)
            {
                Debug.LogWarning("列表是空的，请先上传一个关卡。");
                return;
            }

            Debug.Log($"解析成功! 找到了 {response.total_num} 个关卡 (当前页: {response.tasks.Length})。");
            Debug.Log($"<color=green>建议测试用的 ID: {response.tasks[0].tid}</color> (已为你自动填入Inspector)");
            
            testDownloadId = response.tasks[0].tid;
            
            // 开始批量下载缩略图
            StartCoroutine(DownloadThumbnailsRoutine(response.tasks));
        });
    }

    [ContextMenu("2.2 Test Get List (获取第1页)")]
    public void TestGetListPage1()
    {
        currentPage = 1;
        TestGetListWithParams();
    }

    [ContextMenu("2.3 Test Get List (获取第2页)")]
    public void TestGetListPage2()
    {
        currentPage = 2;
        TestGetListWithParams();
    }

    [ContextMenu("2.4 Test Get List (获取第3页)")]
    public void TestGetListPage3()
    {
        currentPage = 3;
        TestGetListWithParams();
    }

    [ContextMenu("3. Test Download (下载)")]
    public void TestDownload()
    {
        if (string.IsNullOrEmpty(testDownloadId))
        {
            Debug.LogError("请先填入 testDownloadId！");
            return;
        }

        SaveNetworkManager.Instance.DownloadLevel(testDownloadId, (success) => {
            if (success) Debug.Log($"下载成功! 请查看 {Application.persistentDataPath}/WebSaveData 文件夹。");
            else Debug.LogError("下载失败!");
        });
    }

    // --- 具体实现协程 ---

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
            if (string.IsNullOrEmpty(thumbnailUrl)) continue;

            if (!thumbnailUrl.StartsWith("http"))
            {
                thumbnailUrl = SaveNetworkManager.Instance.ServerUrl.TrimEnd('/') + (thumbnailUrl.StartsWith("/") ? "" : "/") + thumbnailUrl;
            }

            int index = i;
            SaveNetworkManager.Instance.DownloadThumbnail(thumbnailUrl, (sprite) => {
                if (sprite != null && imageList[index] != null)
                {
                    imageList[index].sprite = sprite;
                    imageList[index].enabled = true;
                }
            });
        }
        
        Debug.Log("所有缩略图下载请求已发出!");
    }
}