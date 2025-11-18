using UnityEngine;
using VInspector;

public class SaveTest : MonoBehaviour
{
    public string achiveName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    [Button]
    public void Save()
    {
        SaveLoadManager.Instance.SaveSceneObjects(achiveName);
    }

    [Button]
    public void Load()
    {
        SaveLoadManager.Instance.LoadSceneObjects(achiveName);
    }

    [Button]
    public void Delete()
    {
        SaveLoadManager.Instance.DeleteSaveSlot(achiveName);
    }

    [Button]
    public void Debug()
    {
        SaveLoadManager.Instance.DebugAllSaveSlots();
    }

    [Button]
    public void DebugResources()
    {
        SaveLoadManager.Instance.DebugResourcesContent();
    }

    // [Button]
    // public void GetAllSaveSlots()
    // {
    //     SaveLoadManager.Instance.GetAllSaveSlots();
    // }
}
