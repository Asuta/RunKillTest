using UnityEngine;
using UnityEngine.UI;

public class OpenLobbyButton : MonoBehaviour
{
    public GameObject lobbyUI;
    public Button openLobbyButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        openLobbyButton.onClick.AddListener(OnOpenLobbyClick);
    }

    private void OnOpenLobbyClick()
    {
        // 打开大厅UI
        lobbyUI.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
