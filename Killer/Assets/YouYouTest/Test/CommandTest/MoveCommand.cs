using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 移动物体的命令
    /// </summary>
    public class MoveCommand : ICommand
    {
        private Transform _target;      // 目标物体
        private Vector3 _startPosition; // 移动前的位置
        private Vector3 _endPosition;   // 移动后的位置
        private Quaternion _startRotation; // 移动前的角度
        private Quaternion _endRotation;   // 移动后的角度

        public MoveCommand(Transform target, Vector3 startPosition, Vector3 endPosition, Quaternion startRotation, Quaternion endRotation)
        {
            _target = target;
            _startPosition = startPosition;
            _endPosition = endPosition;
            _startRotation = startRotation;
            _endRotation = endRotation;
        }

        // 执行操作：将物体移动到目标位置和角度
        public void Execute()
        {
            _target.position = _endPosition;
            _target.rotation = _endRotation;
        }

        // 撤销操作：将物体移回原始位置和角度
        public void Undo()
        {
            _target.position = _startPosition;
            _target.rotation = _startRotation;
        }
    }
}