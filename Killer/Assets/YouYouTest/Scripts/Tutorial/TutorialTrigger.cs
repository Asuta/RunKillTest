using UnityEngine;
using UnityEngine.Video;

public class TutorialTrigger : MonoBehaviour
{
    public TutorialControl tutorialControl;
    public GameObject tutorialPanel;
    public VideoClip videoClip;

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
            Debug.Log("hahaha");
            tutorialControl.ShowTutorialPanel(tutorialPanel, videoClip);
        }
    }
}
