using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 删除物体的命令
    /// </summary>
    public class DeleteObjectCommand : IDisposableCommand
    {
        private GameObject _targetObject;

        public DeleteObjectCommand(GameObject target)
        {
            _targetObject = target;
        }
  
        public void Execute()
        {
            if (_targetObject != null)
            {
                _targetObject.SetActive(false);
            }
        }

        public void Undo()
        {
            if (_targetObject != null)
            {
                _targetObject.SetActive(true);
            }
        }

        /// <summary>
        /// 释放此命令持有的资源，当命令从历史记录中被永久移除时调用
        /// </summary>
        public void Dispose()
        {
            if (_targetObject != null)
            {
                Object.DestroyImmediate(_targetObject);
                _targetObject = null; // 避免重复销毁和悬空引用
            }
        }
        
        /// <summary>
        /// 获取目标对象
        /// </summary>
        /// <returns>目标对象</returns>
        public GameObject GetTargetObject()
        {
            return _targetObject;
        }
    }
}