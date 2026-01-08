using UnityEngine;
using UnityEngine.UI;

public class LobbyNo : MonoBehaviour
{
    public GameObject lobbyUI;
    public Button lobbyNoButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lobbyNoButton.onClick.AddListener(OnLobbyNoClick);
    }

    private void OnLobbyNoClick()
    {
        // 关闭大厅UI
        lobbyUI.SetActive(false);
    }


}
