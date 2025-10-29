using UnityEngine;
using YouYouTest.CommandFramework;

public class EditorPlayer : MonoBehaviour
{
    #region 字段和属性
    public GameObject UITarget;
    public Transform leftHand;
    public Transform leftCheckSphere;
    public Transform rightHand;
    public Transform rightCheckSphere;

    public IGrabable leftHoldObject = null; // 左手当前抓取的物体
    public IGrabable rightHoldObject = null; // 右手当前抓取的物体

    public IGrabable leftGrabbedObject = null; // 左手当前抓取的物体
    public IGrabable rightGrabbedObject = null; // 右手当前抓取的物体
    private GrabCommand leftCurrentGrabCommand = null; // 左手当前抓取命令
    private GrabCommand rightCurrentGrabCommand = null; // 右手当前抓取命令
    private DuplicateObjectCommand currentDuplicateCommand = null; // 当前复制命令

    private Collider[] hitColliders = new Collider[10]; // 用于OverlapSphereNonAlloc的碰撞器数组
    #endregion

    #region Unity 生命周期方法
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
            if (leftHoldObject != null)
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
            if (rightHoldObject != null)
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

        // 左手柄X键复制当前抓取/接触的物体
        if (InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.WasPressedThisFrame())
        {
            CopyLeftHandObject();
        }

        // 左手柄Y键复制当前抓取/接触的物体
        if (InputActionsManager.Actions.XRILeftInteraction.SecondaryButton.WasPressedThisFrame())
        {
            CopyLeftHandObject();
        }

        // 左手柄X键松开时释放复制的物体
        if (InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.WasReleasedThisFrame())
        {
            ReleaseCopiedObject();
        }

        // 左手柄Y键松开时释放复制的物体
        if (InputActionsManager.Actions.XRILeftInteraction.SecondaryButton.WasReleasedThisFrame())
        {
            ReleaseCopiedObject();
        }

