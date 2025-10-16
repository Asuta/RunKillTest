using UnityEngine;
using YouYouTest.CommandFramework;

public class EditorPlayer : MonoBehaviour
{
    public GameObject UITarget;
    public Transform leftHand;
    public Transform rightHand;

    private CubeMoveTest leftGrabbedObject = null; // 左手当前抓取的物体
    private CubeMoveTest rightGrabbedObject = null; // 右手当前抓取的物体
    private GrabCommand leftCurrentGrabCommand = null; // 左手当前抓取命令
    private GrabCommand rightCurrentGrabCommand = null; // 右手当前抓取命令

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {



        // 左手柄菜单键（Menu）
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasPressedThisFrame())
        {
            Debug.Log("左手柄菜单键按下");
            UITarget.SetActive(!UITarget.activeSelf);
        }

        // 左手扳机抬起时释放物体
        if (InputActionsManager.Actions.XRILeftInteraction.Activate.WasReleasedThisFrame())
        {
            if (leftGrabbedObject != null)
            {
                LeftHandRelease();
                Debug.Log("左手扳机抬起，释放物体");
            }
        }

        // 右手扳机抬起时释放物体
        if (InputActionsManager.Actions.XRIRightInteraction.Activate.WasReleasedThisFrame())
        {
            if (rightGrabbedObject != null)
            {
                RightHandRelease();
                Debug.Log("右手扳机抬起，释放物体");
            }
        }


        // 右手柄A键撤销，B键重做
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Undo();
            Debug.Log("右手柄A键按下：执行撤销操作");
        }

        if (InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Redo();
            Debug.Log("右手柄B键按下：执行重做操作");
        }
    }

    /// <summary>
    /// 左手抓取物体
    /// </summary>
    /// <param name="targetObject">要抓取的物体</param>
    public void LeftHandGrab(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("左手抓取目标物体为空");
            return;
        }

        if (leftGrabbedObject != null)
        {
            Debug.LogWarning("左手已经抓取了物体，请先释放");
            return;
        }

        CubeMoveTest cubeMove = targetObject.GetComponent<CubeMoveTest>();
        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有CubeMoveTest组件，无法抓取");
            return;
        }

        // 创建抓取命令，记录抓取前的状态
        leftCurrentGrabCommand = new GrabCommand(cubeMove.transform, cubeMove.transform.position, cubeMove.transform.rotation);

        // 抓取这个物体
        leftGrabbedObject = cubeMove;
        cubeMove.OnGrabbed(leftHand);
        Debug.Log($"左手抓取了物体: {targetObject.name}");
    }

    /// <summary>
    /// 左手释放物体
    /// </summary>
    public void LeftHandRelease()
    {
        if (leftGrabbedObject == null)
        {
            Debug.LogWarning("左手没有抓取任何物体");
            return;
        }

        // 释放物体
        leftGrabbedObject.OnReleased();

        // 完成抓取命令并添加到历史
        if (leftCurrentGrabCommand != null)
        {
            // 设置最终位置和旋转
            leftCurrentGrabCommand.SetEndTransform(leftGrabbedObject.transform.position, leftGrabbedObject.transform.rotation);

            // 执行命令并添加到历史
            CommandHistory.Instance.ExecuteCommand(leftCurrentGrabCommand);
            Debug.Log($"左手抓取命令已执行: {leftGrabbedObject.gameObject.name} 从 {leftCurrentGrabCommand}");
        }

        Debug.Log($"左手松开了抓取的物体: {leftGrabbedObject.gameObject.name}");
        leftGrabbedObject = null;
        leftCurrentGrabCommand = null;
    }

    /// <summary>
    /// 右手抓取物体
    /// </summary>
    /// <param name="targetObject">要抓取的物体</param>
    public void RightHandGrab(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("右手抓取目标物体为空");
            return;
        }

        if (rightGrabbedObject != null)
        {
            Debug.LogWarning("右手已经抓取了物体，请先释放");
            return;
        }

        CubeMoveTest cubeMove = targetObject.GetComponent<CubeMoveTest>();
        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有CubeMoveTest组件，无法抓取");
            return;
        }

        // 创建抓取命令，记录抓取前的状态
        rightCurrentGrabCommand = new GrabCommand(cubeMove.transform, cubeMove.transform.position, cubeMove.transform.rotation);

        // 抓取这个物体
        rightGrabbedObject = cubeMove;
        cubeMove.OnGrabbed(rightHand);
        Debug.Log($"右手抓取了物体: {targetObject.name}");
    }

    /// <summary>
    /// 右手释放物体
    /// </summary>
    public void RightHandRelease()
    {
        if (rightGrabbedObject == null)
        {
            Debug.LogWarning("右手没有抓取任何物体");
            return;
        }

        // 释放物体
        rightGrabbedObject.OnReleased();

        // 完成抓取命令并添加到历史
        if (rightCurrentGrabCommand != null)
        {
            // 设置最终位置和旋转
            rightCurrentGrabCommand.SetEndTransform(rightGrabbedObject.transform.position, rightGrabbedObject.transform.rotation);

            // 执行命令并添加到历史
            CommandHistory.Instance.ExecuteCommand(rightCurrentGrabCommand);
            Debug.Log($"右手抓取命令已执行: {rightGrabbedObject.gameObject.name} 从 {rightCurrentGrabCommand}");
        }

        Debug.Log($"右手松开了抓取的物体: {rightGrabbedObject.gameObject.name}");
        rightGrabbedObject = null;
        rightCurrentGrabCommand = null;
    }
}
