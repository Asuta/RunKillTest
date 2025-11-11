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
    }
}