using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 创建物体的命令
    /// </summary>
    public class CreateObjectCommand : IDisposableCommand
    {
        private GameObject _createdObject; // 被创建的物体
        private Vector3 _position;          // 创建的位置
        private GameObject _prefab;         // 用于创建的预制体

        public CreateObjectCommand(GameObject prefab, Vector3 position)
        {
            _prefab = prefab;
            _position = position;
        }

        // 执行操作：实例化物体
        public void Execute()
        {
            // 如果是第一次执行，就创建物体
            if (_createdObject == null)
            {
                _createdObject = Object.Instantiate(_prefab, _position, Quaternion.identity);
            }
            // 如果是重做，就重新激活它
            else if (_createdObject != null) // 增加健壮性检查
            {
                _createdObject.SetActive(true);
            }
        }

        // 撤销操作：销毁或隐藏物体
        public void Undo()
        {
            // 为了性能，通常选择隐藏而不是销毁，因为重做时无需重新实例化
            if (_createdObject != null)
            {
                _createdObject.SetActive(false);
            }
        }

        /// <summary>
        /// 释放此命令创建的资源，当命令从历史记录中被永久移除时调用
        /// </summary>
        public void Dispose()
        {
            if (_createdObject != null)
            {
                Object.DestroyImmediate(_createdObject);
                _createdObject = null; // 避免重复销毁和悬空引用
            }
        }
        
        /// <summary>
        /// 获取创建的物体
        /// </summary>
        /// <returns>创建的物体，如果还未创建则返回null</returns>
        public GameObject GetCreatedObject()
        {
            return _createdObject;
        }
    }
}