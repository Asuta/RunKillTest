using System;
using UnityEngine;

public interface IWallSlidingProvider
{

    // 当进入贴墙滑行时，传出当前墙面的法线（世界坐标）
    event Action<Vector3> OnEnterWallSliding;

    // 当退出贴墙滑行时，传出之前记录的墙面法线（世界坐标）以便恢复相关状态
    event Action<Vector3> OnExitWallSliding;
    // event Action<int> OnValueChanged;

}