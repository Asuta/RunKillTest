using UnityEngine;
using UnityEngine.Video;

public class TutorialControl : MonoBehaviour
{
    public GameObject TutorialPanel;
    public VideoPlayer videoPlayer;

    public void ShowTutorialPanel(GameObject newPanel, VideoClip videoClip)
    {
        if (TutorialPanel != null)
        {
            TutorialPanel.SetActive(false);
        }

        TutorialPanel = newPanel;
        TutorialPanel.SetActive(true);

        videoPlayer.clip = videoClip;
        videoPlayer.Stop();
        videoPlayer.Play();
    }
}
