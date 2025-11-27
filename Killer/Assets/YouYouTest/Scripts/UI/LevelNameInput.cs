using UnityEngine;
using TMPro;
using VInspector;

public class LevelNameInput : MonoBehaviour
{
    public TMP_InputField inputField;
    public string levelJsonName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        // 添加输入完成事件监听
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputEndEdit);

            // 确保文本能够正确显示
            Debug.Log("当前输入框文本: " + inputField.text);

            // 如果需要通过代码设置文本，可以取消下面这行注释
            // inputField.text = "默认文本";
        }
    }

 

    void OnInputEndEdit(string text)
    {
        Debug.Log("输入完成，新的关卡名称: " + text);
        // 这里可以添加保存关卡名称的逻辑，例如更新保存数据
        SaveLoadManager.Instance.UpdateLevelName(levelJsonName, text);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
