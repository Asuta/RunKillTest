using UnityEngine;
using System.Collections.Generic;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 批量移动命令：管理多个对象的移动操作
    /// </summary>
    public class BatchMoveCommand : ICommand
    {
        private struct ObjectMoveData
        {
            public Transform target;
            public Vector3 startPosition;
            public Quaternion startRotation;
            public Vector3 endPosition;
            public Quaternion endRotation;
        }

        private List<ObjectMoveData> _moveDataList;

        public BatchMoveCommand(List<Transform> targets)
        {
            _moveDataList = new List<ObjectMoveData>();
            
            foreach (var target in targets)
            {
                if (target != null)
                {
                    _moveDataList.Add(new ObjectMoveData
                    {
                        target = target,
                        startPosition = target.position,
                        startRotation = target.rotation,
                        endPosition = target.position,
                        endRotation = target.rotation
                    });
                }
            }
        }

        /// <summary>
        /// 设置所有对象的最终位置和旋转
        /// </summary>
        public void SetEndTransforms(List<Vector3> endPositions, List<Quaternion> endRotations)
        {
            if (endPositions.Count != _moveDataList.Count || endRotations.Count != _moveDataList.Count)
            {
                Debug.LogWarning("BatchMoveCommand: 位置或旋转列表数量与对象数量不匹配");
                return;
            }

            for (int i = 0; i < _moveDataList.Count; i++)
            {
                _moveDataList[i] = new ObjectMoveData
                {
                    target = _moveDataList[i].target,
                    startPosition = _moveDataList[i].startPosition,
                    startRotation = _moveDataList[i].startRotation,
                    endPosition = endPositions[i],
                    endRotation = endRotations[i]
                };
            }
        }

        /// <summary>
        /// 设置单个对象的最终位置和旋转
        /// </summary>
        public void SetEndTransform(int index, Vector3 position, Quaternion rotation)
        {
            if (index >= 0 && index < _moveDataList.Count)
            {
                var data = _moveDataList[index];
                _moveDataList[index] = new ObjectMoveData
                {
                    target = data.target,
                    startPosition = data.startPosition,
                    startRotation = data.startRotation,
                    endPosition = position,
                    endRotation = rotation
                };
            }
        }

        // 执行操作：将所有物体移动到最终位置和角度
        public void Execute()
        {
            foreach (var data in _moveDataList)
            {
                if (data.target != null)
                {
                    data.target.position = data.endPosition;
                    data.target.rotation = data.endRotation;
                }
            }
        }

        // 撤销操作：将所有物体移回原始位置和角度
        public void Undo()
        {
            foreach (var data in _moveDataList)
            {
                if (data.target != null)
                {
                    data.target.position = data.startPosition;
                    data.target.rotation = data.startRotation;
                }
            }
        }

        /// <summary>
        /// 获取移动对象的数量
        /// </summary>
        public int GetObjectCount()
        {
            return _moveDataList.Count;
        }

        /// <summary>
        /// 获取指定索引的对象Transform
        /// </summary>
        public Transform GetTarget(int index)
        {
            if (index >= 0 && index < _moveDataList.Count)
            {
                return _moveDataList[index].target;
            }
            return null;
        }
    }
}