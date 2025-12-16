using System.Collections.Generic;
using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 批量删除物体的命令：对多个对象进行一次性删除/撤销
    /// </summary>
    public class BatchDeleteObjectCommand : IDisposableCommand
    {
        private struct ObjectDeleteData
        {
            public GameObject target;
            public bool wasActive;
        }

        private readonly List<ObjectDeleteData> _targets;

        public BatchDeleteObjectCommand(IEnumerable<GameObject> targets)
        {
            _targets = new List<ObjectDeleteData>();

            if (targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                if (target == null) continue;

                _targets.Add(new ObjectDeleteData
                {
                    target = target,
                    wasActive = target.activeSelf
                });
            }
        }

        public void Execute()
        {
            foreach (var data in _targets)
            {
                if (data.target != null)
                {
                    data.target.SetActive(false);
                }
            }
        }

        public void Undo()
        {
            foreach (var data in _targets)
            {
                if (data.target != null)
                {
                    data.target.SetActive(data.wasActive);
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i].target;
                if (target == null) continue;

                if (Application.isPlaying)
                {
                    Object.Destroy(target);
                }
                else
                {
                    Object.DestroyImmediate(target);
                }

                _targets[i] = new ObjectDeleteData
                {
                    target = null,
                    wasActive = _targets[i].wasActive
                };
            }
        }
    }
}
