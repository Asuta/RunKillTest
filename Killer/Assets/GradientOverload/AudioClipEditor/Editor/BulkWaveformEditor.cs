using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AudioClipEditor
{
    public class BulkWaveformEditor : EditorWindow
    {
        private List<AudioClip> audioClips;
        private static float volume = 1.0f;
        private static float silenceThreshold = 0.05f;
    
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            AudioClip[] selectedAudioClips = Selection.GetFiltered<AudioClip>(SelectionMode.DeepAssets);
            audioClips = new List<AudioClip>(selectedAudioClips);
           
            Repaint();
        }
    
        [MenuItem("Assets/AudioClip Editor/Batch Process Clips", false, 1003),
         MenuItem("Window/AudioClip Editor/Batch Process Clips", false, 1003)]
        private static void OpenBatchProcessingWindow()
        {
            foreach (var bulkWaveformEditorWindow in Resources.FindObjectsOfTypeAll<WaveformEditor>())
            {
                if (bulkWaveformEditorWindow != null)
                {
                    bulkWaveformEditorWindow.Focus();
                    return;
                }
            }
            AudioClip[] selectedAudioClips = Selection.GetFiltered<AudioClip>(SelectionMode.DeepAssets);
            // AudioClip[] clips = Selection.objects.OfType<AudioClip>().ToArray();

            BulkWaveformEditor window = GetWindow<BulkWaveformEditor>("Batch Process Clips");
            window.SetAudioClips(selectedAudioClips);
            window.Show();
        }
    
        public void SetAudioClips(AudioClip[] clips)
        {
            audioClips = new List<AudioClip>(clips);
        }
    
        private void OnGUI()
        {
            if (audioClips == null || audioClips.Count == 0)
            {
                EditorGUILayout.LabelField("No AudioClips selected.");
                return;
            }
            EditorGUILayout.LabelField($"Processing {audioClips.Count} clips", EditorStyles.boldLabel);
        

            if (GUILayout.Button("Normalize Clips"))
            {
                NormalizeClips();
            }
        
            if (GUILayout.Button("De-normalize Clips"))
            {
                DenormalizeClips();
            }
        
            volume = EditorGUILayout.Slider("Set Volume", volume, 0f, 2f);
            if (GUILayout.Button("Set Volume For Selected Clips"))
            {
                ApplyVolumeToClips();
            }
        
            silenceThreshold = EditorGUILayout.Slider("Silence Threshold", silenceThreshold, 0f, 0.2f);
            if (GUILayout.Button("Trim Start/End Silence"))
            {
                TrimStartEndSilence();
            }
        
            GUILayout.Space(10);
        
            if (GUILayout.Button("Reset Clips"))
            {
                ResetClips();
            }
        }

        private void DenormalizeClips()
        {
            if (audioClips.Count == 0) return;

            foreach (AudioClip clip in audioClips)
            {
                string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(clip);
                EditorPrefs.SetInt($"{key}_Normalize",0);
                AudioProcessingUtils.ApplyModificationsFromSettings(clip);
            }
        }

        private void ApplyVolumeToClips()
        {
            foreach (AudioClip clip in audioClips)
            {
                string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(clip);
                EditorPrefs.SetFloat($"{key}_Volume",volume);
                AudioProcessingUtils.ApplyModificationsFromSettings(clip);
            }
        }

        private void ResetClips()
        {
            if (audioClips.Count == 0) return;

            foreach (AudioClip clip in audioClips)
            {
                string uneditedClipPath = AudioProcessingUtils.GetUneditedClipPath(clip);
                if (!File.Exists(uneditedClipPath)) continue;
            
                byte[] wavData = File.ReadAllBytes(uneditedClipPath);
                string path = AssetDatabase.GetAssetPath(clip);
                File.WriteAllBytes(path, wavData); 
                AssetDatabase.ImportAsset(path);
            
                //File.Delete(uneditedClipPath);
                AssetDatabase.DeleteAsset(uneditedClipPath);
            
                var key = AudioProcessingUtils.GetEditorPrefKeyFromClip(clip);
                EditorPrefs.DeleteKey($"{key}_TrimStart");
                EditorPrefs.DeleteKey($"{key}_TrimEnd");
                EditorPrefs.DeleteKey($"{key}_FadeIn");
                EditorPrefs.DeleteKey($"{key}_FadeOut");
                EditorPrefs.DeleteKey($"{key}_Volume");
                EditorPrefs.DeleteKey($"{key}_Normalize");
                EditorPrefs.DeleteKey($"{key}_FadeInCurve");
                EditorPrefs.DeleteKey($"{key}_FadeOutCurve");
            }
        }

        private void TrimStartEndSilence()
        {
            if (audioClips.Count == 0) return;

            foreach (AudioClip clip in audioClips)
            {
                float trimStart = Trim(clip)[0];
                float trimEnd = Trim(clip)[1];
            
                string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(clip);
                EditorPrefs.SetFloat($"{key}_TrimStart",trimStart);
                EditorPrefs.SetFloat($"{key}_TrimEnd",trimEnd);
                AudioProcessingUtils.ApplyModificationsFromSettings(clip);
            }
        }

        private static float[] Trim(AudioClip clip)
        {
            AudioClip uneditedClip = AudioProcessingUtils.GetUneditedClip(clip) ?? clip;
            float[] samples = new float[uneditedClip.samples * uneditedClip.channels];
            uneditedClip.GetData(samples, 0);

            int totalSamples = samples.Length;
            int startSample = 0;
            int endSample = totalSamples - 1;
        
            while (startSample < totalSamples && Mathf.Abs(samples[startSample]) < silenceThreshold)
            {
                startSample++;
            }
        
            while (endSample > startSample && Mathf.Abs(samples[endSample]) < silenceThreshold)
            {
                endSample--;
            }

            int newLength = endSample - startSample + 1;
            if (newLength <= 0)
            {
                Debug.LogWarning($"Clip {clip.name} is completely silent after trimming.");
                return new float[]{0,1};
            }
            string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(clip);
            float trimStart = (float)startSample / totalSamples;
            float trimEnd = (float)endSample / totalSamples;
            EditorPrefs.SetFloat($"{key}_TrimStart",trimStart);
            EditorPrefs.SetFloat($"{key}_TrimEnd",trimEnd);

            return new []{ trimStart, trimEnd};
        }
    
        private void NormalizeClips()
        {
            if (audioClips.Count == 0) return;

            foreach (AudioClip clip in audioClips)
            {
                string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(clip);
                EditorPrefs.SetInt($"{key}_Normalize",1);

                AudioProcessingUtils.ApplyModificationsFromSettings(clip);
            }
        }
    }
}
