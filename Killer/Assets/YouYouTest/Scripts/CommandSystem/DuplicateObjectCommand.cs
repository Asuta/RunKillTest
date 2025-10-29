using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 复制物体的命令
    /// </summary>
    public class DuplicateObjectCommand : IDisposableCommand
    {
        private GameObject _originalObject;  // 原始物体
        private GameObject _duplicatedObject; // 复制出来的物体
        private Vector3 _duplicatePosition;   // 复制时的位置
        private Quaternion _duplicateRotation; // 复制时的旋转

        public DuplicateObjectCommand(GameObject originalObject, Vector3 position, Quaternion rotation)
        {
            _originalObject = originalObject;
            _duplicatePosition = position;
            _duplicateRotation = rotation;
        }

        // 执行操作：复制物体
        public void Execute()
        {
            // 如果是第一次执行，就复制物体
            if (_duplicatedObject == null && _originalObject != null)
            {
                _duplicatedObject = Object.Instantiate(_originalObject, _duplicatePosition, _duplicateRotation);
                
                // 确保复制的物体有IGrabable组件
                if (_duplicatedObject.GetComponent<IGrabable>() == null)
                {
                    var originalGrabable = _originalObject.GetComponent<IGrabable>();
                    if (originalGrabable != null)
                    {
                        // 如果原始物体有IGrabable组件，尝试添加相同的组件
                        var grabableComponent = _duplicatedObject.AddComponent(originalGrabable.GetType());
                        // 注意：这里可能需要额外的初始化逻辑，取决于IGrabable的具体实现
                    }
                }
            }
            // 如果是重做，就重新激活它
            else if (_duplicatedObject != null)
            {
                _duplicatedObject.SetActive(true);
                _duplicatedObject.transform.position = _duplicatePosition;
                _duplicatedObject.transform.rotation = _duplicateRotation;
            }
        }

        // 撤销操作：销毁或隐藏复制的物体
        public void Undo()
        {
            if (_duplicatedObject != null)
            {
                _duplicatedObject.SetActive(false);
            }
        }

        /// <summary>
        /// 释放此命令创建的资源，当命令从历史记录中被永久移除时调用
        /// </summary>
        public void Dispose()
        {
            if (_duplicatedObject != null)
            {
                Object.DestroyImmediate(_duplicatedObject);
                _duplicatedObject = null;
            }
        }
        
        /// <summary>
        /// 获取复制的物体
        /// </summary>
        /// <returns>复制的物体，如果还未复制则返回null</returns>
        public GameObject GetDuplicatedObject()
        {
            return _duplicatedObject;
        }
        
        /// <summary>
        /// 更新复制物体的位置和旋转（用于抓取移动后更新命令状态）
        /// </summary>
        /// <param name="position">新位置</param>
        /// <param name="rotation">新旋转</param>
        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            _duplicatePosition = position;
            _duplicateRotation = rotation;
            
            if (_duplicatedObject != null)
            {
                _duplicatedObject.transform.position = position;
                _duplicatedObject.transform.rotation = rotation;
            }
        }
    }
}