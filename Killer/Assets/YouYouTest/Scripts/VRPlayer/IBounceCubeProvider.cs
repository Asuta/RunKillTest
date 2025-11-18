using System;
using UnityEngine;

public interface IBounceCubeProvider
{

    // 当进入弹跳状态时，传出当前弹跳面的法线（世界坐标）
    void OutHandleBounceCube(Vector3 normal);

}