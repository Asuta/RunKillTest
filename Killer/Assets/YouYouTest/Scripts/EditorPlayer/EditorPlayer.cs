using UnityEngine;
using YouYouTest.CommandFramework;

public class EditorPlayer : MonoBehaviour
{
    public GameObject UITarget;
    public Transform leftHand;
    public Transform leftCheckSphere;
    public Transform rightHand;
    public Transform rightCheckSphere;

    public IGrabable leftHoldObject = null; // 左手当前抓取的物体
    public IGrabable rightHoldObject = null; // 右手当前抓取的物体

    private IGrabable leftGrabbedObject = null; // 左手当前抓取的物体
    private IGrabable rightGrabbedObject = null; // 右手当前抓取的物体
    private GrabCommand leftCurrentGrabCommand = null; // 左手当前抓取命令
    private GrabCommand rightCurrentGrabCommand = null; // 右手当前抓取命令
    
    private Collider[] hitColliders = new Collider[10]; // 用于OverlapSphereNonAlloc的碰撞器数组

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // 检测左右手附近的可抓取对象
        CheckForGrabableObjects();
        
        // 左手柄菜单键（Menu）
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasPressedThisFrame())
        {
            Debug.Log("左手柄菜单键按下");
            UITarget.SetActive(!UITarget.activeSelf);
        }

        // 左手扳机按下时抓取物体
        if (InputActionsManager.Actions.XRILeftInteraction.Activate.WasPressedThisFrame())
        {
            if (leftHoldObject != null && leftGrabbedObject == null)
            {
                LeftHandGrab(leftHoldObject.ObjectGameObject);
                Debug.Log($"左手扳机按下，抓取物体: {leftHoldObject.ObjectGameObject.name}");
            }
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

        // 右手扳机按下时抓取物体
        if (InputActionsManager.Actions.XRIRightInteraction.Activate.WasPressedThisFrame())
        {
            if (rightHoldObject != null && rightGrabbedObject == null)
            {
                RightHandGrab(rightHoldObject.ObjectGameObject);
                Debug.Log($"右手扳机按下，抓取物体: {rightHoldObject.ObjectGameObject.name}");
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

        IGrabable cubeMove = targetObject.GetComponent<IGrabable>();
        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有CubeMoveTest组件，无法抓取");
            return;
        }

        // 创建抓取命令，记录抓取前的状态
        leftCurrentGrabCommand = new GrabCommand(cubeMove.ObjectTransform, cubeMove.ObjectTransform.position, cubeMove.ObjectTransform.rotation);

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
            leftCurrentGrabCommand.SetEndTransform(leftGrabbedObject.ObjectTransform.position, leftGrabbedObject.ObjectTransform.rotation);

            // 执行命令并添加到历史
            CommandHistory.Instance.ExecuteCommand(leftCurrentGrabCommand);
            Debug.Log($"左手抓取命令已执行: {leftGrabbedObject.ObjectGameObject.name} 从 {leftCurrentGrabCommand}");
        }

        Debug.Log($"左手松开了抓取的物体: {leftGrabbedObject.ObjectGameObject.name}");
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

        IGrabable cubeMove = targetObject.GetComponent<IGrabable>();
        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有CubeMoveTest组件，无法抓取");
            return;
        }

        // 创建抓取命令，记录抓取前的状态
        rightCurrentGrabCommand = new GrabCommand(cubeMove.ObjectTransform, cubeMove.ObjectTransform.position, cubeMove.ObjectTransform.rotation);

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
            rightCurrentGrabCommand.SetEndTransform(rightGrabbedObject.ObjectTransform.position, rightGrabbedObject.ObjectTransform.rotation);

            // 执行命令并添加到历史
            CommandHistory.Instance.ExecuteCommand(rightCurrentGrabCommand);
            Debug.Log($"右手抓取命令已执行: {rightGrabbedObject.ObjectGameObject.name} 从 {rightCurrentGrabCommand}");
        }

        Debug.Log($"右手松开了抓取的物体: {rightGrabbedObject.ObjectGameObject.name}");
        rightGrabbedObject = null;
        rightCurrentGrabCommand = null;
    }
    
    /// <summary>
    /// 检测左右手附近的可抓取对象
    /// </summary>
    private void CheckForGrabableObjects()
    {
        // 检测左手附近的可抓取对象
        leftHoldObject = DetectGrabableObject(leftCheckSphere);
        
        // 检测右手附近的可抓取对象
        rightHoldObject = DetectGrabableObject(rightCheckSphere);
    }
    
    /// <summary>
    /// 在指定Transform位置检测可抓取对象
    /// </summary>
    /// <param name="checkSphere">检测球体的Transform</param>
    /// <returns>检测到的第一个IGrabable对象，如果没有则返回null</returns>
    private IGrabable DetectGrabableObject(Transform checkSphere)
    {
        if (checkSphere == null)
        {
            Debug.LogWarning("检测球体Transform为空");
            return null;
        }
        
        // 使用CheckSphere的lossyScale的最大值作为检测半径
        float radius = Mathf.Max(checkSphere.lossyScale.x, checkSphere.lossyScale.y, checkSphere.lossyScale.z);
        
        // 使用OverlapSphereNonAlloc检测指定位置附近的碰撞器
        int hitCount = Physics.OverlapSphereNonAlloc(checkSphere.position, radius, hitColliders);
        
        // 遍历检测到的碰撞器
        for (int i = 0; i < hitCount; i++)
        {
            // 检查碰撞器所属的GameObject是否有IGrabable组件
            IGrabable grabable = hitColliders[i].GetComponent<IGrabable>();
            if (grabable != null)
            {
                // 返回第一个找到的IGrabable对象
                return grabable;
            }
        }
        
        // 没有找到IGrabable对象，返回null
        return null;
    }
}
