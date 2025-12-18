using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class WebUISlotControl : MonoBehaviour
{
    public string serverUrl = "http://127.0.0.1:8000";
    public GameObject sampleSlotPrefab;
    public Transform slotContainer;

    private List<GameObject> spawnedSlots = new List<GameObject>();

    void Start()
    {
        RefreshList();
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
        string url = serverUrl.TrimEnd('/') + "/get_levels/?page=1&page_size=20";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("获取列表失败: " + www.error);
                yield break;
            }

            string jsonString = www.downloadHandler.text;
            LevelItem[] levels = null;

            try
            {
                // 使用 NetworkTest.cs 中定义的 JsonHelper
                levels = JsonHelper.FromJson<LevelItem>(jsonString);
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON解析出错: " + e.Message);
                yield break;
            }

            if (levels != null)
            {
                foreach (var levelData in levels)
                {
                    CreateSlot(levelData);
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
            slot.Setup(data, serverUrl);
        }
    }
}
