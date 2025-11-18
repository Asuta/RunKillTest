using UnityEngine;

public class VRGrabBody : MonoBehaviour
{
    public Transform bodyTarget;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// LateUpdate is called every frame, if the Behaviour is enabled.
    /// It is called after all Update functions have been called.
    /// </summary>
    void LateUpdate()
    {
        transform.position = bodyTarget.position;
        transform.rotation = bodyTarget.rotation;
    }
}
