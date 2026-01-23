using UnityEditor;
using UnityEngine;

namespace AudioClipEditor
{
    public static class OpenAudioClipEditor
    {
        public static void OpenWaveformEditor()
        {
            EditorApplication.ExecuteMenuItem("Window/AudioClip Editor/Waveform Editor");
        }
        
        public static void OpenBatchProcessClipsEditor()
        {
            EditorApplication.ExecuteMenuItem("Window/AudioClip Editor/Batch Process Clips");
        }
    }
}
