using UnityEngine;

public class VRBody : MonoBehaviour
{
    public Transform Target;
    public Vector3 offset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var targetPosition = Target.position + offset;
        transform.position = targetPosition;
    }
}
