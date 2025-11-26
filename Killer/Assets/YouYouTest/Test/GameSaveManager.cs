using UnityEngine;
using System.Collections.Generic;
using System.IO;
using VInspector;

public class GameSaveManager : MonoBehaviour
{
    public List<GameObject> selectedObjects; // 假设这是你在运行时选中的物体 a, b, c
    public SnapshotSaveType saveType = SnapshotSaveType.TypeSelect; // 默认保存到a文件夹
    public string snapshotName = "snapshot"; // 图片的名称

    [Button]
    public void SaveGame()
    {
        // 执行截图
        ObjectSnapshot.CaptureAndSave(selectedObjects, saveType, snapshotName);

        Debug.Log($"存档缩略图已生成，保存类型: {saveType}");
    }
}
