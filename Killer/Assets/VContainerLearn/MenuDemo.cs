using UnityEngine;
using VContainer;

public class MenuDemo : MonoBehaviour
{
    private AudioManager _audioManager;
    private MenuController _menuController;

    [Inject]
    public void Construct(AudioManager audioManager, MenuController menuController)
    {
        _audioManager = audioManager;
        _menuController = menuController;
    }

    void Start()
    {
        Debug.Log("--- Menu Demo Start ---");
        _audioManager.PlaySound("MenuBGM");
        _menuController.OpenMenu();
    }
}