using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmUI : MonoBehaviour
{
    public SaveSlotInfo currentSaveSlot;
    public Button confirmUploadButton;
    public Button cancelButton;
    public Transform successPanel;

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
        SaveNetworkManager.Instance.UploadLevel(currentSaveSlot, "", (success, message) =>
        {
            if (success)
            {
                Debug.Log($"<color=green>上传成功!</color> {message}");
                if (successPanel != null)
                {
                    successPanel.gameObject.SetActive(true);
                    gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError($"上传失败: {message}");
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
