using UnityEngine;
using UnityEngine.UI;

public class DeleteSelectUI : MonoBehaviour
{
    public Button deleteConfirmButton;
    public Button deleteCancelButton;
    public GameObject thisUI;
    public string deleteObjectName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 为取消按钮绑定点击事件，点击时禁用UI
        deleteCancelButton.onClick.AddListener(() => {
            thisUI.SetActive(false);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
