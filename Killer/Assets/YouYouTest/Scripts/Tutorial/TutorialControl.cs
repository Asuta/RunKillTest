using UnityEngine;

public class TutorialControl : MonoBehaviour
{
    public GameObject TutorialPanel;

    public void ShowTutorialPanel(GameObject gameObject)
    {
        if (TutorialPanel != null)
        {
            TutorialPanel.SetActive(false);
            TutorialPanel = gameObject;
            TutorialPanel.SetActive(true);
        }
        else
        {
            TutorialPanel = gameObject;
            TutorialPanel.SetActive(true);
        }
    }
}
