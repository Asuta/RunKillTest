using UnityEngine;

public class BeGrabAndScaleobject : MonoBehaviour, IGrabable
{
    // IGrabable 接口实现
    public Transform ObjectTransform => transform;
    public GameObject ObjectGameObject => gameObject;

    [Header("平滑设置")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 15f;

    [Header("跟随设置")]
    public bool freezeYaxis = false;

    private bool isGrabbed = false;
    private Transform primaryHand;   // 主手（用于位置/旋转跟随）
    private Transform secondaryHand; // 副手（用于双手缩放）

    private Vector3 offsetFromPrimary;
    private Quaternion rotationOffsetFromPrimary;

    // 双手缩放所需数据
    private bool isTwoHandScaling = false;
    private float initialHandsDistance = 0f;
    private Vector3 baseScale;
    private Quaternion twoHandRotationOffset = Quaternion.identity;

    private EditorPlayer editorPlayer; // 用于检测当前是否双手都在抓

    private void Awake()
    {
        editorPlayer = FindFirstObjectByType<EditorPlayer>();
    }

    private void Update()
    {
        // 同步两手抓取状态（以 EditorPlayer 为权威信息）
        bool leftHolding = editorPlayer != null && editorPlayer.leftGrabbedObject == (IGrabable)this;
        bool rightHolding = editorPlayer != null && editorPlayer.rightGrabbedObject == (IGrabable)this;
        bool nowTwoHand = leftHolding && rightHolding;

        // 进入双手状态时记录初始距离与缩放
        if (!isTwoHandScaling && nowTwoHand)
        {
            isTwoHandScaling = true;
            // 确认两只手引用
            if (editorPlayer != null)
            {
                if (primaryHand == null)
                {
                    // 若此前未建立主手，优先取已抓住的那只
                    primaryHand = leftHolding ? editorPlayer.leftHand : editorPlayer.rightHand;
                    offsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * (transform.position - primaryHand.position);
                    rotationOffsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * transform.rotation;
                }
                secondaryHand = (primaryHand == editorPlayer.leftHand) ? editorPlayer.rightHand : editorPlayer.leftHand;
                initialHandsDistance = Vector3.Distance(editorPlayer.leftHand.position, editorPlayer.rightHand.position);
                baseScale = transform.localScale;
                    Quaternion avgRot = Quaternion.Slerp(editorPlayer.leftHand.rotation, editorPlayer.rightHand.rotation, 0.5f);
                    twoHandRotationOffset = Quaternion.Inverse(avgRot) * transform.rotation;
            }
        }
        // 退出双手状态（其中一只手松开）
        else if (isTwoHandScaling && !nowTwoHand)
        {
            isTwoHandScaling = false;
            // 仅剩的一只手继续作为主手跟随
            if (leftHolding && editorPlayer != null)
            {
                primaryHand = editorPlayer.leftHand;
            }
            else if (rightHolding && editorPlayer != null)
            {
                primaryHand = editorPlayer.rightHand;
            }
            else
            {
                // 都不再抓，等待 OnReleased 统一清理
                primaryHand = null;
            }
            secondaryHand = null;
            // 重新计算与主手的偏移以避免跳变
            if (primaryHand != null)
            {
                offsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * (transform.position - primaryHand.position);
                rotationOffsetFromPrimary = Quaternion.Inverse(primaryHand.rotation) * transform.rotation;
            }
        }

        if (!isGrabbed || primaryHand == null) return;

        // 单手/主手 跟随位置和旋转（与 BeGrabobject 一致）
        Vector3 targetPos = primaryHand.position + primaryHand.rotation * offsetFromPrimary;
        Quaternion targetRot = primaryHand.rotation * rotationOffsetFromPrimary;

        // 双手时，位置跟随两手中点，旋转跟随两手平均朝向（保持进入时的相对旋转偏移）
        if (isTwoHandScaling && editorPlayer != null && editorPlayer.leftHand != null && editorPlayer.rightHand != null)
        {
            Vector3 mid = (editorPlayer.leftHand.position + editorPlayer.rightHand.position) * 0.5f;
            targetPos = mid;
            Quaternion avgRot = Quaternion.Slerp(editorPlayer.leftHand.rotation, editorPlayer.rightHand.rotation, 0.5f);
            targetRot = avgRot * twoHandRotationOffset;
        }

        if (freezeYaxis)
        {
            Vector3 curEuler = transform.rotation.eulerAngles;
            Vector3 targetEuler = targetRot.eulerAngles;
            targetRot = Quaternion.Euler(curEuler.x, targetEuler.y, curEuler.z);
        }

        float posAlpha = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
        float rotAlpha = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, posAlpha);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotAlpha);

