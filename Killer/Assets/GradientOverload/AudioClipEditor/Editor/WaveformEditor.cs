using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AudioClipEditor
{
    public class WaveformEditor : EditorWindow
    {
        private AudioClip audioClip;
        private AudioClip selectedAudioClip;
        private float uneditedAudioClipDuration;
        private float[] samples;
        private float[] modifiedSamples;
        private float trimStart = 0;
        private float trimEnd = 1;
        private float fadeInDuration = 0;
        private float fadeOutDuration = 0;
        private AnimationCurve fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private AnimationCurve fadeOutCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private bool normalize = false;
        private float volume = 1f;
        private float playbackSpeed = 1f;
        private int sampleRate;
        private AudioSource audioSource;
        private bool isPlaying = false;
        private float playheadPosition = 0;
        private string uneditedClipPath;
        private bool madeChanges = false;
        private bool wasSelectionChanged;
    
        private double lastChangeTime = 0;
        private bool subscribedToCheckAndApplyChanges;
    
        private Material lineMaterial;
        
        [MenuItem("Assets/AudioClip Editor/Waveform Editor", false, 1000),
         MenuItem("Window/AudioClip Editor/Waveform Editor", false, 1000)]
        private static void ShowWindow()
        {
            WaveformEditor window = GetWindow<WaveformEditor>("Waveform Editor");
            if (Selection.activeObject is AudioClip clip)
            {
                window.SetAudioClip(clip);
            }
        }

        private void OnEnable()
        {
            EditorApplication.update += Update;
            Selection.selectionChanged += OnSelectionChanged;
            if (!audioSource)
            {
                GameObject go = new GameObject("WaveformEditorAudioSource");
                audioSource = go.AddComponent<AudioSource>();
                go.hideFlags = HideFlags.HideAndDontSave;
            }
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is AudioClip clip)
            {
                selectedAudioClip = clip;
                LoadAudioClipData(clip);
                ApplyRealTimeAdjustments();
                PlayModifiedClip();
                wasSelectionChanged = true;
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
            Selection.selectionChanged -= OnSelectionChanged;
            if (audioSource)
            {
                DestroyImmediate(audioSource.gameObject);
            }
            audioSource = null;
            selectedAudioClip = null;
            ClearWaveformData();
            GC.Collect();
        }
        private void ClearWaveformData()
        {
            samples = null;
            modifiedSamples = null;
            if (lineMaterial != null)
            {
                DestroyImmediate(lineMaterial);
            }
        }

        private void Update()
        {
            if (isPlaying && audioSource.clip != null)
            {
                float newPlayheadPosition = audioSource.time / audioSource.clip.length;
                if (Mathf.Abs(newPlayheadPosition - playheadPosition) > 0.001f)
                {
                    playheadPosition = newPlayheadPosition;
                    Repaint();
                }
            
                if (!audioSource.isPlaying || Mathf.Abs(audioSource.time - audioSource.clip.length) < 0.01f)
                {
                    StopClip(); 
                }
            }
        }
        
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            selectedAudioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", selectedAudioClip, typeof(AudioClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                wasSelectionChanged = true;
            }

            if (selectedAudioClip != null)
            {
                if (samples == null || sampleRate != audioClip.frequency || wasSelectionChanged)
                {
                    LoadAudioClipData(selectedAudioClip);
                    ApplyRealTimeAdjustments();
                    PlayModifiedClip();
                }

                if(madeChanges)
                    ApplyRealTimeAdjustments();
            
                bool applyChanges = false;
                if (Event.current != null)
                {
                    if (Event.current.rawType == EventType.MouseUp && madeChanges)
                    {
                        applyChanges = true;
                        madeChanges = false;
                    }
                }
            
                DrawWaveform();
                DrawTrimControls();
                DrawFadeControls();
                DrawVolumeControls();

                
                if (GUILayout.Button("Reset"))
                {
                    ResetClip();
                }
                
                EditorGUILayout.Space();
                DrawPlaybackControls();
                
                if (applyChanges)
                {
                    ApplyChanges();
                    PlayModifiedClip();
                }
                
                if (wasSelectionChanged)
                {
                    wasSelectionChanged = false;
                    Repaint();
                }
            }
        }

        public void SetAudioClip(AudioClip clip)
        {
            selectedAudioClip = clip;
            LoadAudioClipData(clip);
            LoadSettings();
        }
    
        private void LoadAudioClipData(AudioClip selectedAudioClip)
        {
            if (audioClip != null && audioClip != selectedAudioClip)
            {
                audioClip = null;
            }
        
            uneditedClipPath = AudioProcessingUtils.GetUneditedClipPath(selectedAudioClip);
            if (File.Exists(uneditedClipPath))
            {
                // Load the unedited clip from the file
                audioClip = WavUtility.ToAudioClip(File.ReadAllBytes(uneditedClipPath), selectedAudioClip.name);
                // get the duration of the unedited clip
                uneditedAudioClipDuration = audioClip.length;
                LoadSettings();
            }
            else
            {
                audioClip = selectedAudioClip;
                uneditedAudioClipDuration = audioClip.length;
                string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(selectedAudioClip);
                // Delete saved settings data in case the user deleted the unedited clip manually
                EditorPrefs.DeleteKey($"{key}_TrimStart");
                EditorPrefs.DeleteKey($"{key}_TrimEnd");
                EditorPrefs.DeleteKey($"{key}_FadeIn");
                EditorPrefs.DeleteKey($"{key}_FadeOut");
                EditorPrefs.DeleteKey($"{key}_Volume");
                EditorPrefs.DeleteKey($"{key}_Normalize");
                EditorPrefs.DeleteKey($"{key}_FadeInCurve");
                EditorPrefs.DeleteKey($"{key}_FadeOutCurve");
                EditorPrefs.DeleteKey($"{key}_Speed");
                LoadSettings();
            }

            sampleRate = audioClip.frequency;
            int sampleCount = audioClip.samples * audioClip.channels;
            samples = new float[sampleCount];
            audioClip.GetData(samples, 0);

            modifiedSamples = new float[sampleCount];
            Array.Copy(samples, modifiedSamples, sampleCount);
        }

        private void SaveSettings()
        {
            if (audioClip == null) return;

            string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(selectedAudioClip);

            // Save trim values directly as 0-1 range
            EditorPrefs.SetFloat($"{key}_TrimStart", trimStart);
            EditorPrefs.SetFloat($"{key}_TrimEnd", trimEnd);
            
            // Calculate the trimmed duration
            float trimmedDuration = (trimEnd - trimStart) * uneditedAudioClipDuration;
            
            // Save fade durations as seconds
            EditorPrefs.SetFloat($"{key}_FadeIn", fadeInDuration);
            EditorPrefs.SetFloat($"{key}_FadeOut", fadeOutDuration);
            
            EditorPrefs.SetFloat($"{key}_Volume", volume);
            EditorPrefs.SetInt($"{key}_Normalize", normalize ? 1 : 0);
            EditorPrefs.SetFloat($"{key}_Speed", playbackSpeed);

            AudioProcessingUtils.SaveCurve(fadeInCurve, $"{key}_FadeInCurve");
            AudioProcessingUtils.SaveCurve(fadeOutCurve, $"{key}_FadeOutCurve");
        }

        private void LoadSettings()
        { 
            if (audioClip == null) return;

            string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(selectedAudioClip);

            trimStart = EditorPrefs.GetFloat($"{key}_TrimStart", 0);
            trimEnd = EditorPrefs.GetFloat($"{key}_TrimEnd", 1);
            
            // Load fade durations in seconds
            fadeInDuration = EditorPrefs.GetFloat($"{key}_FadeIn", 0);
            fadeOutDuration = EditorPrefs.GetFloat($"{key}_FadeOut", 0);
            
            // Calculate the trimmed duration
            float trimmedDuration = (trimEnd - trimStart) * uneditedAudioClipDuration;
            
            // Clamp fade durations to the trimmed length to ensure they're valid
            fadeInDuration = Mathf.Min(fadeInDuration, trimmedDuration);
            fadeOutDuration = Mathf.Min(fadeOutDuration, trimmedDuration);
            
            volume = EditorPrefs.GetFloat($"{key}_Volume", 1);
            normalize = EditorPrefs.GetInt($"{key}_Normalize", 0) == 1;
            playbackSpeed = EditorPrefs.GetFloat($"{key}_Speed", 1);
            
            string fadeInCurveJson = EditorPrefs.GetString($"{key}_FadeInCurve", "");
            if (!string.IsNullOrEmpty(fadeInCurveJson) && fadeInCurveJson != "{}")
            {
                fadeInCurve = AudioProcessingUtils.LoadCurve($"{key}_FadeInCurve", AnimationCurve.Linear(0, 0, 1, 1));
            }
            else
            {
                fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            }

            string fadeOutCurveJson = EditorPrefs.GetString($"{key}_FadeOutCurve", "");
            if (!string.IsNullOrEmpty(fadeOutCurveJson) && fadeInCurveJson != "{}")
            {
                fadeOutCurve = AudioProcessingUtils.LoadCurve($"{key}_FadeOutCurve", AnimationCurve.Linear(0, 0, 1, 1));
            }
            else
            {
                fadeOutCurve = AnimationCurve.Linear(0, 0, 1, 1);
            }
        }


        private void ApplyRealTimeAdjustments()
        {
            if (samples == null) return;

            // Apply trim
            modifiedSamples = AudioProcessingUtils.ApplyTrim(samples, trimStart, trimEnd);
            
            // Calculate the actual trimmed clip length in seconds
            float trimmedDuration = (trimEnd - trimStart) * uneditedAudioClipDuration;
            
            // Get the length of the actual samples in samples (after trimming)
            int sampleLength = modifiedSamples.Length / audioClip.channels;
            float actualClipDuration = (float)sampleLength / sampleRate;
            
            // Make sure fadeInDuration and fadeOutDuration don't exceed the actual clip length
            // This handles edge cases where the audio clip length doesn't match exactly what we compute
            float actualFadeInDuration = Mathf.Min(fadeInDuration, actualClipDuration);
            float actualFadeOutDuration = Mathf.Min(fadeOutDuration, actualClipDuration);
            
            // Apply fades with channel count
            AudioProcessingUtils.ApplyFade(
                modifiedSamples, 
                modifiedSamples.Length, 
                sampleRate, 
                actualFadeInDuration, 
                actualFadeOutDuration, 
                fadeInCurve, 
                fadeOutCurve,
                audioClip.channels
            );
            
            // Apply other effects
            if (normalize) AudioProcessingUtils.Normalize(modifiedSamples);
            AudioProcessingUtils.AdjustVolume(modifiedSamples, volume);
            if (!Mathf.Approximately(playbackSpeed, 1f))
            {
                modifiedSamples = AudioProcessingUtils.ApplySpeed(modifiedSamples, playbackSpeed, audioClip.channels);
            }
        }
        
        void CreateLineMaterial()
        {
            if (!lineMaterial)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                lineMaterial.SetInt("_ZWrite", 0);
                lineMaterial.SetFloat("_AntiAliasing", 1.0f); // Smooth lines
            }
        }
        
        private void DrawWaveform()
        {
            if (modifiedSamples == null || modifiedSamples.Length == 0) return;

            EditorGUILayout.LabelField("Waveform Preview", EditorStyles.boldLabel);

            float width = position.width - 20;
            float height = 100;
            Rect rect = GUILayoutUtility.GetRect(width, height);

            if (Event.current.type == EventType.Repaint)
            {
                Color originalColor = Handles.color;
                Handles.color = new Color(0f, 0f, 0f, 0.4f); // Light gray with transparency
                Handles.DrawSolidRectangleWithOutline(new Rect(rect.x, rect.y, rect.width, rect.height), Handles.color, Color.clear);
                Handles.color = originalColor;

                CreateLineMaterial();
                lineMaterial.SetPass(0);
                GL.PushMatrix();
                GL.LoadPixelMatrix();
                GL.Begin(GL.LINES);
                GL.Color(Color.cyan);
                
                // Higher resolution sampling
                int step = Mathf.Max(1, modifiedSamples.Length / (int)(width * 4)); 
                float centerY = rect.y + rect.height / 2;
                float amplitude = rect.height / 2;
            
                for (int i = 0; i < modifiedSamples.Length - step; i += step)
                {
                    float sample = modifiedSamples[i];
                    float nextSample = modifiedSamples[i + step];
                
                    // Ensure samples stay within [-1, 1] before visual scaling
                    sample = Mathf.Clamp(sample, -1f, 1f);
                    nextSample = Mathf.Clamp(nextSample, -1f, 1f);
            
                    // Amplify low volume sections using logarithmic scaling
                    sample = Mathf.Sign(sample) * Mathf.Log10(Mathf.Abs(sample) * 9 + 1);
                    nextSample = Mathf.Sign(nextSample) * Mathf.Log10(Mathf.Abs(nextSample) * 9 + 1);
            
                    float x1 = rect.x + ((float)i / modifiedSamples.Length) * rect.width;
                    float y1 = centerY - sample * amplitude;
                    float x2 = rect.x + ((float)(i + step) / modifiedSamples.Length) * rect.width;
                    float y2 = centerY - nextSample * amplitude;
            
                    //Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2)); less performant
                    GL.Vertex3(x1, y1, 0);
                    GL.Vertex3(x2, y2, 0);
                }
                GL.End();
                GL.PopMatrix();

                // Draw fade regions
                if (fadeInDuration > 0)
                {
                    // Calculate fade width considering channels
                    float actualClipDuration = (float)(modifiedSamples.Length / audioClip.channels) / sampleRate;
                    float fadeInRatio = fadeInDuration / actualClipDuration;
                    float fadeInWidth = fadeInRatio * rect.width;
                    
                    // Cap the visualization to the actual waveform width
                    fadeInWidth = Mathf.Min(fadeInWidth, rect.width);
                    
                    // Draw fade in overlay
                    Handles.color = new Color(0f, 1f, 0f, 0.2f); // Light green for fade in
                    Handles.DrawSolidRectangleWithOutline(
                        new Rect(rect.x, rect.y, fadeInWidth, rect.height), 
                        Handles.color, Color.clear);
                    
                    // Draw the fade in curve on top of the region
                    if (fadeInWidth > 5) // Only draw if there's enough space
                    {
                        Handles.color = new Color(0f, 0.8f, 0f, 0.9f); // Darker green for the curve
                        int curveSegments = Mathf.Max(10, Mathf.Min(50, (int)fadeInWidth / 2));
                        
                        Vector3 prevPoint = Vector3.zero;
                        for (int i = 0; i <= curveSegments; i++)
                        {
                            float t = (float)i / curveSegments;
                            float curveValue = fadeInCurve.Evaluate(t);
                            
                            // Map curve value (0-1) to the rect height, leaving some margin at top and bottom
                            float x = rect.x + t * fadeInWidth;
                            float margin = rect.height * 0.1f;
                            float y = rect.y + margin + (rect.height - 2 * margin) * (1 - curveValue);
                            
                            Vector3 currentPoint = new Vector3(x, y, 0);
                            if (i > 0)
                            {
                                Handles.DrawAAPolyLine(3f, prevPoint, currentPoint); // Thicker anti-aliased line
                            }
                            prevPoint = currentPoint;
                        }
                    }
                }
                
                if (fadeOutDuration > 0)
                {
                    // Calculate fade width considering channels
                    float actualClipDuration = (float)(modifiedSamples.Length / audioClip.channels) / sampleRate;
                    float fadeOutRatio = fadeOutDuration / actualClipDuration;
                    float fadeOutWidth = fadeOutRatio * rect.width;
                    
                    // Cap the visualization to the actual waveform width
                    fadeOutWidth = Mathf.Min(fadeOutWidth, rect.width);
                    
                    // Draw fade out overlay
                    Handles.color = new Color(1f, 0f, 0f, 0.2f); // Light red for fade out
                    Rect fadeOutRect = new Rect(rect.x + rect.width - fadeOutWidth, rect.y, fadeOutWidth, rect.height);
                    Handles.DrawSolidRectangleWithOutline(fadeOutRect, Handles.color, Color.clear);
                    
                    // Draw the fade out curve on top of the region
                    if (fadeOutWidth > 5) // Only draw if there's enough space
                    {
                        Handles.color = new Color(0.8f, 0f, 0f, 0.9f); // Darker red for the curve
                        int curveSegments = Mathf.Max(10, Mathf.Min(50, (int)fadeOutWidth / 2));
                        
                        Vector3 prevPoint = Vector3.zero;
                        float startX = rect.x + rect.width - fadeOutWidth;
                        
                        for (int i = 0; i <= curveSegments; i++)
                        {
                            float t = (float)i / curveSegments;
                            float curveValue = fadeOutCurve.Evaluate(1 - t); // Reverse the curve for fade out
                            
                            // Map curve value (0-1) to the rect height, leaving some margin at top and bottom
                            float x = startX + t * fadeOutWidth;
                            float margin = rect.height * 0.1f;
                            float y = rect.y + margin + (rect.height - 2 * margin) * (1 - curveValue);
                            
                            Vector3 currentPoint = new Vector3(x, y, 0);
                            if (i > 0)
                            {
                                Handles.DrawAAPolyLine(3f, prevPoint, currentPoint); // Thicker anti-aliased line
                            }
                            prevPoint = currentPoint;
                        }
                    }
                }

                // Draw playthrough line if playing
                if (isPlaying)
                {
                    Handles.color = Color.red;
                    float playheadX = rect.x + playheadPosition * rect.width;
                    Handles.DrawLine(new Vector3(playheadX, rect.y), new Vector3(playheadX, rect.y + rect.height));
                }
            }
        }

        private void DrawTrimControls()
        {
            EditorGUILayout.LabelField("Trim", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float oldTrimStart = trimStart;
            float oldTrimEnd = trimEnd;
        
            float currentTrimStartDuration = trimStart * uneditedAudioClipDuration;
            float currentTrimEndDuration = trimEnd * uneditedAudioClipDuration;

            float newTrimStartDuration = EditorGUILayout.Slider("Start", currentTrimStartDuration, 0f, uneditedAudioClipDuration);
            float newTrimEndDuration = EditorGUILayout.Slider("End", currentTrimEndDuration, 0f, uneditedAudioClipDuration);

            float newTrimStart = newTrimStartDuration / uneditedAudioClipDuration;
            float newTrimEnd = newTrimEndDuration / uneditedAudioClipDuration;


            if (EditorGUI.EndChangeCheck())
            {
                trimStart = newTrimStart;
                trimEnd = newTrimEnd;
            }

            if(!Mathf.Approximately(oldTrimStart, newTrimStart) || !Mathf.Approximately(oldTrimEnd, newTrimEnd))
            {
                madeChanges = true;
            }
        }

        private void DrawFadeControls()
        {
            float oldFadeInDuration = fadeInDuration;
            float oldFadeOutDuration = fadeOutDuration;

            // Calculate the actual duration after trimming
            float trimmedDuration = (trimEnd - trimStart) * uneditedAudioClipDuration;
            
            // Update the sliders to use the trimmed duration as maximum
            fadeInDuration = EditorGUILayout.Slider("Fade In Duration", fadeInDuration, 0, trimmedDuration);
            fadeOutDuration = EditorGUILayout.Slider("Fade Out Duration", fadeOutDuration, 0, trimmedDuration);
            
            // Add a note about the maximum fade duration
            if (fadeInDuration + fadeOutDuration > trimmedDuration)
            {
                EditorGUILayout.HelpBox("Warning: Total fade duration exceeds audio length. Fades will overlap.", MessageType.Warning);
            }
            
            EditorGUI.BeginChangeCheck();

            AnimationCurve newFadeInCurve = EditorGUILayout.CurveField("Fade In Curve", fadeInCurve);
            AnimationCurve newFadeOutCurve = EditorGUILayout.CurveField("Fade Out Curve", fadeOutCurve);

            if (EditorGUI.EndChangeCheck())
            {
                fadeInCurve = newFadeInCurve;
                fadeOutCurve = newFadeOutCurve;

                lastChangeTime = EditorApplication.timeSinceStartup;
                if(subscribedToCheckAndApplyChanges)
                {
                    EditorApplication.update -= CheckAndApplyChanges;
                }
                EditorApplication.update += CheckAndApplyChanges;
                subscribedToCheckAndApplyChanges = true;
                
                madeChanges = true;
            }

            if(!Mathf.Approximately(oldFadeInDuration, fadeInDuration) || !Mathf.Approximately(oldFadeOutDuration, fadeOutDuration))
            {
                madeChanges = true;
            }
        }
    
        private void CheckAndApplyChanges()
        {
            if (EditorApplication.timeSinceStartup - lastChangeTime >= 0.5f)
            {
                ApplyChanges();
                PlayModifiedClip();
                Repaint();
            
                EditorApplication.update -= CheckAndApplyChanges;
                subscribedToCheckAndApplyChanges = false;
            }
        }

        private void DrawVolumeControls()
        {
            bool newNormalize = EditorGUILayout.Toggle("Normalize", normalize);
            float newVolume = EditorGUILayout.Slider("Volume", volume, 0f, 2f);
            float newPlaybackSpeed = EditorGUILayout.Slider("Playback Speed", playbackSpeed, 0.25f, 2f);
        
            if (!Mathf.Approximately(newVolume, volume))
            {
                volume = newVolume;
                madeChanges = true;
            }
        
            if (!Mathf.Approximately(newPlaybackSpeed, playbackSpeed))
            {
                playbackSpeed = newPlaybackSpeed;
                madeChanges = true;
            }
        
            if (newNormalize != normalize)
            {
                normalize = newNormalize;
                ApplyChanges();
                PlayModifiedClip();
            }
        }
    
        private void ApplyChanges()
        {
            if (audioClip == null) return;

            AudioProcessingUtils.SaveUneditedClip(selectedAudioClip);

            ApplyRealTimeAdjustments();
        
            // Create new AudioClip
            AudioClip newClip = AudioClip.Create(selectedAudioClip.name, modifiedSamples.Length / audioClip.channels, audioClip.channels, audioClip.frequency, false);
            newClip.SetData(modifiedSamples, 0);

            // get the path of the AudioClip
            string path = AssetDatabase.GetAssetPath(selectedAudioClip);
            byte[] wavData = WavUtility.FromAudioClip(newClip);
            
            AudioProcessingUtils.WriteAllBytesSafe(path, wavData);
            AssetDatabase.ImportAsset(path);
            SaveSettings();
        }
        

        private void ResetClip()
        {
            StopClip();
            uneditedClipPath = AudioProcessingUtils.GetUneditedClipPath(selectedAudioClip);
            if (string.IsNullOrEmpty(uneditedClipPath) || !File.Exists(uneditedClipPath))
                return;
        
            byte[] wavData = File.ReadAllBytes(uneditedClipPath);
            string path = AssetDatabase.GetAssetPath(selectedAudioClip);
            
            AudioProcessingUtils.WriteAllBytesSafe(path, wavData);
            AssetDatabase.ImportAsset(path);
        
            AssetDatabase.DeleteAsset(uneditedClipPath);
            uneditedClipPath = null;

            audioClip = selectedAudioClip;
        
            trimStart = 0;
            trimEnd = 1;
            fadeInDuration = 0;
            fadeOutDuration = 0;
            fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            fadeOutCurve = AnimationCurve.Linear(0, 0, 1, 1);
            normalize = false;
            volume = 1f;
            playbackSpeed = 1f;

            string key = AudioProcessingUtils.GetEditorPrefKeyFromClip(selectedAudioClip);
        
            EditorPrefs.DeleteKey($"{key}_TrimStart");
            EditorPrefs.DeleteKey($"{key}_TrimEnd");
            EditorPrefs.DeleteKey($"{key}_FadeIn");
            EditorPrefs.DeleteKey($"{key}_FadeOut");
            EditorPrefs.DeleteKey($"{key}_Volume");
            EditorPrefs.DeleteKey($"{key}_Normalize");
            EditorPrefs.DeleteKey($"{key}_FadeInCurve");
            EditorPrefs.DeleteKey($"{key}_FadeOutCurve");
            EditorPrefs.DeleteKey($"{key}_Speed");
        
            modifiedSamples = new float[wavData.Length];
            Array.Copy(wavData, modifiedSamples, wavData.Length);
            ApplyRealTimeAdjustments();
            Repaint();
        }
    
        private void DrawPlaybackControls()
        {
            if (EditorUtility.audioMasterMute)
            {
                EditorGUILayout.HelpBox("Audio is muted in Game View. Unmute to hear playback!", MessageType.Warning);
            }
            EditorGUILayout.BeginHorizontal();
            if (isPlaying)
            {
                if (GUILayout.Button("⏹️ Stop"))
                {
                    StopClip();
                }
            }
            else
            {
                if (GUILayout.Button("▶️ Play"))
                {
                    PlayModifiedClip();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PlayModifiedClip()
        {
            AudioClip clip = AudioClip.Create("ModifiedClip", modifiedSamples.Length/audioClip.channels, audioClip.channels, sampleRate, false);
            clip.SetData(modifiedSamples, 0);
            audioSource.clip = clip;
            audioSource.Play();
            isPlaying = true;
        }

        private void StopClip()
        {
            audioSource.Stop();
            isPlaying = false;
            playheadPosition = 0;
        }
    }
}
