using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class ExitTrigger : MonoBehaviour
{
    public AudioClip exitSound;
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
            // 播放退出音效
            if (exitSound != null)
            {
                AudioSource.PlayClipAtPoint(exitSound, transform.position);
            }
            StartCoroutine(DelayedLoadLobbyScene());
        }
    }

    IEnumerator DelayedLoadLobbyScene()
    {
        yield return new WaitForSeconds(6f);
        SceneManager.LoadScene("LobbyScene");
    }
}
