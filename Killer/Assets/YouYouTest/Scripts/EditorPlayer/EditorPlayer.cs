using UnityEngine;
using YouYouTest.CommandFramework;
using YouYouTest.OutlineSystem;
using YouYouTest;

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
    private YouYouTest.CommandFramework.CombinedCreateAndMoveCommand currentDuplicateCommand = null; // 当前复制命令（合并创建与移动）

    private Collider[] hitColliders = new Collider[10]; // 用于OverlapSphereNonAlloc的碰撞器数组
    
    // 描边系统相关字段（单实例管理左右手）
    [SerializeField] private HandOutlineController handOutlineController;
    
    // 跟踪菜单键状态的变量
    private bool menuKeyPressed = false;
    private float menuKeyPressTime = 0f;
    private const float QUICK_PRESS_THRESHOLD = 0.2f; // 快速按下的时间阈值（秒）
    
    // 右手 A 键短按选择的计时变量
    private bool rightAKeyPressed = false;
    private float rightAKeyPressTime = 0f;
    private const float RIGHT_A_QUICK_THRESHOLD = 0.2f; // A键短按阈值（秒）
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

        // 检测左手柄菜单键按下
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasPressedThisFrame())
        {
            menuKeyPressed = true;
            menuKeyPressTime = Time.time;
            Debug.Log("左手柄菜单键按下");
        }
        
        // 检测左手柄菜单键抬起（只有快速按下后抬起的情况）
        if (InputActionsManager.Actions.XRILeftInteraction.Menu.WasReleasedThisFrame() && menuKeyPressed)
        {
            float pressDuration = Time.time - menuKeyPressTime;
            menuKeyPressed = false;
            
            // 只有按下时间小于阈值才认为是快速按下
            if (pressDuration < QUICK_PRESS_THRESHOLD)
            {
                Debug.Log($"左手柄菜单键快速抬起，按下时长: {pressDuration:F2}秒");
                
                // 切换UITarget的active状态
                if (UITarget != null)
                {
                    bool newState = !UITarget.activeSelf;
                    UITarget.SetActive(newState);
                    Debug.Log("UITarget状态切换为: " + (newState ? "激活" : "禁用"));
                }
                else
                {
                    Debug.LogWarning("UITarget为空");
                }
            }
            else
            {
                Debug.Log($"左手柄菜单键长按后抬起，按下时长: {pressDuration:F2}秒，不触发UI切换");
            }
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

        // 左手柄X键撤销
        if (InputActionsManager.Actions.XRILeftInteraction.PrimaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Undo();
            Debug.Log("左手柄X键按下：执行撤销操作");
        }

        // 左手柄Y键重做
        if (InputActionsManager.Actions.XRILeftInteraction.SecondaryButton.WasPressedThisFrame())
        {
            CommandHistory.Instance.Redo();
            Debug.Log("左手柄Y键按下：执行重做操作");
        }

        // 右手柄A键：开始/结束计时以支持短按选择逻辑（短按 < RIGHT_A_QUICK_THRESHOLD）
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasPressedThisFrame())
        {
            rightAKeyPressed = true;
            rightAKeyPressTime = Time.time;
            Debug.Log("右手A键按下，开始计时");
        }
 
        // 右手A键抬起时根据按下时长判断是否为短按，短按则将当前接触的物体设为 Selected
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasReleasedThisFrame() && rightAKeyPressed)
        {
            float pressDuration = Time.time - rightAKeyPressTime;
            rightAKeyPressed = false;
 
            if (pressDuration < RIGHT_A_QUICK_THRESHOLD)
            {
                if (rightHoldObject != null)
                {
                    handOutlineController?.SetSelectedForCurrentHold(false, rightHoldObject);
                    Debug.Log($"右手A键快速点击，设置 Selected：{rightHoldObject.ObjectGameObject.name}");
                }
                else
                {
                    Debug.Log("右手A键快速点击，但没有接触物体");
                }
            }
            else
            {
                Debug.Log($"右手A键长按后抬起，按下时长: {pressDuration:F2}s，不设置 Selected");
            }
        }
 
        // 右手柄B键复制当前抓取/接触的物体（替代原删除功能）
        if (InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.WasPressedThisFrame())
        {
            CopyRightHandObject();
        }
 
        // 右手柄B键松开时释放复制的物体（对应复制从 B 键开始）
        if (InputActionsManager.Actions.XRIRightInteraction.SecondaryButton.WasReleasedThisFrame())
        {
            ReleaseRightCopiedObject();
        }
    }
    #endregion

    #region 左手操作方法
    /// <summary>
    /// 左手抓取物体（重构：使用工具方法以减少重复代码）
    /// </summary>
    /// <param name="targetObject">要抓取的物体</param>
    public void LeftHandGrab(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("左手抓取目标物体为空");
            return;
        }

        // 从工具类获取 IGrabable（支持直接在 GameObject 或其刚体上查找）
        IGrabable cubeMove = EditorPlayerHelpers.GetGrabableFromGameObject(targetObject);
        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有IGrabable组件，无法抓取");
            return;
        }

        // 双手同时抓取检查与已有抓取冲突
        if (rightGrabbedObject == cubeMove)
        {
            Debug.Log($"物体 {targetObject.name} 已被右手抓取，允许双手同时抓取");
        }
        else if (leftGrabbedObject != null && leftGrabbedObject != cubeMove)
        {
            Debug.LogWarning("左手已经抓取了其他物体，请先释放");
            return;
        }

        // 根据是否为复制刚创建的物体决定是否生成单独的 GrabCommand
        leftCurrentGrabCommand = EditorPlayerHelpers.CreateGrabCommandIfNotDuplicated(currentDuplicateCommand, cubeMove);

        // 抓取并更新描边
        leftGrabbedObject = cubeMove;

        // 防御性检查：确保 leftHand 已在 Inspector 分配，避免运行时 NullReferenceException
        if (leftHand == null)
        {
            Debug.LogError("EditorPlayer.leftHand 未分配，请在 Inspector 中为 EditorPlayer 组件设置 leftHand 引用");
            return;
        }

        cubeMove.OnGrabbed(leftHand);
        handOutlineController?.UpdateTarget(true, leftHoldObject, leftHoldObject, leftGrabbedObject);
        Debug.Log($"左手抓取了物体: {targetObject.name}");
    }

    /// <summary>
    /// 左手释放物体（重构：委托给工具方法处理释放及命令提交）
    /// </summary>
    public void LeftHandRelease()
    {
        EditorPlayerHelpers.ReleaseGrab(ref leftGrabbedObject, ref leftCurrentGrabCommand, leftHand, true, handOutlineController);
    }
    #endregion

    #region 右手操作方法
    /// <summary>
    /// 右手抓取物体（重构：使用工具方法以减少重复代码）
    /// </summary>
    /// <param name="targetObject">要抓取的物体</param>
    public void RightHandGrab(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("右手抓取目标物体为空");
            return;
        }

        IGrabable cubeMove = EditorPlayerHelpers.GetGrabableFromGameObject(targetObject);
        if (cubeMove == null)
        {
            Debug.LogWarning($"目标物体 {targetObject.name} 没有IGrabable组件，无法抓取");
            return;
        }

        if (leftGrabbedObject == cubeMove)
        {
            Debug.Log($"物体 {targetObject.name} 已被左手抓取，允许双手同时抓取");
        }
        else if (rightGrabbedObject != null && rightGrabbedObject != cubeMove)
        {
            Debug.LogWarning("右手已经抓取了其他物体，请先释放");
            return;
        }

        rightCurrentGrabCommand = EditorPlayerHelpers.CreateGrabCommandIfNotDuplicated(currentDuplicateCommand, cubeMove);

        rightGrabbedObject = cubeMove;

        // 防御性检查：确保 rightHand 已在 Inspector 分配，避免运行时 NullReferenceException
        if (rightHand == null)
        {
            Debug.LogError("EditorPlayer.rightHand 未分配，请在 Inspector 中为 EditorPlayer 组件设置 rightHand 引用");
            return;
        }

        cubeMove.OnGrabbed(rightHand);
        handOutlineController?.UpdateTarget(false, rightHoldObject, rightHoldObject, rightGrabbedObject);
        Debug.Log($"右手抓取了物体: {targetObject.name}");
    }

    /// <summary>
    /// 右手释放物体（重构：委托给工具方法处理释放及命令提交）
    /// </summary>
    public void RightHandRelease()
    {
        EditorPlayerHelpers.ReleaseGrab(ref rightGrabbedObject, ref rightCurrentGrabCommand, rightHand, false, handOutlineController);
    }
    #endregion

    #region 物体检测方法
    /// <summary>
    /// 检测左右手附近的可抓取对象
    /// </summary>
    private void CheckForGrabableObjects()
    {
        // 检测左手附近的可抓取对象
        IGrabable previousLeftHoldObject = leftHoldObject;
        leftHoldObject = DetectGrabableObject(leftCheckSphere);
        // 使用单实例 HandOutlineController 管理描边显示（传入手侧标识）
        handOutlineController?.UpdateTarget(true, previousLeftHoldObject, leftHoldObject, leftGrabbedObject);

        // 检测右手附近的可抓取对象
        IGrabable previousRightHoldObject = rightHoldObject;
        rightHoldObject = DetectGrabableObject(rightCheckSphere);
        // 使用单实例 HandOutlineController 管理描边显示（传入手侧标识）
        handOutlineController?.UpdateTarget(false, previousRightHoldObject, rightHoldObject, rightGrabbedObject);
    }

    /// <summary>
    /// 在指定Transform位置检测可抓取对象
    /// </summary>
    /// <param name="checkSphere">检测球体的Transform</param>
    /// <returns>检测到的第一个IGrabable对象，如果没有则返回null</returns>
    private IGrabable DetectGrabableObject(Transform checkSphere)
    {
        // 重构：使用工具类实现检测逻辑，减少重复代码并共享 hitColliders 缓冲区
        var g = EditorPlayerHelpers.DetectGrabableObject(checkSphere, hitColliders);
        if (g == null)
        {
            // 手动保留原有的警告行为以便调试
            if (checkSphere == null) Debug.LogWarning("检测球体Transform为空");
        }
        return g;
    }
    #endregion


    #region 物体删除方法
    /// <summary>
    /// 删除左手当前hold的物体
    /// </summary>
    private void DeleteLeftHandObject()
    {
        // 使用工具方法执行删除逻辑并清理抓取状态
        EditorPlayerHelpers.ExecuteDelete(leftHoldObject, ref leftGrabbedObject, ref leftCurrentGrabCommand, leftHand, true, handOutlineController);
        leftHoldObject = null;
    }
    #endregion

    #region 物体复制方法
    /// <summary>
    /// 复制左手当前抓取/接触的物体
    /// </summary>
    private void CopyLeftHandObject()
    {
        // 优先复制正在抓取的物体，否则复制 hold 的物体
        IGrabable sourceObject = leftGrabbedObject ?? leftHoldObject;
        if (sourceObject == null)
        {
            Debug.Log("左手没有抓取或接触任何物体，无法复制");
            return;
        }

        currentDuplicateCommand = EditorPlayerHelpers.CreateDuplicateAndGrab(sourceObject, true);
        if (currentDuplicateCommand != null)
        {
            var duplicatedObject = currentDuplicateCommand.GetCreatedObject();
            if (duplicatedObject != null)
            {
                if (leftGrabbedObject != null) LeftHandRelease();
                LeftHandGrab(duplicatedObject);
            }
            else
            {
                currentDuplicateCommand = null;
            }
        }
    }

    /// <summary>
    /// 释放复制的物体
    /// </summary>
    private void ReleaseCopiedObject()
    {
        if (leftGrabbedObject != null)
        {
            EditorPlayerHelpers.UpdateDuplicateOnRelease(currentDuplicateCommand, leftGrabbedObject);
            currentDuplicateCommand = null;
            LeftHandRelease();
            Debug.Log("释放左手复制的物体");
        }
    }

    /// <summary>
    /// 删除右手当前hold的物体
    /// </summary>
    private void DeleteRightHandObject()
    {
        EditorPlayerHelpers.ExecuteDelete(rightHoldObject, ref rightGrabbedObject, ref rightCurrentGrabCommand, rightHand, false, handOutlineController);
        rightHoldObject = null;
    }

    /// <summary>
    /// 复制右手当前抓取/接触的物体
    /// </summary>
    private void CopyRightHandObject()
    {
        IGrabable sourceObject = rightGrabbedObject ?? rightHoldObject;
        if (sourceObject == null)
        {
            Debug.Log("右手没有抓取或接触任何物体，无法复制");
            return;
        }

        currentDuplicateCommand = EditorPlayerHelpers.CreateDuplicateAndGrab(sourceObject, false);
        if (currentDuplicateCommand != null)
        {
            var duplicatedObject = currentDuplicateCommand.GetCreatedObject();
            if (duplicatedObject != null)
            {
                if (rightGrabbedObject != null) RightHandRelease();
                RightHandGrab(duplicatedObject);
            }
            else
            {
                currentDuplicateCommand = null;
            }
        }
    }

    /// <summary>
    /// 释放右手复制的物体
    /// </summary>
    private void ReleaseRightCopiedObject()
    {
        if (rightGrabbedObject != null)
        {
            EditorPlayerHelpers.UpdateDuplicateOnRelease(currentDuplicateCommand, rightGrabbedObject);
            currentDuplicateCommand = null;
            RightHandRelease();
            Debug.Log("释放右手复制的物体");
        }
    }
    #endregion

}