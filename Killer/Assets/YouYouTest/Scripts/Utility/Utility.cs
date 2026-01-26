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

    /// <summary>
    /// 播放 2D 音效（播放完后自动销毁）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="volume">音量</param>
    public static void PlayClip2D(AudioClip clip, float volume = 1f)
    {
        GameObject tempGO = new GameObject("TempAudio2D");
        AudioSource aSource = tempGO.AddComponent<AudioSource>();

        aSource.clip = clip;
        aSource.volume = volume;

        // 关键设置：将空间融合设为 0 (完全 2D) 
        aSource.spatialBlend = 0f;

        aSource.Play();

        // 播放完后自动销毁 
        Object.Destroy(tempGO, clip.length);
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
