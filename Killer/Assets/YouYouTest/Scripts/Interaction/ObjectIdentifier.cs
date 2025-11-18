using UnityEngine;
using VInspector;
 
public class ObjectIdentifier : MonoBehaviour
{
    // 这个ID是我们手动设置的，并且在物体的生命周期内保持不变。
    // 它应该对同一种“类型”的预制体是唯一的。
    // 例如，所有“石墙”预制体都用"prop_wall_stone_01"这个ID。
    [Tooltip("这个预制体的唯一且稳定的ID，保存和加载时将使用此ID。")]
    public string prefabID;

    [Button]
    public void SetID()
    {
        prefabID = gameObject.name;
    }
}
