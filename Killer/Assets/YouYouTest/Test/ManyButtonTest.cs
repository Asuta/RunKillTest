using UnityEngine;
using VInspector;


public class ManyButtonTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Button(" 模式切换")]
    void Button1()
    {
        GlobalEvent.ModeButtonPoke.Invoke();
    }

    [Button]
    void Button2()
    {
        Debug.Log("Button2");
    }

    [Button("enter play scene and load target json")]
    void Button3(string jsonName)
    {
        
    }
}
