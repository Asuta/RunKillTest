using UnityEngine;

namespace YouYouTest.CommandTest
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
    }
}