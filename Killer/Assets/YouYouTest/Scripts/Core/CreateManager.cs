using UnityEngine;
using YouYouTest.CommandFramework;

public class CreateManager : MonoBehaviour
{
    public EditorPlayer editorPlayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 注册监听创建按钮点击事件
        GlobalEvent.CreateButtonPoke.AddListener(OnCreateButtonPoke);
        Debug.Log("CreateManager已注册CreateButtonPoke事件监听");
    }

    // Update is called once per frame
    void Update()
    {
        // 检测撤销和重做快捷键
        if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            CommandHistory.Instance.Undo();
        }
        else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            CommandHistory.Instance.Redo();
        }
    }
    
    // 处理创建按钮点击事件
    void OnCreateButtonPoke(GameObject prefab, Transform buttonTransform)
    {
        Debug.Log($"CreateManager收到按钮点击事件 - 预制体: {prefab.name}, 位置: {buttonTransform.position}");
        
        // 创建生成物体的命令
        CreateObjectCommand createCommand = new CreateObjectCommand(prefab, buttonTransform.position);
        
        // 通过CommandHistory单例执行命令
        CommandHistory.Instance.ExecuteCommand(createCommand);
        
        // 获取创建的物体
        GameObject createdObject = createCommand.GetCreatedObject();
        if (createdObject != null && editorPlayer != null)
        {
            // 让右手直接抓取创建的物体
            editorPlayer.RightHandGrab(createdObject);
            Debug.Log($"创建后右手自动抓取物体: {createdObject.name}");
        }
        else
        {
            if (createdObject == null)
                Debug.LogWarning("创建的物体为空，无法抓取");
            if (editorPlayer == null)
                Debug.LogWarning("EditorPlayer引用为空，无法执行抓取");
        }
    }
    
    // 当对象被销毁时移除事件监听
    void OnDestroy()
    {
        GlobalEvent.CreateButtonPoke.RemoveListener(OnCreateButtonPoke);
        Debug.Log("CreateManager已移除CreateButtonPoke事件监听");
    }
}
