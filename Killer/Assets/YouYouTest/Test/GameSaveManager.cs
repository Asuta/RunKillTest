using UnityEngine;
using System.Collections.Generic;
using System.IO;
using VInspector;

public class GameSaveManager : MonoBehaviour
{
    public ObjectSnapshot snapshotTool;
    public List<GameObject> selectedObjects; // 假设这是你在运行时选中的物体 a, b, c


    [Button]
    public void SaveGame()
    {
        string path = Path.Combine(Application.persistentDataPath, "save_thumb.png");

        // 执行截图
        snapshotTool.CaptureAndSave(selectedObjects, path);

        Debug.Log("存档缩略图已生成");
    }
}
