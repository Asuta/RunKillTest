using UnityEngine;

public static class Utility
{
    /// <summary>
    /// 计算两个Vector3点之间的水平距离（忽略Y轴高度差）
    /// </summary>
    /// <param name="pointA">第一个点</param>
    /// <param name="pointB">第二个点</param>
    /// <returns>水平距离</returns>
    public static float CalculateHorizontalDistance(Vector3 pointA, Vector3 pointB)
    {
        Vector3 a = new Vector3(pointA.x, 0, pointA.z);
        Vector3 b = new Vector3(pointB.x, 0, pointB.z);
        return Vector3.Distance(a, b);
    }
}



public static class CustomLog 
{
    // // define a enum
    // public enum LogLevel
    // {
    //     Info,
    //     Warning,
    //     Error
    // }
    /// <summary>
    /// 计算两个Vector3点之间的水平距离（忽略Y轴高度差）
    /// </summary>
    /// <param name="pointA">第一个点</param>
    /// <param name="pointB">第二个点</param>
    /// <returns>水平距离</returns>
    public static void Log(bool needLog, string message)
    {
        if (needLog)
        {
            Debug.Log(message);
        }

    }

    public static void LogWarning(bool needLog, string message)
    {
        if (needLog)
        {
            Debug.LogWarning(message);
        }

    }

    public static void LogError(bool needLog, string message)
    {
        if (needLog)
        {
            Debug.LogError(message);
        }

    }
}
