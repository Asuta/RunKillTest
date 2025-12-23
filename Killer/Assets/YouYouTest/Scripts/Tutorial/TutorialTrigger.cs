using UnityEngine;
using UnityEngine.Video;

public class TutorialTrigger : MonoBehaviour
{
    private bool hasTriggered = false;
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
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("hahaha");
            tutorialControl.ShowTutorialPanel(tutorialPanel, videoClip);
        }
    }
}
