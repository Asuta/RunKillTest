using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 将一个Transform的抖动位置和旋转进行平滑处理，并应用到另一个Transform上。
/// </summary>
public class Debounce : MonoBehaviour
{
    public enum DebounceMethod
    {
        None,
        MovingAverage,
        ExponentialMovingAverage
    }

    [Header("核心设置")]
    [Tooltip("抖动的源对象 (A)")]
    public Transform sourceTransform;

    [Tooltip("应用平滑变换的目标对象 (B)")]
    public Transform targetTransform;

    [Header("平滑算法")]
    [Tooltip("选择要使用的去抖动方法")]
    public DebounceMethod selectedMethod = DebounceMethod.ExponentialMovingAverage;

    [Header("移动平均滤波 (Moving Average)")]
    [Tooltip("用于计算移动平均的样本数量")]
    [Range(2, 100)]
    public int movingAverageWindowSize = 10;

    [Header("指数移动平均 (Exponential Moving Average)")]
    [Tooltip("平滑因子，值越小越平滑，但响应越慢")]
    [Range(0.01f, 1.0f)]
    public float emaAlpha = 0.1f;

    // --- 数据存储 ---
    // 位置平滑
    private Queue<Vector3> _posQueue = new Queue<Vector3>();
    private Vector3 _lastEmaPos;

    // 旋转平滑
    private Queue<Quaternion> _rotQueue = new Queue<Quaternion>();
    private Quaternion _lastEmaRot;


    void Start()
    {
        if (sourceTransform == null || targetTransform == null)
        {
            Debug.LogError("请在Inspector中指定Source Transform和Target Transform！", this);
            this.enabled = false;
            return;
        }

        // 初始化位置和旋转，避免第一帧跳变
        _lastEmaPos = sourceTransform.position;
        _lastEmaRot = sourceTransform.rotation;
        
        _posQueue.Enqueue(sourceTransform.position);
        _rotQueue.Enqueue(sourceTransform.rotation);

        targetTransform.position = sourceTransform.position;
        targetTransform.rotation = sourceTransform.rotation;
    }

    void LateUpdate()
    {
        if (sourceTransform == null || targetTransform == null) return;

        Vector3 smoothedPos;
        Quaternion smoothedRot;

        // 根据所选方法更新平滑值
        switch (selectedMethod)
        {
            case DebounceMethod.None:
                smoothedPos = sourceTransform.position;
                smoothedRot = sourceTransform.rotation;
                break;
            case DebounceMethod.MovingAverage:
                smoothedPos = ApplyMovingAverage(sourceTransform.position);
                smoothedRot = ApplyMovingAverage(sourceTransform.rotation);
                break;
            case DebounceMethod.ExponentialMovingAverage:
            default:
                smoothedPos = ApplyExponentialMovingAverage(sourceTransform.position);
                smoothedRot = ApplyExponentialMovingAverage(sourceTransform.rotation);
                break;
        }

        targetTransform.position = smoothedPos;
        targetTransform.rotation = smoothedRot;
    }

    #region Vector3 Smoothing
    private Vector3 ApplyMovingAverage(Vector3 newValue)
    {
        _posQueue.Enqueue(newValue);
        while (_posQueue.Count > movingAverageWindowSize)
        {
            _posQueue.Dequeue();
        }

        Vector3 sum = Vector3.zero;
        foreach (var v in _posQueue) sum += v;
        return sum / _posQueue.Count;
    }

    private Vector3 ApplyExponentialMovingAverage(Vector3 newValue)
    {
        _lastEmaPos = Vector3.Lerp(_lastEmaPos, newValue, emaAlpha);
        return _lastEmaPos;
    }
    #endregion

    #region Quaternion Smoothing
    private Quaternion ApplyMovingAverage(Quaternion newValue)
    {
        _rotQueue.Enqueue(newValue);
        while (_rotQueue.Count > movingAverageWindowSize)
        {
            _rotQueue.Dequeue();
        }
        
        // 对四元数进行平均（使用Slerp链）
        // 注意：这对于大的窗口和剧烈旋转可能不是最优的，但对于平滑抖动足够了
        if (_rotQueue.Count == 0) return Quaternion.identity;
        
        Quaternion average = _rotQueue.First();
        int i = 1;
        foreach (var q in _rotQueue.Skip(1))
        {
            i++;
            average = Quaternion.Slerp(average, q, 1.0f / i);
        }
        return average;
    }

    private Quaternion ApplyExponentialMovingAverage(Quaternion newValue)
    {
        _lastEmaRot = Quaternion.Slerp(_lastEmaRot, newValue, emaAlpha);
        return _lastEmaRot;
    }
    #endregion
}
