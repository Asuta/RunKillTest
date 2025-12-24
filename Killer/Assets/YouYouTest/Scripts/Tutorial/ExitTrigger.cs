using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class ExitTrigger : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(DelayedLoadLobbyScene());
        }
    }

    IEnumerator DelayedLoadLobbyScene()
    {
        yield return new WaitForSeconds(6f);
        SceneManager.LoadScene("LobbyScene");
    }
}
