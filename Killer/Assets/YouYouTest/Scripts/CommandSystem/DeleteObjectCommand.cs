using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 删除物体的命令
    /// </summary>
    public class DeleteObjectCommand : ICommand
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
        /// 获取目标对象
        /// </summary>
        /// <returns>目标对象</returns>
        public GameObject GetTargetObject()
        {
            return _targetObject;
        }
    }
}