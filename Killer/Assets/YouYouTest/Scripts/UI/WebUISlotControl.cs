using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class WebUISlotControl : MonoBehaviour
{
    [Header("服务器设置")]
    public string serverUrl = "http://192.168.5.236:8080";
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
    }

    void OnEnable()
    {
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

        // 使用统一的 SaveNetworkManager 获取列表
        SaveNetworkManager.Instance.GetLevelList(serverUrl, currentPage, PageSize, (response) => {
            LevelItem[] levels = response != null ? response.tasks : null;

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
        });
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
            // 如果 data 为 null，Setup 内部应该处理空数据的情况
            slot.Setup(data, serverUrl);
        }
    }
}
