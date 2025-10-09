using UnityEditor;
using UnityEngine;
public class CompileControl : MonoBehaviour
{
    [MenuItem("Tools/Pause Compilation %t")]
    static void PauseCompilation()
    {
        if (EditorPrefs.GetBool("ExampleTogglePref"))
        {
            EditorApplication.UnlockReloadAssemblies();
            Debug.Log("Compilation Resumed");
            EditorPrefs.SetBool("ExampleTogglePref", false);
        }
        else
        {
            EditorApplication.LockReloadAssemblies();
            Debug.Log("Compilation Paused");
            EditorPrefs.SetBool("ExampleTogglePref", true);
        }
    }
    [MenuItem("Tools/Resume Compilation %e")]
    static void ResumeCompilation()
    {
        EditorApplication.UnlockReloadAssemblies();
        Debug.Log("Compilation Resumed");
        EditorPrefs.SetBool("ExampleTogglePref", false);
    }
}






