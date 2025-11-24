using UnityEngine;

public class GoGrab : MonoBehaviour
{
    // 要测试的目标（在 Inspector 中指定）
    public IGrabable target;

    // 按 G 键开始间接抓取，松开 G 键停止
    void Update()
    {
        if (target == null) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            // 将自己的 Transform 传入目标的间接抓取方法
            target.BatchIndirectGrab(this.transform, this.transform);
            Debug.Log($"GoGrab: StartIndirectGrab -> {target.ObjectGameObject.name}");
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            // 批量间接抓取不需要停止方法，因为会被新的批量抓取覆盖
            Debug.Log($"GoGrab: StopIndirectGrab -> {target.ObjectGameObject.name}");
        }
    }
}
