using UnityEngine;
 
public class EditorPlayerSetting : MonoBehaviour
{
    public Transform vrEditorRigOffset;
    public Transform vrEditorRigO;
 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 主动注册到 GameManager（如果 GameManager 已初始化）
        if (GameManager.Instance != null)
        {
            if (vrEditorRigO != null)
            {
                GameManager.Instance.RegisterVrEditorRig(vrEditorRigO);
            }
            if (vrEditorRigOffset != null)
            {
                GameManager.Instance.RegisterVrEditorRigOffset(vrEditorRigOffset);
            }
        }
    }
 
    // Update is called once per frame
    void Update()
    {
        
    }
 
    private void OnDestroy()
    {
        // 在销毁时反注册，避免悬挂引用
        if (GameManager.Instance != null)
        {
            if (vrEditorRigO != null)
            {
                GameManager.Instance.UnregisterVrEditorRig(vrEditorRigO);
            }
            if (vrEditorRigOffset != null)
            {
                GameManager.Instance.UnregisterVrEditorRigOffset(vrEditorRigOffset);
            }
        }
    }
}
