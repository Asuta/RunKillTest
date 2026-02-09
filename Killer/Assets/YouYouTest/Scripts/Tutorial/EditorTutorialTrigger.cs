using UnityEngine;
using UnityEngine.Video;
using VInspector;

public class EditorTutorialTrigger : MonoBehaviour
{
    public TutorialControl tutorialControl;
    public GameObject tutorialPanel;
    public VideoClip videoClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tutorialControl.ShowTutorialPanel(tutorialPanel, videoClip);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         Debug.Log("hahaha");
    //         tutorialControl.ShowTutorialPanel(tutorialPanel, videoClip);
    //     }
    // }

    [Button]
    public void ShowTutorial()
    {
        tutorialControl.ShowTutorialPanel(tutorialPanel, videoClip);
    }
}
