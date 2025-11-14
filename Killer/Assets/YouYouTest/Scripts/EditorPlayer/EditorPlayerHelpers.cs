using UnityEngine;
using YouYouTest.CommandFramework;
using YouYouTest.OutlineSystem;

namespace YouYouTest
{
    public static class EditorPlayerHelpers
    {
        public static IGrabable GetGrabableFromGameObject(GameObject target)
        {
            if (target == null) return null;
            var g = target.GetComponent<IGrabable>();
            if (g != null) return g;
            var rb = target.GetComponent<Rigidbody>();
            if (rb != null) return rb.GetComponent<IGrabable>();
            return null;
        }

        public static GrabCommand CreateGrabCommandIfNotDuplicated(YouYouTest.CommandFramework.CombinedCreateAndMoveCommand duplicateCmd, IGrabable grabable)
        {
            if (grabable == null) return null;
            if (duplicateCmd != null && duplicateCmd.GetCreatedObject() == grabable.ObjectGameObject)
                return null;
            return new GrabCommand(grabable.ObjectTransform, grabable.ObjectTransform.position, grabable.ObjectTransform.rotation);
        }

        public static IGrabable DetectGrabableObject(Transform checkSphere, Collider[] hitColliders)
        {
            if (checkSphere == null) return null;
            float radius = Mathf.Max(checkSphere.lossyScale.x, checkSphere.lossyScale.y, checkSphere.lossyScale.z);
            int hitCount = Physics.OverlapSphereNonAlloc(checkSphere.position, radius, hitColliders);
            for (int i = 0; i < hitCount; i++)
            {
                var rb = hitColliders[i].attachedRigidbody;
                if (rb != null)
                {
                    var g = rb.GetComponent<IGrabable>();
                    if (g != null) return g;
                }
            }
            return null;
        }

        public static void ExecuteDelete(IGrabable holdObject, ref IGrabable grabbedObject, ref GrabCommand currentGrabCommand, Transform hand, bool isLeftHand, HandOutlineController handOutline)
        {
            if (holdObject == null) { Debug.Log(isLeftHand ? "左手没有hold任何物体，无法删除" : "右手没有hold任何物体，无法删除"); return; }
            var objectToDelete = holdObject.ObjectGameObject;
            var deleteCommand = new DeleteObjectCommand(objectToDelete);
            CommandHistory.Instance.ExecuteCommand(deleteCommand);

            if (grabbedObject == holdObject)
            {
                grabbedObject.OnReleased(hand);
                grabbedObject = null;
                currentGrabCommand = null;
            }

            Debug.Log($"{(isLeftHand ? "左手" : "右手")} 删除物体 {objectToDelete.name}");
            // 清理描边（如果需要）
            handOutline?.UpdateTarget(isLeftHand, null, null, null);
        }

        public static YouYouTest.CommandFramework.CombinedCreateAndMoveCommand CreateDuplicateAndGrab(IGrabable sourceObject, bool isLeftHand)
        {
            if (sourceObject == null) return null;
            var original = sourceObject.ObjectGameObject;
            var cmd = new YouYouTest.CommandFramework.CombinedCreateAndMoveCommand(original, original.transform.position, original.transform.rotation);
            CommandHistory.Instance.ExecuteCommand(cmd);
            var duplicated = cmd.GetCreatedObject();
            if (duplicated != null)
            {
                duplicated.transform.localScale = original.transform.localScale;
                Debug.Log($"复制物体: {original.name} -> {duplicated.name}");
            }
            else
            {
                Debug.LogWarning("复制物体失败");
                return null;
            }
            return cmd;
        }

        public static void UpdateDuplicateOnRelease(YouYouTest.CommandFramework.CombinedCreateAndMoveCommand cmd, IGrabable grabbedObject)
        {
            if (cmd != null && grabbedObject != null && grabbedObject.ObjectGameObject == cmd.GetCreatedObject())
            {
                cmd.UpdateTransform(grabbedObject.ObjectTransform.position, grabbedObject.ObjectTransform.rotation);
            }
        }

