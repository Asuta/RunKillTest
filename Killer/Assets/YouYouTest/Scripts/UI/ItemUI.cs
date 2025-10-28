using UnityEngine;

public class ItemUI : MonoBehaviour
{
    public GameObject SaveUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// This function is called when the behaviour becomes disabled or inactive.
    /// </summary>
    void OnDisable()
    {
        // 隐藏SaveUI
        SaveUI.SetActive(false);
    }
}
