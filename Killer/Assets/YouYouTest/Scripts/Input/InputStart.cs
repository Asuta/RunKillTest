using UnityEngine;

public class InputStart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputActionsManager.EnableAll();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        InputActionsManager.DisableAll();
    }
}