        public static void ReleaseGrab(ref IGrabable grabbedObject, ref GrabCommand currentGrabCommand, Transform hand, bool isLeftHand, HandOutlineController handOutline)
        {
            if (grabbedObject == null) { Debug.LogWarning((isLeftHand ? "左手" : "右手") + "没有抓取任何物体"); return; }
            var host = grabbedObject as MonoBehaviour;
            if (host == null)
            {
                Debug.LogWarning((isLeftHand ? "左手" : "右手") + "抓取的对象已被销毁，跳过释放与命令记录");
                grabbedObject = null; currentGrabCommand = null; return;
            }

            grabbedObject.OnReleased(hand);
            handOutline?.UpdateTarget(isLeftHand, null, null, null);

            if (currentGrabCommand != null)
            {
                currentGrabCommand.SetEndTransform(grabbedObject.ObjectTransform.position, grabbedObject.ObjectTransform.rotation);
                CommandHistory.Instance.ExecuteCommand(currentGrabCommand);
            }

            Debug.Log($"{(isLeftHand ? "左手" : "右手")}松开了抓取的物体: {grabbedObject.ObjectGameObject.name}");
            grabbedObject = null; currentGrabCommand = null;
        }

        /// <summary>
        /// 释放多抓取对象
        /// </summary>
        /// <param name="multiGrabbedObjects">多抓取对象列表</param>
        /// <param name="hand">手部Transform</param>
        /// <param name="isLeftHand">是否为左手</param>
        public static void ReleaseMultiGrabbedObjects(System.Collections.Generic.List<IGrabable> multiGrabbedObjects, Transform hand, bool isLeftHand)
        {
            if (multiGrabbedObjects.Count > 0)
            {
                foreach (var grabable in multiGrabbedObjects)
                {
                    if (grabable != null)
                    {
                        grabable.OnReleased(hand);
                    }
                }
                multiGrabbedObjects.Clear();
                Debug.Log($"释放{(isLeftHand ? "左手" : "右手")}多抓取的所有对象");
            }
        }

        /// <summary>
        /// 抓取多个对象（统一使用间接抓取）
        /// </summary>
        /// <param name="sourceObjects">源对象列表</param>
        /// <param name="hand">手部Transform</param>
        /// <param name="multiGrabbedObjects">多抓取对象列表（输出参数）</param>
        public static void GrabMultipleObjects(System.Collections.Generic.List<IGrabable> sourceObjects, Transform hand, System.Collections.Generic.List<IGrabable> multiGrabbedObjects)
        {
            if (sourceObjects == null || sourceObjects.Count == 0) return;

            multiGrabbedObjects.Clear();

            foreach (var grabable in sourceObjects)
            {
                if (grabable == null) continue;
                GameObject go = grabable.ObjectGameObject;
                if (go == null) continue;

                // 统一对所有对象使用间接抓取
                if (grabable is BeGrabAndScaleobject bg)
                {
                    bg.StartIndirectGrab(hand);
                }
                else
                {
                    grabable.OnGrabbed(hand);
                }

                multiGrabbedObjects.Add(grabable);
            }
        }

        /// <summary>
        /// 根据 HandOutlineController 的多选集合执行多抓取
        /// </summary>
        /// <param name="selectedGrabables">当前多选的 IGrabable 集合</param>
        /// <param name="hand">执行抓取的手部 Transform</param>
        /// <param name="multiGrabbedObjects">该手维护的多抓取列表</param>
        /// <param name="releaseAction">用于释放当前抓取状态的委托</param>
        /// <param name="isLeftHand">是否为左手</param>
        /// <returns>若成功抓取到任意对象则返回 true</returns>
        public static bool GrabMultiSelection(
            System.Collections.Generic.IList<IGrabable> selectedGrabables,
            Transform hand,
            System.Collections.Generic.List<IGrabable> multiGrabbedObjects,
            System.Action releaseAction,
            bool isLeftHand)
        {
            if (selectedGrabables == null || selectedGrabables.Count == 0) return false;
            if (hand == null)
            {
                Debug.LogError($"{(isLeftHand ? "左手" : "右手")}多选抓取失败：手部Transform未分配");
                return false;
            }

            releaseAction?.Invoke();
            multiGrabbedObjects.Clear();

            int grabbedCount = 0;
            foreach (var grabable in selectedGrabables)
            {
                if (grabable == null) continue;
                GameObject go = grabable.ObjectGameObject;
                if (go == null) continue;

                grabable.UnifiedGrab(hand);
                multiGrabbedObjects.Add(grabable);
                grabbedCount++;
            }

            if (grabbedCount == 0)
            {
                Debug.LogWarning($"{(isLeftHand ? "左手" : "右手")}多选抓取失败：没有有效的IGrabable对象");
                return false;
            }

            Debug.Log($"{(isLeftHand ? "左手" : "右手")}多选抓取 {grabbedCount} 个对象");
            return true;
        }

