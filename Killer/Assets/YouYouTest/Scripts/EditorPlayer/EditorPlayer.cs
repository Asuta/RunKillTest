using UnityEngine;
using YouYouTest.CommandFramework;
using YouYouTest.OutlineSystem;
using YouYouTest;
using System.Collections.Generic;

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

    // 多抓取支持
    private System.Collections.Generic.List<IGrabable> leftMultiGrabbedObjects = new System.Collections.Generic.List<IGrabable>(); // 左手多抓取的对象列表
    private System.Collections.Generic.List<IGrabable> rightMultiGrabbedObjects = new System.Collections.Generic.List<IGrabable>(); // 右手多抓取的对象列表
    private GrabCommand leftCurrentGrabCommand = null; // 左手当前抓取命令
    private GrabCommand rightCurrentGrabCommand = null; // 右手当前抓取命令
    private YouYouTest.CommandFramework.CombinedCreateAndMoveCommand currentDuplicateCommand = null; // 当前复制命令（合并创建与移动）
    private System.Collections.Generic.List<YouYouTest.CommandFramework.CombinedCreateAndMoveCommand> currentMultiDuplicateCommands = new System.Collections.Generic.List<YouYouTest.CommandFramework.CombinedCreateAndMoveCommand>(); // 当前多选复制命令列表
    private YouYouTest.CommandFramework.BatchDuplicateCommand currentBatchDuplicateCommand = null; // 当前批量复制命令
    private YouYouTest.CommandFramework.BatchMoveCommand currentBatchMoveCommand = null; // 当前批量移动命令

    private Collider[] hitColliders = new Collider[10]; // 用于OverlapSphereNonAlloc的碰撞器数组

    //select UI相关
    public GameObject selectUI;
    public float offsetYdistance;
    private GameObject currentSelectUIInstance; // 当前显示的selectUI实例


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

    // 右手 A 键长按多选相关变量
    private bool rightALongPressActive = false;
    private float rightALongPressStartTime = 0f;
    private const float RIGHT_A_LONG_PRESS_THRESHOLD = 0.2f; // A键长按阈值（秒）
    
    // 持续范围选择时显示的青色球相关变量
    private Material selectionSphereMaterial;
    private Mesh dynamicSelectionSphereMesh; // 用于动态半径的球体网格
    private float lastSelectionSphereRadius = -1f; // 上次使用的球体半径
    private static readonly Color SELECTION_SPHERE_COLOR = Color.cyan; // 选择球体的颜色（青色）
    
    // 检测球体颜色控制相关变量
    private Renderer leftCheckSphereRenderer;
    private Color originalLeftCheckSphereColor;
    private Renderer rightCheckSphereRenderer;
    private Color originalRightCheckSphereColor;
    private static readonly Color CYAN_COLOR = Color.cyan; // 青色
    private static readonly Color YELLOW_COLOR = Color.yellow; // 黄色
    #endregion

    #region Unity 生命周期方法
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初始化选择球体的网格和材质
        InitializeSelectionSphere();
        
        // 初始化检测球体的渲染器和原始颜色
        InitializeCheckSpheres();
        
        // 注册全局事件监听
        GlobalEvent.OnLoadObjectsSetSelected.AddListener(SetObjectsAsSelected);
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

        // 左手扳机按下时抓取物体（优先抓取多选对象）
        if (InputActionsManager.Actions.XRILeftInteraction.Activate.WasPressedThisFrame())
        {
            // 检查射线是否命中UI，如果命中UI则不允许抓取
            if (GameManager.Instance.IsRaycastHittingUI)
            {
                Debug.Log("左手扳机按下，但射线命中UI，不允许抓取对象");
                return;
            }

            var multi = handOutlineController?.GetAllMultiSelectedGrabables();
            if (multi != null && multi.Count > 0)
            {
                GrabMultiForHand(true);
                Debug.Log($"左手扳机按下，抓取多选对象，共 {multi.Count} 个");
            }
            else if (leftHoldObject != null)
            {
                LeftHandGrab(leftHoldObject.ObjectGameObject);
                Debug.Log($"左手扳机按下，抓取物体: {leftHoldObject.ObjectGameObject.name}");
            }
        }

        // 左手扳机抬起时释放物体
        if (InputActionsManager.Actions.XRILeftInteraction.Activate.WasReleasedThisFrame())
        {
            if (leftGrabbedObject != null || leftMultiGrabbedObjects.Count > 0)
            {
                LeftHandRelease();
                Debug.Log("左手扳机抬起，释放物体");
            }
        }

        // 右手扳机按下时抓取物体（优先抓取多选对象）
        if (InputActionsManager.Actions.XRIRightInteraction.Activate.WasPressedThisFrame())
        {
            // 检查射线是否命中UI，如果命中UI则不允许抓取
            if (GameManager.Instance.IsRaycastHittingUI)
            {
                Debug.Log("右手扳机按下，但射线命中UI，不允许抓取对象");
                return;
            }

            var multi = handOutlineController?.GetAllMultiSelectedGrabables();
            if (multi != null && multi.Count > 0)
            {
                GrabMultiForHand(false);
                Debug.Log($"右手扳机按下，抓取多选对象，共 {multi.Count} 个");
            }
            else if (rightHoldObject != null)
            {
                RightHandGrab(rightHoldObject.ObjectGameObject);
                Debug.Log($"右手扳机按下，抓取物体: {rightHoldObject.ObjectGameObject.name}");
            }
        }

        // 右手扳机抬起时释放物体
        if (InputActionsManager.Actions.XRIRightInteraction.Activate.WasReleasedThisFrame())
        {
            if (rightGrabbedObject != null || rightMultiGrabbedObjects.Count > 0)
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
            rightALongPressStartTime = Time.time;
            rightALongPressActive = false;
            
            // 将右手检测球体颜色变为青色
            SetRightCheckSphereColor(CYAN_COLOR);
            
            Debug.Log("右手A键按下，开始计时");
        }

        // 右手A键抬起时根据按下时长判断是否为短按，短按则将当前接触的物体设为 Selected
        if (InputActionsManager.Actions.XRIRightInteraction.PrimaryButton.WasReleasedThisFrame() && rightAKeyPressed)
        {
            float pressDuration = Time.time - rightAKeyPressTime;
            rightAKeyPressed = false;
            rightALongPressActive = false;
            
            // 将右手检测球体颜色恢复为黄色
            SetRightCheckSphereColor(YELLOW_COLOR);

            if (pressDuration < RIGHT_A_QUICK_THRESHOLD)
            {
                // 短按：清除所有多选，然后进行单选
                handOutlineController?.ClearAllMultiSelection();

                if (rightHoldObject != null)
                {
                    handOutlineController?.SetSelectedForCurrentHold(false, rightHoldObject);
                    Debug.Log($"右手A键快速点击，设置 Selected：{rightHoldObject.ObjectGameObject.name}");

                    // 触发选择成功事件
                    GlobalEvent.OnSelect.Invoke();

                    // 显示选择UI
                    ShowSelectUI();
                }
                else
                {
                    // 未命中任何对象时，取消上一次的选中
                    handOutlineController?.CancelLastSelected();
                    Debug.Log("右手A键快速点击，但没有接触物体，已取消上一次的选中");

                    // 隐藏选择UI
                    HideSelectUI();
                }
            }
            else
            {
                Debug.Log($"右手A键长按后抬起，按下时长: {pressDuration:F2}s，多选模式结束");

                // 检查是否有多选对象，如果有则触发选择成功事件
                var multiSelectedGrabables = handOutlineController?.GetAllMultiSelectedGrabables();
                if (multiSelectedGrabables != null && multiSelectedGrabables.Count > 0)
                {
                    Debug.Log($"多选完成，共选中 {multiSelectedGrabables.Count} 个对象，触发选择成功事件");
                    GlobalEvent.OnSelect.Invoke();

                    // 显示选择UI
                    ShowSelectUI();
                }
                else
                {
                    // 多选模式结束但没有选中任何对象，隐藏UI
                    HideSelectUI();
                }
            }
        }

        // 右手A键长按多选逻辑
        if (rightAKeyPressed && !rightALongPressActive)
        {
            float pressDuration = Time.time - rightALongPressStartTime;
            if (pressDuration >= RIGHT_A_LONG_PRESS_THRESHOLD)
            {
                rightALongPressActive = true;
                // 开始长按多选模式，清除之前的单选
                handOutlineController?.CancelLastSelected();
                Debug.Log($"右手A键长按超过 {RIGHT_A_LONG_PRESS_THRESHOLD}s，开始多选模式");
            }
        }

        // 长按多选模式下的持续检测（每帧执行）
        if (rightALongPressActive)
        {
            EditorPlayerHelpers.PerformMultiSelectionCheck(rightCheckSphere, hitColliders, handOutlineController);
            
            // 在持续范围选择期间绘制青色球
            if (rightCheckSphere != null)
            {
                // 计算实际的检测半径（与PerformMultiSelectionCheck中的计算方式相同）
                float detectionRadius = Mathf.Max(rightCheckSphere.lossyScale.x, rightCheckSphere.lossyScale.y, rightCheckSphere.lossyScale.z);
                DrawSelectionSphere(rightCheckSphere.position, detectionRadius);
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
            
            // B键抬起时重新生成selectUI，确保注入最新的选中对象
            if (currentSelectUIInstance != null)
            {
                // 销毁之前的selectUI
                Destroy(currentSelectUIInstance);
                currentSelectUIInstance = null;
                
                // 重新生成selectUI
                ShowSelectUI();
                Debug.Log("B键抬起：重新生成selectUI以更新注入的对象");
            }
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
        // 处理多抓取对象的释放
        EditorPlayerHelpers.ReleaseMultiGrabbedObjects(leftMultiGrabbedObjects, leftHand, true);

        // 处理单抓取对象的释放
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
    /// 右手释放物体（支持单抓取和多抓取）
    /// </summary>
    public void RightHandRelease()
    {
        // 处理批量移动命令的更新
        if (currentBatchMoveCommand != null && rightMultiGrabbedObjects.Count > 0)
        {
            EditorPlayerHelpers.UpdateBatchMoveCommand(currentBatchMoveCommand, rightMultiGrabbedObjects);
            currentBatchMoveCommand = null;
        }

        // 处理多抓取对象的释放
        EditorPlayerHelpers.ReleaseMultiGrabbedObjects(rightMultiGrabbedObjects, rightHand, false);

        // 处理单抓取对象的释放
        EditorPlayerHelpers.ReleaseGrab(ref rightGrabbedObject, ref rightCurrentGrabCommand, rightHand, false, handOutlineController);
    }

    /// <summary>
    /// 根据当前多选集合抓取所有选中的对象（isLeftHand 控制左右手）
    /// 优先级：若存在多选对象，则抓取它们；否则不处理。
    /// </summary>
    private void GrabMultiForHand(bool isLeftHand)
    {
        var multi = handOutlineController?.GetAllMultiSelectedGrabables();
        if (multi == null || multi.Count == 0) return;

        Transform hand = isLeftHand ? leftHand : rightHand;
        var targetMultiGrabbedObjects = isLeftHand ? leftMultiGrabbedObjects : rightMultiGrabbedObjects;
        System.Action releaseAction = isLeftHand ? (System.Action)LeftHandRelease : RightHandRelease;

        // 创建批量移动命令（只对非复制操作）
        if (!isLeftHand) // 只为右手创建批量移动命令
        {
            currentBatchMoveCommand = EditorPlayerHelpers.CreateBatchMoveCommand(multi);
        }

        bool grabbedAny = EditorPlayerHelpers.GrabMultiSelection(
            multi,
            hand,
            targetMultiGrabbedObjects,
            releaseAction,
            isLeftHand);

        if (!grabbedAny)
        {
            Debug.LogWarning($"{(isLeftHand ? "左手" : "右手")}多选抓取失败，未找到有效对象");
            if (!isLeftHand) currentBatchMoveCommand = null; // 清理失败的批量移动命令
        }
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
    /// 复制右手当前抓取/接触的物体，优先复制多选对象
    /// </summary>
    private void CopyRightHandObject()
    {
        // 优先级1：检查是否有多选对象
        var multiSelectedGrabables = handOutlineController?.GetAllMultiSelectedGrabables();
        if (multiSelectedGrabables != null && multiSelectedGrabables.Count > 0)
        {
            CopyMultipleObjects(multiSelectedGrabables);
            return;
        }

        // 优先级2：复制单个对象（抓取的或接触的）
        IGrabable sourceObject = rightGrabbedObject ?? rightHoldObject;
        if (sourceObject == null)
        {
            Debug.Log("右手没有抓取或接触任何物体，且没有多选对象，无法复制");
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
    /// 释放右手复制的物体（支持单选和多选复制）
    /// </summary>
    private void ReleaseRightCopiedObject()
    {
        // 处理批量复制的释放
        EditorPlayerHelpers.ReleaseMultiDuplicateObjects(currentMultiDuplicateCommands, currentDuplicateCommand, currentBatchDuplicateCommand, rightGrabbedObject, rightMultiGrabbedObjects);

        // 清理复制命令
        if (currentBatchDuplicateCommand != null)
        {
            currentBatchDuplicateCommand = null;
        }
        else if (currentMultiDuplicateCommands.Count == 0 && rightGrabbedObject != null)
        {
            currentDuplicateCommand = null;
        }

        // 统一调用释放方法（处理多抓取对象和单抓取对象）
        RightHandRelease();
    }
    #endregion

    #region 多选功能方法
    /// <summary>
    /// 复制多个对象并抓取所有复制的对象
    /// </summary>
    private void CopyMultipleObjects(System.Collections.Generic.List<IGrabable> sourceObjects)
    {
        if (sourceObjects == null || sourceObjects.Count == 0)
        {
            Debug.Log("多选复制：源对象列表为空");
            return;
        }

        // 清除之前的复制命令
        currentMultiDuplicateCommands.Clear();
        currentBatchDuplicateCommand = null;

        // 释放当前抓取的对象（包括多抓取）
        if (rightGrabbedObject != null || rightMultiGrabbedObjects.Count > 0) RightHandRelease();

        Debug.Log($"开始批量复制，共 {sourceObjects.Count} 个对象");

        // 使用新的批量复制命令
        currentBatchDuplicateCommand = EditorPlayerHelpers.CreateBatchDuplicateAndGrab(sourceObjects);
        
        if (currentBatchDuplicateCommand == null)
        {
            Debug.LogWarning("批量复制失败：无法创建批量复制命令");
            return;
        }

        // 执行命令以创建对象
        CommandHistory.Instance.ExecuteCommand(currentBatchDuplicateCommand);
        
        var duplicatedGameObjects = currentBatchDuplicateCommand.GetCreatedObjects();
        if (duplicatedGameObjects.Count == 0)
        {
            Debug.LogWarning("批量复制失败：没有成功复制任何对象");
            return;
        }

        // 1) 取消之前所有被选中的对象（将选中切换到新复制出来的对象）
        handOutlineController?.ClearAllMultiSelection();

        // 2) 将新复制出来的对象设置为 Selected（并记录到 HandOutlineController 的多选集合）
        foreach (var go in duplicatedGameObjects)
        {
            if (go == null) continue;
            var recv = go.GetComponentInParent<OutlineReceiver>();
            if (recv != null)
            {
                handOutlineController?.AddToMultiSelection(recv);
            }
        }

        // 3) 抓取所有复制的对象（让它们跟随手移动）
        var duplicatedGrabables = new System.Collections.Generic.List<IGrabable>();
        foreach (var go in duplicatedGameObjects)
        {
            if (go == null) continue;
            var grabable = EditorPlayerHelpers.GetGrabableFromGameObject(go);
            if (grabable != null) duplicatedGrabables.Add(grabable);
        }

        // 使用辅助方法统一抓取所有复制的对象
        EditorPlayerHelpers.GrabMultipleObjects(duplicatedGrabables, rightHand, rightMultiGrabbedObjects);

        Debug.Log($"批量复制并切换选中目标完成：复制 {duplicatedGameObjects.Count} 个对象，抓取 {rightMultiGrabbedObjects.Count} 个对象");
    }
    #endregion

    #region UI出现方法

    public void ShowSelectUI()
    {
        if (selectUI != null && rightCheckSphere != null)
        {
            //获取当前scale偏差值
            var scaleOffset = GameManager.Instance.VrEditorScale;


            // 如果已有selectUI实例，先销毁
            if (currentSelectUIInstance != null)
            {
                Destroy(currentSelectUIInstance);
            }

            // 在右手检测球体的位置生成selectUI
            currentSelectUIInstance = Instantiate(selectUI, rightCheckSphere.position, rightCheckSphere.rotation);
            // 根据offsetYdistance调整UI位置
            currentSelectUIInstance.transform.position += new Vector3(0, offsetYdistance * scaleOffset, 0);
            // 根据scaleOffset调整UI缩放
            currentSelectUIInstance.transform.localScale *= scaleOffset;
            
            // 获取SelectUI组件并注入选中的对象
            var selectUIComponent = currentSelectUIInstance.GetComponent<SelectUI>();
            if (selectUIComponent != null)
            {
                // 获取当前选中的对象（支持单选和多选）
                var selectedObjects = GetSelectedObjects();
                
                // 尝试获取 IConfigurable 接口（仅在单选时）
                IConfigurable configurable = null;
                if (selectedObjects.Length == 1 && selectedObjects[0] != null)
                {
                    configurable = selectedObjects[0].GetComponent<IConfigurable>();
                }

                selectUIComponent.InitializeSelectedObjects(selectedObjects, configurable);
                Debug.Log($"已向SelectUI注入 {selectedObjects.Length} 个选中的对象");
            }
            else
            {
                Debug.LogWarning("生成的selectUI实例上没有找到SelectUI组件");
            }
            
            // 让UI只在Y轴朝向相机
            Transform EditorCamera = GameManager.Instance.VrEditorCameraT;
            if (EditorCamera != null)
            {
                Vector3 lookAtPosition = EditorCamera.position;
                lookAtPosition.y = currentSelectUIInstance.transform.position.y; // 保持Y轴高度不变
                currentSelectUIInstance.transform.LookAt(lookAtPosition); 
                Debug.Log($"selectUI已朝向相机，只调整Y轴方向"); 
            }
            else
            {
                Debug.LogWarning("无法获取玩家相机，selectUI将保持默认朝向");
            }
            
            Debug.Log($"在右手检测球体位置生成selectUI: {rightCheckSphere.position}");
        }
        else
        {
            Debug.LogWarning("selectUI或rightCheckSphere为空，无法生成UI");
        }
    }

    /// <summary>
    /// 销毁当前显示的selectUI
    /// </summary>
    public void HideSelectUI()
    {
        if (currentSelectUIInstance != null)
        {
            Destroy(currentSelectUIInstance);
            currentSelectUIInstance = null;
            Debug.Log("销毁selectUI");
        }
    }

    /// <summary>
    /// 获取当前选中的对象（支持单选和多选）
    /// </summary>
    /// <returns>选中的GameObject数组</returns>
    private GameObject[] GetSelectedObjects()
    {
        var selectedObjects = new System.Collections.Generic.List<GameObject>();
        
        // 优先获取多选对象
        var multiSelectedGrabables = handOutlineController?.GetAllMultiSelectedGrabables();
        if (multiSelectedGrabables != null && multiSelectedGrabables.Count > 0)
        {
            foreach (var grabable in multiSelectedGrabables)
            {
                if (grabable != null && grabable.ObjectGameObject != null)
                {
                    selectedObjects.Add(grabable.ObjectGameObject);
                }
            }
        }
        // 如果没有多选，则获取单选对象
        else if (rightHoldObject != null && rightHoldObject.ObjectGameObject != null)
        {
            selectedObjects.Add(rightHoldObject.ObjectGameObject);
        }
        
        return selectedObjects.ToArray();
    }

    #endregion

    #region 选择球体相关方法
    /// <summary>
    /// 初始化选择球体的网格和材质
    /// </summary>
    private void InitializeSelectionSphere()
    {
        // 创建青色材质
        selectionSphereMaterial = new Material(Shader.Find("Sprites/Default"));
        selectionSphereMaterial.color = SELECTION_SPHERE_COLOR;
        
        // 初始化动态网格为null，将在需要时创建
        dynamicSelectionSphereMesh = null;
        lastSelectionSphereRadius = -1f;
    }
    
    /// <summary>
    /// 绘制选择球体
    /// </summary>
    /// <param name="position">球体位置</param>
    /// <param name="radius">球体半径</param>
    private void DrawSelectionSphere(Vector3 position, float radius)
    {
        if (selectionSphereMaterial == null) return;
        
        // 检查是否需要重新创建网格（半径变化）
        if (Mathf.Abs(radius - lastSelectionSphereRadius) > 0.001f)
        {
            // 清理旧的动态网格
            if (dynamicSelectionSphereMesh != null)
            {
                Destroy(dynamicSelectionSphereMesh);
            }
            
            // 创建新的动态半径球体网格
            dynamicSelectionSphereMesh = MeshDrawUtility.CreateSphereMesh(radius);
            lastSelectionSphereRadius = radius;
        }
        
        // 使用动态网格绘制球体
        if (dynamicSelectionSphereMesh != null)
        {
            MeshDrawUtility.DrawSphere(position, selectionSphereMaterial, dynamicSelectionSphereMesh);
        }
    }
    
    /// <summary>
    /// 清理选择球体资源
    /// </summary>
    private void CleanupSelectionSphere()
    {
        if (dynamicSelectionSphereMesh != null)
        {
            Destroy(dynamicSelectionSphereMesh);
            dynamicSelectionSphereMesh = null;
        }
        
        if (selectionSphereMaterial != null)
        {
            Destroy(selectionSphereMaterial);
            selectionSphereMaterial = null;
        }
        
        lastSelectionSphereRadius = -1f;
    }
    
    /// <summary>
    /// OnDestroy时清理资源
    /// </summary>
    void OnDestroy()
    {
        CleanupSelectionSphere();
        
        // 移除全局事件监听
        GlobalEvent.OnLoadObjectsSetSelected.RemoveListener(SetObjectsAsSelected);
    }
    #endregion

    #region 检测球体颜色控制方法
    /// <summary>
    /// 初始化检测球体的渲染器和原始颜色
    /// </summary>
    private void InitializeCheckSpheres()
    {
        // 初始化左手检测球体
        if (leftCheckSphere != null)
        {
            leftCheckSphereRenderer = leftCheckSphere.GetComponent<Renderer>();
            if (leftCheckSphereRenderer != null)
            {
                // 保存原始颜色
                originalLeftCheckSphereColor = leftCheckSphereRenderer.material.color;
                Debug.Log($"左手检测球体原始颜色: {originalLeftCheckSphereColor}");
            }
            else
            {
                Debug.LogWarning("左手检测球体没有Renderer组件");
            }
        }
        else
        {
            Debug.LogWarning("左手检测球体Transform为空");
        }
        
        // 初始化右手检测球体
        if (rightCheckSphere != null)
        {
            rightCheckSphereRenderer = rightCheckSphere.GetComponent<Renderer>();
            if (rightCheckSphereRenderer != null)
            {
                // 保存原始颜色
                originalRightCheckSphereColor = rightCheckSphereRenderer.material.color;
                Debug.Log($"右手检测球体原始颜色: {originalRightCheckSphereColor}");
            }
            else
            {
                Debug.LogWarning("右手检测球体没有Renderer组件");
            }
        }
        else
        {
            Debug.LogWarning("右手检测球体Transform为空");
        }
    }

    /// <summary>
    /// 设置左手检测球体的颜色
    /// </summary>
    /// <param name="color">要设置的颜色</param>
    private void SetLeftCheckSphereColor(Color color)
    {
        if (leftCheckSphereRenderer != null)
        {
            leftCheckSphereRenderer.material.color = color;
            Debug.Log($"左手检测球体颜色已设置为: {color}");
        }
        else
        {
            Debug.LogWarning("左手检测球体Renderer为空，无法设置颜色");
        }
    }

    /// <summary>
    /// 设置右手检测球体的颜色
    /// </summary>
    /// <param name="color">要设置的颜色</param>
    private void SetRightCheckSphereColor(Color color)
    {
        if (rightCheckSphereRenderer != null)
        {
            rightCheckSphereRenderer.material.color = color;
            Debug.Log($"右手检测球体颜色已设置为: {color}");
        }
        else
        {
            Debug.LogWarning("右手检测球体Renderer为空，无法设置颜色");
        }
    }
    #endregion
    
    #region 公共访问方法
    /// <summary>
    /// 获取HandOutlineController实例
    /// </summary>
    /// <returns>HandOutlineController实例</returns>
    public HandOutlineController GetHandOutlineController()
    {
        return handOutlineController;
    }
    
    /// <summary>
    /// 将指定的对象列表设置为选中状态，并设置为正在被右手抓住的状态
    /// </summary>
    /// <param name="objects">要设置为选中状态的对象列表</param>
    public void SetObjectsAsSelected(List<GameObject> objects)
    {
        if (handOutlineController == null)
        {
            Debug.LogError("HandOutlineController为空，无法设置选中状态");
            return;
        }
        
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("对象列表为空，无需设置选中状态");
            return;
        }
        
        // 清除之前的多选状态
        handOutlineController.ClearAllMultiSelection();
        
        // 清除右手当前抓取的对象
        if (rightGrabbedObject != null || rightMultiGrabbedObjects.Count > 0)
        {
            RightHandRelease();
        }
        
        // 将所有对象添加到多选列表中
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            
            // 获取OutlineReceiver组件
            OutlineReceiver outlineReceiver = obj.GetComponentInParent<OutlineReceiver>();
            if (outlineReceiver != null)
            {
                handOutlineController.AddToMultiSelection(outlineReceiver);
                Debug.Log($"已将对象 {obj.name} 添加到选中状态");
            }
            else
            {
                Debug.LogWarning($"对象 {obj.name} 上没有找到OutlineReceiver组件");
            }
        }
        
        // 将所有选中的对象设置为正在被右手抓住的状态
        var selectedGrabables = handOutlineController.GetAllMultiSelectedGrabables();
        if (selectedGrabables != null && selectedGrabables.Count > 0)
        {
            // 使用辅助方法统一抓取所有选中的对象
            EditorPlayerHelpers.GrabMultipleObjects(selectedGrabables, rightHand, rightMultiGrabbedObjects);
            Debug.Log($"已将 {selectedGrabables.Count} 个选中的对象设置为正在被右手抓住的状态");
        }
        
        Debug.Log($"已将 {objects.Count} 个对象设置为选中状态并抓取");
    }
    #endregion

}