using UnityEngine;

public class VRBody2 : MonoBehaviour
{
    public bool isFollowing = false;
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
            offset.y = -transform.localScale.y;
            var targetPosition = Target.position + offset;
            transform.position = targetPosition;
            // 只跟随目标的Y轴旋转，保持当前的X和Z轴角度
            Vector3 currentEuler = transform.eulerAngles;
            Vector3 targetEuler = Target.eulerAngles;
            transform.eulerAngles = new Vector3(currentEuler.x, targetEuler.y, currentEuler.z);
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
