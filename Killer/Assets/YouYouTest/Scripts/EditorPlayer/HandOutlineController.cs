using UnityEngine;
using YouYouTest.OutlineSystem;

/// <summary>
/// 管理两只手的 hover 描边显示逻辑（支持在场景中只挂载一个实例，
/// EditorPlayer 会为左右手分别调用 UpdateTarget 并传入 isLeftHand 标识）.
/// 优先级：grabbed > hold。
/// </summary>
public class HandOutlineController : MonoBehaviour
{
    [SerializeField] private Color hoverColor = Color.blue;

    // 为左右手分别维护描边接收器，以支持单实例管理两只手的描边状态
    private OutlineReceiver hoveredReceiverLeft;
    private OutlineReceiver hoveredReceiverRight;

    /// <summary>
    /// 区分左右手的标识
    /// </summary>
    public enum HandSide { Left, Right }

    /// <summary>
    /// 更新指定手的描边目标
    /// isLeftHand: true 表示左手，false 表示右手
    /// previousHold: 上一帧检测到的 hold 对象（可能为 null）
    /// currentHold: 当前检测到的 hold 对象（可能为 null）
    /// grabbedObject: 当前被抓取的对象（优先使用）
    /// </summary>
    public void UpdateTarget(bool isLeftHand, IGrabable previousHold, IGrabable currentHold, IGrabable grabbedObject)
    {
        // 优先级：grabbedObject > currentHold
        IGrabable target = grabbedObject != null ? grabbedObject : currentHold;

        // 当前手上记录的 receiver
        OutlineReceiver currentHovered = isLeftHand ? hoveredReceiverLeft : hoveredReceiverRight;

        // 目标的 OutlineReceiver（如果有）
        OutlineReceiver targetReceiver = null;
        if (target != null)
        {
            GameObject go = target.ObjectGameObject;
            if (go != null)
            {
                targetReceiver = go.GetComponentInParent<OutlineReceiver>();
            }
        }

        // 如果当前记录的 receiver 就是目标 receiver，则不需要任何操作
        if (currentHovered == targetReceiver)
            return;

        // 清除之前的描边（仅针对当前手）
        if (currentHovered != null)
        {
            // 使用 Restore 确保完整还原到原始颜色与启用状态
            currentHovered.Restore();
            if (isLeftHand) hoveredReceiverLeft = null; else hoveredReceiverRight = null;
        }

        // 设置新目标的 hover 描边（仅针对当前手）
        if (targetReceiver != null)
        {
            targetReceiver.SetState(OutlineState.Hover, hoverColor);
            if (isLeftHand) hoveredReceiverLeft = targetReceiver; else hoveredReceiverRight = targetReceiver;
        }
    }

    /// <summary>
    /// 直接清除指定手上的 hover 描边（用于程序化抓取/释放后强制清理）。
    /// </summary>
    public void ClearHover(bool isLeftHand)
    {
        var h = isLeftHand ? hoveredReceiverLeft : hoveredReceiverRight;
        if (h != null)
        {
            h.Restore();
            if (isLeftHand) hoveredReceiverLeft = null; else hoveredReceiverRight = null;
        }
    }

    /// <summary>
    /// 保持向后兼容：如果外部仍调用无 isLeftHand 的旧签名，默认作为左手处理。
    /// 建议将 EditorPlayer 更新为传入左右手标识并只使用单个 HandOutlineController 实例。
    /// </summary>
    public void UpdateTarget(IGrabable previousHold, IGrabable currentHold, IGrabable grabbedObject)
    {
        UpdateTarget(true, previousHold, currentHold, grabbedObject);
    }

    private void OnDisable()
    {
        if (hoveredReceiverLeft != null)
        {
            hoveredReceiverLeft.Restore();
            hoveredReceiverLeft = null;
        }

        if (hoveredReceiverRight != null)
        {
            hoveredReceiverRight.Restore();
            hoveredReceiverRight = null;
        }
    }
}