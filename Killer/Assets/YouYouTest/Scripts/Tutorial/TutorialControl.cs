using UnityEngine;
using UnityEngine.Video;

public class TutorialControl : MonoBehaviour
{
    public GameObject TutorialPanel;
    public VideoPlayer videoPlayer;

    public void ShowTutorialPanel(GameObject gameObject, VideoClip videoClip)
    {
        if (TutorialPanel != null)
        {
            TutorialPanel.SetActive(false);
            TutorialPanel = gameObject;
            TutorialPanel.SetActive(true);
            videoPlayer.clip = videoClip;
            videoPlayer.Stop();
            videoPlayer.Play();
        }
        else
        {
            TutorialPanel = gameObject;
            TutorialPanel.SetActive(true);
            videoPlayer.clip = videoClip;
            videoPlayer.Stop();
            videoPlayer.Play();
        }
    }
}
