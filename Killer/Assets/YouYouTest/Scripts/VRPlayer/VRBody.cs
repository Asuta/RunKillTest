using UnityEngine;

public class VRBody : MonoBehaviour
{
    bool isFollowing = false;
    public Transform Target;
    public Vector3 offset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isFollowing)
        {
            var targetPosition = Target.position + offset;
            transform.position = targetPosition;
        }

    }

    public void StartFollow()
    {
        isFollowing = true;
    }

    public void StopFollow()
    {
        isFollowing = false;
    }


}
