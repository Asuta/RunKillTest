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


    /// <summary>
    /// LateUpdate is called every frame, if the Behaviour is enabled.
    /// It is called after all Update functions have been called.
    /// </summary>
    void LateUpdate()
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
