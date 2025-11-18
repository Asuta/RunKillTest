using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 合并的创建并移动命令：在一个命令内记录创建（Instantiate）和随后由编辑器移动的最终位置/旋转。
    /// 该命令实现 IDisposableCommand，用于被 CommandHistory 管理。
    /// </summary>
    public class CombinedCreateAndMoveCommand : IDisposableCommand
    {
        private GameObject _prefab;               // 用于创建的预制体（原始物体）
        private GameObject _createdObject;        // 创建出来的物体实例
        private Vector3 _initialPosition;         // 创建时的位置（作为“开始”状态）
        private Quaternion _initialRotation;      // 创建时的旋转（作为“开始”状态）
        private Vector3 _finalPosition;           // 最终位置（编辑完成后由外部更新）
        private Quaternion _finalRotation;        // 最终旋转（编辑完成后由外部更新）

        public CombinedCreateAndMoveCommand(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            _prefab = prefab;
            _initialPosition = position;
            _initialRotation = rotation;
            _finalPosition = position;
            _finalRotation = rotation;
        }

        // 执行操作：第一次执行会实例化物体；重做会激活并设置为最终变换
        public void Execute()
        {
            if (_createdObject == null && _prefab != null)
            {
                _createdObject = Object.Instantiate(_prefab, _initialPosition, _initialRotation);
            }
            else if (_createdObject != null)
            {
                _createdObject.SetActive(true);
                _createdObject.transform.position = _finalPosition;
                _createdObject.transform.rotation = _finalRotation;
            }
        }

        // 撤销操作：隐藏或禁用创建的物体（保留实例以便重做）
        public void Undo()
        {
            if (_createdObject != null)
            {
                _createdObject.SetActive(false);
            }
        }

        /// <summary>
        /// 更新最终位置/旋转（在编辑器抓取并放置后由 EditorPlayer 调用）
        /// </summary>
        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            _finalPosition = position;
            _finalRotation = rotation;

            if (_createdObject != null)
            {
                _createdObject.transform.position = position;
                _createdObject.transform.rotation = rotation;
            }
        }

        /// <summary>
        /// 获取创建出的物体实例（如果已创建），供 EditorPlayer 抓取使用
        /// </summary>
        public GameObject GetCreatedObject()
        {
            return _createdObject;
        }

        /// <summary>
        /// 当命令从历史中被永久移除时，销毁实例以释放资源
        /// </summary>
        public void Dispose()
        {
            if (_createdObject != null)
            {
                Object.DestroyImmediate(_createdObject);
                _createdObject = null;
            }
        }
    }
}