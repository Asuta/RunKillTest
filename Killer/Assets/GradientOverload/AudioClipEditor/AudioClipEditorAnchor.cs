using UnityEngine;

namespace AudioClipEditor
{
    /// This class is used to locate the AudioClipEditor folder which is needed for saving UneditedClips.
    /// This approach avoids the need to hardcode the path to the AudioClipEditor folder, which means
    /// you are free to move the folder around.
    /// 
    /// Please don't delete this class!
    public class AudioClipEditorAnchor : ScriptableObject {}
}
