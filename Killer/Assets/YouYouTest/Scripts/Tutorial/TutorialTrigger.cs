using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    private bool hasTriggered = false;
    public TutorialControl tutorialControl;
    public GameObject tutorialPanel;

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
        if (!hasTriggered)
        {
            hasTriggered = true;
            Debug.Log("hahaha");
            tutorialControl.ShowTutorialPanel(tutorialPanel);
        }
    }
}
