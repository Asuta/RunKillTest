using UnityEngine;

namespace YouYouTest.CommandTest
{
    /// <summary>
    /// 移动物体的命令
    /// </summary>
    public class MoveCommand : ICommand
    {
        private Transform _target;      // 目标物体
        private Vector3 _startPosition; // 移动前的位置
        private Vector3 _endPosition;   // 移动后的位置

        public MoveCommand(Transform target, Vector3 startPosition, Vector3 endPosition)
        {
            _target = target;
            _startPosition = startPosition;
            _endPosition = endPosition;
        }

        // 执行操作：将物体移动到目标位置
        public void Execute()
        {
            _target.position = _endPosition;
        }

        // 撤销操作：将物体移回原始位置
        public void Undo()
        {
            _target.position = _startPosition;
        }
    }
}