        /// <summary>
        /// 执行多选检测，将检测范围内的对象添加到描边控制器
        /// </summary>
        /// <param name="checkSphere">检测球体</param>
        /// <param name="hitColliders">Overlap 用的缓冲区</param>
        /// <param name="handOutlineController">描边控制器</param>
        public static void PerformMultiSelectionCheck(
            Transform checkSphere,
            Collider[] hitColliders,
            HandOutlineController handOutlineController)
        {
            if (checkSphere == null)
            {
                Debug.LogWarning("右手检测球体为空，无法执行多选检测");
                return;
            }

            if (handOutlineController == null)
            {
                Debug.LogWarning("HandOutlineController 未设置，无法执行多选检测");
                return;
            }

            float radius = Mathf.Max(checkSphere.lossyScale.x, checkSphere.lossyScale.y, checkSphere.lossyScale.z);
            int hitCount = Physics.OverlapSphereNonAlloc(checkSphere.position, radius, hitColliders);

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = hitColliders[i];
                if (collider == null) continue;

                var rb = collider.attachedRigidbody;
                if (rb == null) continue;

                var grabable = rb.GetComponent<IGrabable>();
                if (grabable == null) continue;

                GameObject targetObject = grabable.ObjectGameObject;
                if (targetObject == null) continue;

                OutlineReceiver receiver = targetObject.GetComponentInParent<OutlineReceiver>();
                if (receiver == null) continue;

                handOutlineController.AddToMultiSelection(receiver);
            }
        }

        /// <summary>
        /// 处理多选复制的释放
        /// </summary>
        /// <param name="currentMultiDuplicateCommands">当前多选复制命令列表</param>
        /// <param name="currentDuplicateCommand">当前单选复制命令</param>
        /// <param name="rightGrabbedObject">右手抓取的对象</param>
        public static void ReleaseMultiDuplicateObjects(
            System.Collections.Generic.List<YouYouTest.CommandFramework.CombinedCreateAndMoveCommand> currentMultiDuplicateCommands,
            YouYouTest.CommandFramework.CombinedCreateAndMoveCommand currentDuplicateCommand,
            IGrabable rightGrabbedObject)
        {
            // 检查是否是多选复制
            if (currentMultiDuplicateCommands.Count > 0)
            {
                // 处理多选复制的释放
                foreach (var duplicateCommand in currentMultiDuplicateCommands)
                {
                    if (duplicateCommand != null)
                    {
                        var duplicatedObject = duplicateCommand.GetCreatedObject();
                        if (duplicatedObject != null)
                        {
                            var grabable = GetGrabableFromGameObject(duplicatedObject);
                            if (grabable != null)
                            {
                                // 更新所有多抓取对象的复制命令
                                UpdateDuplicateOnRelease(duplicateCommand, grabable);
                            }
                        }
                    }
                }
                currentMultiDuplicateCommands.Clear();
                Debug.Log($"释放右手多选复制的物体，共处理 {currentMultiDuplicateCommands.Count} 个复制命令");
            }
            else if (rightGrabbedObject != null)
            {
                // 处理单选复制的释放
                UpdateDuplicateOnRelease(currentDuplicateCommand, rightGrabbedObject);
                Debug.Log("释放右手单选复制的物体");
            }
        }
    }
}