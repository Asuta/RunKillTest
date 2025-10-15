using UnityEngine;

public class EditorPlayer : MonoBehaviour
{
    public GameObject UITarget;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {



        // 左手柄菜单键（Menu）
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasPressedThisFrame())
        {
            Debug.Log("左手柄菜单键按下");
            UITarget.SetActive(!UITarget.activeSelf);
        }
    }
}