        // 双手缩放：根据两手距离比例缩放
        if (isTwoHandScaling && editorPlayer != null && editorPlayer.leftHand != null && editorPlayer.rightHand != null)
        {
            float currentDistance = Vector3.Distance(editorPlayer.leftHand.position, editorPlayer.rightHand.position);
            if (initialHandsDistance > 1e-4f)
            {
                float ratio = currentDistance / initialHandsDistance;
                transform.localScale = baseScale * ratio;
            }
        }
    }

    // 抓取：单手逻辑与 BeGrabobject 一致；第二只手抓取将进入双手缩放
    public void OnGrabbed(Transform handTransform)
    {
        if (handTransform == null) return;

        if (!isGrabbed)
        {
            isGrabbed = true;
            primaryHand = handTransform;
            secondaryHand = null;
            isTwoHandScaling = false;

            offsetFromPrimary = Quaternion.Inverse(handTransform.rotation) * (transform.position - handTransform.position);
            rotationOffsetFromPrimary = Quaternion.Inverse(handTransform.rotation) * transform.rotation;

            Debug.Log($"{gameObject.name} 被 {GetHandName(handTransform)} 抓住了（单手）");
            return;
        }

        // 已被抓：若是另一只手加入，则进入双手缩放
        if (handTransform != primaryHand && secondaryHand == null)
        {
            secondaryHand = handTransform;
            // 若能拿到 EditorPlayer，立刻记录一次初始值
            if (editorPlayer == null) editorPlayer = FindFirstObjectByType<EditorPlayer>();
            if (editorPlayer != null)
            {
                initialHandsDistance = Vector3.Distance(editorPlayer.leftHand.position, editorPlayer.rightHand.position);
                baseScale = transform.localScale;
                Quaternion avgRot = Quaternion.Slerp(editorPlayer.leftHand.rotation, editorPlayer.rightHand.rotation, 0.5f);
                twoHandRotationOffset = Quaternion.Inverse(avgRot) * transform.rotation;
                isTwoHandScaling = true;
                Debug.Log($"{gameObject.name} 进入双手抓取，记录初始距离 {initialHandsDistance:F3} 与基准缩放 {baseScale}");
            }
        }
    }

    // 完全松开（EditorPlayer 在两手都不再抓时才会调用）
    public void OnReleased()
    {
        isGrabbed = false;
        isTwoHandScaling = false;
        primaryHand = null;
        secondaryHand = null;
        Debug.Log($"{gameObject.name} 被完全松开了");
    }

    // 供 EditorPlayer/其他逻辑查询：释放一只手后是否仍被另一只手抓住
    public bool IsStillGrabbedByOtherHand(Transform releasedHandTransform)
    {
        if (!isGrabbed) return false;
        if (editorPlayer == null) editorPlayer = FindFirstObjectByType<EditorPlayer>();
        bool leftHolding = editorPlayer != null && editorPlayer.leftGrabbedObject == (IGrabable)this;
        bool rightHolding = editorPlayer != null && editorPlayer.rightGrabbedObject == (IGrabable)this;
        return leftHolding || rightHolding;
    }

    private string GetHandName(Transform handTransform)
    {
        if (editorPlayer == null) editorPlayer = FindFirstObjectByType<EditorPlayer>();
        if (editorPlayer != null)
        {
            if (handTransform == editorPlayer.leftHand) return "左手";
            if (handTransform == editorPlayer.rightHand) return "右手";
        }
        return "未知手部";
    }
}
