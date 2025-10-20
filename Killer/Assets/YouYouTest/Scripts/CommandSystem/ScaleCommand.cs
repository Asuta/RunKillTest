using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 缩放物体的命令
    /// </summary>
    public class ScaleCommand : ICommand
    {
        private Transform _target;           // 目标物体
        private Vector3 _startScale;         // 缩放前的尺寸
        private Vector3 _endScale;           // 缩放后的尺寸
        private Vector3 _startPosition;      // 缩放前的位置
        private Quaternion _startRotation;   // 缩放前的角度
        private Vector3 _endPosition;        // 缩放后的位置
        private Quaternion _endRotation;     // 缩放后的角度

        public ScaleCommand(Transform target, Vector3 startScale, Vector3 startPosition, Quaternion startRotation)
        {
            _target = target;
            _startScale = startScale;
            _startPosition = startPosition;
            _startRotation = startRotation;
            // 缩放后的位置、旋转和尺寸在松开时设置，这里初始化为默认值
            _endScale = startScale;
            _endPosition = startPosition;
            _endRotation = startRotation;
        }

        // 执行操作：将物体缩放到最终尺寸、位置和角度
        public void Execute()
        {
            if (_target != null)
            {
                _target.localScale = _endScale;
                _target.position = _endPosition;
                _target.rotation = _endRotation;
            }
        }

        // 撤销操作：将物体恢复到缩放前的尺寸、位置和角度
        public void Undo()
        {
            if (_target != null)
            {
                _target.localScale = _startScale;
                _target.position = _startPosition;
                _target.rotation = _startRotation;
            }
        }

        /// <summary>
        /// 设置松开时的最终缩放、位置和旋转
        /// </summary>
        /// <param name="endScale">最终缩放</param>
        /// <param name="endPosition">最终位置</param>
        /// <param name="endRotation">最终旋转</param>
        public void SetEndTransform(Vector3 endScale, Vector3 endPosition, Quaternion endRotation)
        {
            _endScale = endScale;
            _endPosition = endPosition;
            _endRotation = endRotation;
        }
    }
}