using UnityEngine;

public class RotateAroundTest11 : MonoBehaviour
{
    public Transform rotateCenter;
    public float rotateSpeed = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(rotateCenter.position, Vector3.up, rotateSpeed * Time.deltaTime);
    }
}
