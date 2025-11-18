using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;


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
            textMeshPro.text = "now load: " + GameManager.Instance.nowLoadSaveSlot;
        }
    }

    private void OnLoadSaveChange(string arg0)
    {
        textMeshPro.text = "now load: "+arg0;
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
