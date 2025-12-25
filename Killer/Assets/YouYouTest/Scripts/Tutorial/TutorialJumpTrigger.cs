using UnityEngine;
using UnityEngine.Video;
using YouYouTest.VRMove2;

public class TutorialJumpTrigger : MonoBehaviour
{


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject vrPlayerDrag = GameObject.Find("VRPlayerDrag(Clone)");

            // 查找NewVRMove2组件并开启普通跳跃功能
            NewVRMove2 vrMove = vrPlayerDrag.GetComponent<NewVRMove2>();
            if (vrMove != null)
            {
                vrMove.enableNormalJump = true;
                Debug.Log("已开启普通跳跃功能");
            }
        }
    }
}
