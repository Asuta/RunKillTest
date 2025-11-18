using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 抓取并移动物体的命令
    /// </summary>
    public class GrabCommand : ICommand
    {
        private Transform _target;           // 目标物体
        private Vector3 _startPosition;      // 抓取前的位置
        private Quaternion _startRotation;   // 抓取前的角度
        private Vector3 _endPosition;        // 松开后的位置
        private Quaternion _endRotation;     // 松开后的角度

        public GrabCommand(Transform target, Vector3 startPosition, Quaternion startRotation)
        {
            _target = target;
            _startPosition = startPosition;
            _startRotation = startRotation;
            // 松开时的位置和旋转在松开时设置，这里初始化为默认值
            _endPosition = startPosition;
            _endRotation = startRotation;
        }

        // 执行操作：将物体移动到最终位置和角度
        public void Execute()
        {
            if (_target != null)
            {
                _target.position = _endPosition;
                _target.rotation = _endRotation;
            }
        }

        // 撤销操作：将物体移回抓取前的位置和角度
        public void Undo()
        {
            if (_target != null)
            {
                _target.position = _startPosition;
                _target.rotation = _startRotation;
            }
        }

        /// <summary>
        /// 设置松开时的最终位置和旋转
        /// </summary>
        /// <param name="endPosition">最终位置</param>
        /// <param name="endRotation">最终旋转</param>
        public void SetEndTransform(Vector3 endPosition, Quaternion endRotation)
        {
            _endPosition = endPosition;
            _endRotation = endRotation;
        }
    }
}