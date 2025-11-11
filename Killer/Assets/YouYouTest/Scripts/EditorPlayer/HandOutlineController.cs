using UnityEngine;
using YouYouTest.OutlineSystem;

/// <summary>
/// 管理两只手的 hover 描边显示逻辑（支持在场景中只挂载一个实例，
/// EditorPlayer 会为左右手分别调用 UpdateTarget 并传入 isLeftHand 标识）.
/// 优先级：grabbed > hold。
/// </summary>
public class HandOutlineController : MonoBehaviour
{
    // 颜色已由 OutlineReceiver 内部管理，此处不再需要

    // 为左右手分别维护描边接收器，以支持单实例管理两只手的描边状态
    private OutlineReceiver hoveredReceiverLeft;
    private OutlineReceiver hoveredReceiverRight;
    // 记录上一次被选中的 Receiver（用于全局取消选中）
    private OutlineReceiver lastSelectedReceiver;

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
  
        // 清除之前的 hover 描边（仅针对当前手）
        // 使用 Receiver.ClearHover 让 Receiver 自行决定是否取消描边（Selected 会被保留）
        if (currentHovered != null)
        {
            currentHovered.ClearHover();
            if (isLeftHand) hoveredReceiverLeft = null; else hoveredReceiverRight = null;
        }
  
        // 设置新目标的 hover 描边（仅针对当前手）
        if (targetReceiver != null)
        {
            targetReceiver.OnHover();
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
            h.ClearHover();
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

    /// <summary>
    /// 将当前 hold（或接触）对象设置为 Selected 状态（用于短按选择逻辑）。
    /// isLeftHand: 指定左右手；holdObject: 要设置为 Selected 的 IGrabable（通常为当前 hold 对象）。
    /// </summary>
    public void SetSelectedForCurrentHold(bool isLeftHand, IGrabable holdObject)
    {
        // 先取消上一次的选中（如果有）
        if (lastSelectedReceiver != null)
        {
            lastSelectedReceiver.SetState(OutlineState.None);
            Debug.Log($"取消上一次的 Selected：{lastSelectedReceiver.gameObject.name}");
            lastSelectedReceiver = null;
        }

        if (holdObject == null)
        {
            Debug.Log("SetSelectedForCurrentHold: holdObject 为 null，跳过设置 Selected");
            return;
        }
 
        GameObject go = holdObject.ObjectGameObject;
        if (go == null)
        {
            Debug.LogWarning("SetSelectedForCurrentHold: holdObject.ObjectGameObject 为 null");
            return;
        }
 
        var receiver = go.GetComponentInParent<OutlineReceiver>();
        if (receiver == null)
        {
            Debug.LogWarning($"SetSelectedForCurrentHold: 对象 {go.name} 上未找到 OutlineReceiver");
            return;
        }
 
        // 设置新的选中并记录
        receiver.SetState(OutlineState.Selected);
        lastSelectedReceiver = receiver;
        // 清理当前手的 hover 记录（Receiver 自行决定是否保留描边）
        if (isLeftHand) hoveredReceiverLeft = null; else hoveredReceiverRight = null;
        Debug.Log($"设置新的 Selected：{go.name}");
    }

    /// <summary>
    /// 取消上一次的选中（用于 A 键短按但未命中任何对象时）。
    /// </summary>
    public void CancelLastSelected()
    {
        if (lastSelectedReceiver != null)
        {
            lastSelectedReceiver.SetState(OutlineState.None);
            Debug.Log($"取消上一次的 Selected（未命中对象）：{lastSelectedReceiver.gameObject.name}");
            lastSelectedReceiver = null;
        }
    }

    private void OnDisable()
    {
        if (hoveredReceiverLeft != null)
        {
            hoveredReceiverLeft.DisableOutline();
            hoveredReceiverLeft = null;
        }

        if (hoveredReceiverRight != null)
        {
            hoveredReceiverRight.DisableOutline();
            hoveredReceiverRight = null;
        }

        if (lastSelectedReceiver != null)
        {
            lastSelectedReceiver.DisableOutline();
            lastSelectedReceiver = null;
        }
    }
}
