using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class WebUISlotControl : MonoBehaviour
{
    public string serverUrl = "http://127.0.0.1:8000";
    public GameObject sampleSlotPrefab;
    public Transform slotContainer;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    public Button nextButton;
    public Button prevButton;

    private int currentPage = 1;
    private const int PageSize = 9;

    void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextPage);
        if (prevButton != null) prevButton.onClick.AddListener(OnPrevPage);

        RefreshList();
    }

    public void OnNextPage()
    {
        currentPage++;
        RefreshList();
    }

    public void OnPrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            RefreshList();
        }
    }

    [ContextMenu("Refresh List")]
    public void RefreshList()
    {
        // 清空旧的 Slot
        foreach (GameObject slot in spawnedSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        spawnedSlots.Clear();

        StartCoroutine(GetListRoutine());
    }

    IEnumerator GetListRoutine()
    {
        string url = $"{serverUrl.TrimEnd('/')}/get_levels/?page={currentPage}&page_size={PageSize}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            LevelItem[] levels = null;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("获取列表失败: " + www.error);
                // 即使失败也生成空槽位，或者保持旧的？
                // 根据要求，我们应该尝试解析或处理
            }
            else
            {
                string jsonString = www.downloadHandler.text;
                try
                {
                    // 使用 NetworkTest.cs 中定义的 JsonHelper
                    levels = JsonHelper.FromJson<LevelItem>(jsonString);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON解析出错: " + e.Message);
                }
            }

            // 无论成功与否，都确保生成 9 个槽位
            for (int i = 0; i < PageSize; i++)
            {
                if (levels != null && i < levels.Length)
                {
                    CreateSlot(levels[i]);
                }
                else
                {
                    CreateSlot(null); // 生成空槽位
                }
            }
        }
    }

    void CreateSlot(LevelItem data)
    {
        if (sampleSlotPrefab == null || slotContainer == null) return;

        GameObject go = Instantiate(sampleSlotPrefab, slotContainer);
        go.SetActive(true);
        spawnedSlots.Add(go);

        WebLevelSlot slot = go.GetComponent<WebLevelSlot>();
        if (slot != null)
        {
            // 如果 data 为 null，Setup 内部应该处理空数据的情况（比如隐藏 UI 元素）
            slot.Setup(data, serverUrl);
        }
    }
}
