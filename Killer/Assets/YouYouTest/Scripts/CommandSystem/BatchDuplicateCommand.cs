using UnityEngine;
using System.Collections.Generic;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 批量复制并移动命令：在一个命令内记录多个对象的创建（Instantiate）和随后由编辑器移动的最终位置/旋转。
    /// 该命令实现 IDisposableCommand，用于被 CommandHistory 管理。
    /// </summary>
    public class BatchDuplicateCommand : IDisposableCommand
    {
        private struct ObjectDuplicateData
        {
            public GameObject prefab;               // 用于创建的预制体（原始物体）
            public GameObject createdObject;        // 创建出来的物体实例
            public Vector3 initialPosition;         // 创建时的位置（作为"开始"状态）
            public Quaternion initialRotation;      // 创建时的旋转（作为"开始"状态）
            public Vector3 finalPosition;           // 最终位置（编辑完成后由外部更新）
            public Quaternion finalRotation;        // 最终旋转（编辑完成后由外部更新）
        }

        private List<ObjectDuplicateData> _duplicateDataList;

        public BatchDuplicateCommand(List<GameObject> prefabs, List<Vector3> positions, List<Quaternion> rotations)
        {
            _duplicateDataList = new List<ObjectDuplicateData>();
            
            if (prefabs.Count != positions.Count || prefabs.Count != rotations.Count)
            {
                Debug.LogWarning("BatchDuplicateCommand: 预制体、位置或旋转列表数量不匹配");
                return;
            }

            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] != null)
                {
                    _duplicateDataList.Add(new ObjectDuplicateData
                    {
                        prefab = prefabs[i],
                        createdObject = null,
                        initialPosition = positions[i],
                        initialRotation = rotations[i],
                        finalPosition = positions[i],
                        finalRotation = rotations[i]
                    });
                }
            }
        }

        // 执行操作：第一次执行会实例化所有物体；重做会激活并设置为最终变换
        public void Execute()
        {
            for (int i = 0; i < _duplicateDataList.Count; i++)
            {
                var data = _duplicateDataList[i];
                
                if (data.createdObject == null && data.prefab != null)
                {
                    // 第一次执行，创建物体
                    data.createdObject = Object.Instantiate(data.prefab, data.initialPosition, data.initialRotation);
                    if (data.createdObject != null)
                    {
                        data.createdObject.transform.localScale = data.prefab.transform.localScale;
                    }
                }
                else if (data.createdObject != null)
                {
                    // 重做操作，激活并设置最终变换
                    data.createdObject.SetActive(true);
                    data.createdObject.transform.position = data.finalPosition;
                    data.createdObject.transform.rotation = data.finalRotation;
                }
                
                _duplicateDataList[i] = data;
            }
        }

        // 撤销操作：隐藏或禁用创建的物体（保留实例以便重做）
        public void Undo()
        {
            foreach (var data in _duplicateDataList)
            {
                if (data.createdObject != null)
                {
                    data.createdObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 更新所有对象的最终位置/旋转（在编辑器抓取并放置后由 EditorPlayer 调用）
        /// </summary>
        public void UpdateTransforms(List<Vector3> positions, List<Quaternion> rotations)
        {
            if (positions.Count != _duplicateDataList.Count || rotations.Count != _duplicateDataList.Count)
            {
                Debug.LogWarning("BatchDuplicateCommand: 位置或旋转列表数量与对象数量不匹配");
                return;
            }

            for (int i = 0; i < _duplicateDataList.Count; i++)
            {
                var data = _duplicateDataList[i];
                data.finalPosition = positions[i];
                data.finalRotation = rotations[i];

                if (data.createdObject != null)
                {
                    data.createdObject.transform.position = positions[i];
                    data.createdObject.transform.rotation = rotations[i];
                }
                
                _duplicateDataList[i] = data;
            }
        }

        /// <summary>
        /// 更新单个对象的最终位置/旋转
        /// </summary>
        public void UpdateTransform(int index, Vector3 position, Quaternion rotation)
        {
            if (index >= 0 && index < _duplicateDataList.Count)
            {
                var data = _duplicateDataList[index];
                data.finalPosition = position;
                data.finalRotation = rotation;

                if (data.createdObject != null)
                {
                    data.createdObject.transform.position = position;
                    data.createdObject.transform.rotation = rotation;
                }
                
                _duplicateDataList[index] = data;
            }
        }

        /// <summary>
        /// 获取所有创建出的物体实例列表（如果已创建），供 EditorPlayer 抓取使用
        /// </summary>
        public List<GameObject> GetCreatedObjects()
        {
            var createdObjects = new List<GameObject>();
            foreach (var data in _duplicateDataList)
            {
                if (data.createdObject != null)
                {
                    createdObjects.Add(data.createdObject);
                }
            }
            return createdObjects;
        }

        /// <summary>
        /// 获取指定索引的创建出的物体实例
        /// </summary>
        public GameObject GetCreatedObject(int index)
        {
            if (index >= 0 && index < _duplicateDataList.Count)
            {
                return _duplicateDataList[index].createdObject;
            }
            return null;
        }

        /// <summary>
        /// 获取复制对象的数量
        /// </summary>
        public int GetObjectCount()
        {
            return _duplicateDataList.Count;
        }

        /// <summary>
        /// 当命令从历史中被永久移除时，销毁所有实例以释放资源
        /// </summary>
        public void Dispose()
        {
            foreach (var data in _duplicateDataList)
            {
                if (data.createdObject != null)
                {
                    Object.DestroyImmediate(data.createdObject);
                }
            }
            _duplicateDataList.Clear();
        }
    }
}