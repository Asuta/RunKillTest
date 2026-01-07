using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public static class SaveLoadUtils
{
    /// <summary>
    /// 统一处理存档加载逻辑：检查场景、跳转场景并恢复存档
    /// </summary>
    /// <param name="slotInfo">存档信息</param>
    public static void LoadSaveSlot(SaveSlotInfo slotInfo)
    {
        if (slotInfo == null)
        {
            Debug.LogWarning("LoadSaveSlot: slotInfo 为空");
            return;
        }

        string fileName = slotInfo.fileName;
        string subFolder = slotInfo.subFolder;
        string saveKey = SaveLoadManager.ComposeSaveKey(subFolder, slotInfo.slotName);

        Debug.Log($"[SaveLoadUtils] 准备加载存档: {subFolder}/{fileName}");

        // 1. 设置 GameManager 中的当前存档标识
        if (GameManager.Instance != null)
        {
            GameManager.Instance.nowLoadSaveSlot = saveKey;
        }
        else
        {
            Debug.LogWarning("GameManager 实例不存在，无法设置 nowLoadSaveSlot");
        }

        // 2. 检查场景并执行加载
        if (SceneManager.GetActiveScene().name == "KillScene")
        {
            ExecuteLoad(fileName, subFolder, saveKey);
        }
        else
        {
            // 不在 KillScene 时：注册加载完成回调并切换场景
            UnityAction<Scene, LoadSceneMode> onLoaded = null;
            onLoaded = (scene, mode) =>
            {
                if (scene.name == "KillScene")
                {
                    SceneManager.sceneLoaded -= onLoaded;
                    ExecuteLoad(fileName, subFolder, saveKey);
                    
                    // 场景切换后的额外状态处理
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetPlayMode(false);
                    }
                }
            };

            SceneManager.sceneLoaded += onLoaded;
            SceneManager.LoadScene("KillScene");
        }
    }

    /// <summary>
    /// 执行具体的存档恢复逻辑
    /// </summary>
    private static void ExecuteLoad(string fileName, string subFolder, string saveKey)
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.LoadSceneObjectsByFileName(fileName, subFolder);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCanSwitchMode(true);
            }

            // 通知所有监听者存档已变更
            GlobalEvent.OnLoadSaveChange.Invoke(saveKey);
            Debug.Log($"[SaveLoadUtils] 存档恢复完成: {saveKey}");
        }
        else
        {
            Debug.LogError("SaveLoadManager 实例不存在，无法恢复存档数据！");
        }
    }
}
