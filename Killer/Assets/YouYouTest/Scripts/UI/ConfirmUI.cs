using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmUI : MonoBehaviour
{
    public SaveSlotInfo currentSaveSlot;
    public Button confirmUploadButton;
    public Button cancelButton;
    public Transform successPanel;
    public Transform limitPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        confirmUploadButton.onClick.AddListener(OnConfirmUploadButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void OnCancelButtonClicked()
    {
        // 关闭确认UI
        gameObject.SetActive(false);
    }

    private void OnConfirmUploadButtonClicked()
    {
        // 确认上传当前存档
        SaveNetworkManager.Instance.UploadLevel(currentSaveSlot, "", (result, message) =>
        {
            if (result == UploadResult.Success)
            {
                Debug.Log($"<color=green>上传成功!</color> {message}");
                if (successPanel != null)
                {
                    successPanel.gameObject.SetActive(true);
                    gameObject.SetActive(false);
                }
            }
            else if (result == UploadResult.LimitReached)
            {
                Debug.LogWarning($"<color=orange>上传受限:</color> {message}");
                // 这里可以弹出专门的提示框，或者在 log 中用不同的颜色
                if (limitPanel != null)
                {
                    limitPanel.gameObject.SetActive(true);
                    // gameObject.SetActive(false);
                }
                // 也可以根据需要不关闭 UI，让玩家知道原因
                // gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError($"上传失败 [{result}]: {message}");
                // 关闭确认UI
                gameObject.SetActive(false);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
