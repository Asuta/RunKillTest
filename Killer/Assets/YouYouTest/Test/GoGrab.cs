using UnityEngine;

public class GoGrab : MonoBehaviour
{
    // 要测试的目标（在 Inspector 中指定）
    public BeGrabAndScaleobject target;

    // 按 G 键开始间接抓取，松开 G 键停止
    void Update()
    {
        if (target == null) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            // 将自己的 Transform 传入目标的间接抓取方法
            target.StartIndirectGrab(this.transform);
            Debug.Log($"GoGrab: StartIndirectGrab -> {target.name}");
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            target.StopIndirectGrab();
            Debug.Log($"GoGrab: StopIndirectGrab -> {target.name}");
        }
    }
}