        // 右手柄A键前进（重做），B键后退（撤销）
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Redo();
            Debug.Log("右手柄A键按下：执行前进操作");
        }

        if (InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Undo();
            Debug.Log("右手柄B键按下：执行后退操作");
        }
    }
    #endregion

    #region 左手操作方法
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

        IGrabable cubeMove = null;

        // 首先尝试在目标对象上查找IGrabable组件
        cubeMove = targetObject.GetComponent<IGrabable>();

        // 如果没找到，尝试在附加的刚体上查找
        if (cubeMove == null)
        {
            Rigidbody rigidbody = targetObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                cubeMove = rigidbody.GetComponent<IGrabable>();
            }
        }

        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有IGrabable组件，无法抓取");
            return;
        }

        // 检查这个物体是否已经被右手抓取
        if (rightGrabbedObject == cubeMove)
        {
            // 允许双手同时抓取（对所有 IGrabable 生效），不清空右手状态与命令
            Debug.Log($"物体 {targetObject.name} 已被右手抓取，允许双手同时抓取");
        }
        // 如果左手已经抓取了其他物体，先完成当前抓取命令
        else if (leftGrabbedObject != null && leftGrabbedObject != cubeMove)
        {
            Debug.LogWarning("左手已经抓取了其他物体，请先释放");
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

        // 若底层对象已被销毁（Unity null），安全清理并退出
        var leftHost = leftGrabbedObject as MonoBehaviour;
        if (leftHost == null)
        {
            Debug.LogWarning("左手抓取的对象已被销毁，跳过释放与命令记录");
            leftGrabbedObject = null;
            leftCurrentGrabCommand = null;
            return;
        }

        // 通知对象左手释放，由对象内部决定是否完全释放或切换到另一只手
        leftGrabbedObject.OnReleased(leftHand);

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
    #endregion

    #region 右手操作方法
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

        IGrabable cubeMove = null;

        // 首先尝试在目标对象上查找IGrabable组件
        cubeMove = targetObject.GetComponent<IGrabable>();

        // 如果没找到，尝试在附加的刚体上查找
        if (cubeMove == null)
        {
            Rigidbody rigidbody = targetObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                cubeMove = rigidbody.GetComponent<IGrabable>();
            }
        }

        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有IGrabable组件，无法抓取");
            return;
        }

        // 检查这个物体是否已经被左手抓取
        if (leftGrabbedObject == cubeMove)
        {
            // 允许双手同时抓取（对所有 IGrabable 生效），不清空左手状态与命令
            Debug.Log($"物体 {targetObject.name} 已被左手抓取，允许双手同时抓取");
        }
        // 如果右手已经抓取了其他物体，先完成当前抓取命令
        else if (rightGrabbedObject != null && rightGrabbedObject != cubeMove)
        {
            Debug.LogWarning("右手已经抓取了其他物体，请先释放");
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
        // 若底层对象已被销毁（Unity null），安全清理并退出
        var rightHost = rightGrabbedObject as MonoBehaviour;
        if (rightHost == null)
        {
            Debug.LogWarning("右手抓取的对象已被销毁，跳过释放与命令记录");
            rightGrabbedObject = null;
            rightCurrentGrabCommand = null;
            return;
        }


        // 通知对象右手释放，由对象内部决定是否完全释放或切换到另一只手
        rightGrabbedObject.OnReleased(rightHand);

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
    #endregion

    #region 物体检测方法
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
            // 获取碰撞器附加的刚体
            Rigidbody rigidbody = hitColliders[i].attachedRigidbody;
            if (rigidbody != null)
            {
                // 在刚体的GameObject上查找IGrabable组件
                IGrabable grabable = rigidbody.GetComponent<IGrabable>();
                if (grabable != null)
                {
                    // 返回第一个找到的IGrabable对象
                    return grabable;
                }
            }
        }

        // 没有找到IGrabable对象，返回null
        return null;
    }
    #endregion

    #region 物体删除方法
    /// <summary>
    /// 删除左手当前hold的物体
    /// </summary>
    private void DeleteLeftHandObject()
    {
        if (leftHoldObject != null)
        {
            // 获取要删除的物体
            GameObject objectToDelete = leftHoldObject.ObjectGameObject;

            // 创建删除命令并执行
            DeleteObjectCommand deleteCommand = new DeleteObjectCommand(objectToDelete);
            CommandHistory.Instance.ExecuteCommand(deleteCommand);

            // 如果当前正在抓取这个物体，需要先释放
            if (leftGrabbedObject == leftHoldObject)
            {
                leftGrabbedObject.OnReleased(leftHand);
                leftGrabbedObject = null;
                leftCurrentGrabCommand = null;
            }

            // 清空hold状态
            Debug.Log($"左手柄A键按下：删除物体 {objectToDelete.name}");
            leftHoldObject = null;
        }
        else
        {
            Debug.Log("左手没有hold任何物体，无法删除");
        }
    }
    #endregion

    #region 物体复制方法
    /// <summary>
    /// 复制左手当前抓取/接触的物体
    /// </summary>
    private void CopyLeftHandObject()
    {
        // 优先复制正在抓取的物体
        IGrabable sourceObject = leftGrabbedObject;
        
        // 如果没有抓取的物体，尝试复制hold的物体
        if (sourceObject == null && leftHoldObject != null)
        {
            sourceObject = leftHoldObject;
        }
        
        if (sourceObject != null)
        {
            GameObject originalObject = sourceObject.ObjectGameObject;
            
            // 在原始物体的位置和角度复制物体
            currentDuplicateCommand = new DuplicateObjectCommand(originalObject, originalObject.transform.position, originalObject.transform.rotation);
            
            // 执行复制命令并添加到历史
            CommandHistory.Instance.ExecuteCommand(currentDuplicateCommand);
            
            // 获取复制的物体
            GameObject duplicatedObject = currentDuplicateCommand.GetDuplicatedObject();
            
            if (duplicatedObject != null)
            {
                // 设置相同的缩放
                duplicatedObject.transform.localScale = originalObject.transform.localScale;
                
                // 如果左手已经抓取了其他物体，先释放
                if (leftGrabbedObject != null)
                {
                    LeftHandRelease();
                }
                
                // 立即抓取复制的物体
                LeftHandGrab(duplicatedObject);
                
                Debug.Log($"复制物体: {originalObject.name} -> {duplicatedObject.name}");
            }
            else
            {
                Debug.LogWarning("复制物体失败");
                currentDuplicateCommand = null;
            }
        }
        else
        {
            Debug.Log("左手没有抓取或接触任何物体，无法复制");
        }
    }

    /// <summary>
    /// 释放复制的物体
    /// </summary>
    private void ReleaseCopiedObject()
    {
        if (leftGrabbedObject != null)
        {
            // 如果有复制命令，更新其最终位置和旋转
            if (currentDuplicateCommand != null && leftGrabbedObject.ObjectGameObject == currentDuplicateCommand.GetDuplicatedObject())
            {
                currentDuplicateCommand.UpdateTransform(leftGrabbedObject.ObjectTransform.position, leftGrabbedObject.ObjectTransform.rotation);
                currentDuplicateCommand = null;
            }
            
            LeftHandRelease();
            Debug.Log("释放复制的物体");
        }
    }
    #endregion
}
