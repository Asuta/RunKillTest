using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;
using System.Collections.Generic;


public class SlotNameText : MonoBehaviour
{
    //public Text text;
    public TextMeshProUGUI textMeshPro;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GlobalEvent.OnLoadSaveChange.AddListener(OnLoadSaveChange);
        
        // 主动获取当前存档名称并设置
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.nowLoadSaveSlot))
        {
            UpdateSlotNameDisplay(GameManager.Instance.nowLoadSaveSlot);
        }
    }

    private void OnLoadSaveChange(string arg0)
    {
        UpdateSlotNameDisplay(arg0);
    }

    private void UpdateSlotNameDisplay(string slotName)
    {
        // 兼容：slotName 可能是 "LocalSaveData|xxx" / "WebSaveData|xxx"
        SaveLoadManager.ParseSaveKey(slotName, out string subFolder, out string parsedSlotName);
        string displayName = !string.IsNullOrEmpty(parsedSlotName) ? parsedSlotName : slotName;

        // 尝试从SaveLoadManager获取真实的关卡名称
        if (SaveLoadManager.Instance != null)
        {
            List<SaveSlotInfo> allSlots = SaveLoadManager.Instance.GetAllSaveSlots();
            SaveSlotInfo info = allSlots.Find(s =>
                s.slotName == displayName &&
                (string.IsNullOrEmpty(subFolder) || s.subFolder == subFolder));
            if (info != null && !string.IsNullOrEmpty(info.LevelName))
            {
                displayName = info.LevelName;
            }
        }

        string prefix = string.Empty;
        if (subFolder == "WebSaveData") prefix = "[Web] ";
        else if (subFolder == "LocalSaveData") prefix = "[Local] ";

        textMeshPro.text = "now load: " + prefix + displayName;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// </summary>
    void OnDestroy()
    {
        GlobalEvent.OnLoadSaveChange.RemoveListener(OnLoadSaveChange);
    }
}
