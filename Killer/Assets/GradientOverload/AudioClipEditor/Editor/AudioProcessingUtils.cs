using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AudioClipEditor
{
    public static class AudioProcessingUtils
    {
        private static string UneditedClipsFolderPath;

        public static void ApplyModificationsFromSettings(AudioClip clip)
        {
            SaveUneditedClip(clip);
            AudioClip baseClip = clip;
            string uneditedClipPath = GetUneditedClipPath(clip);
            if (File.Exists(uneditedClipPath))
            {
                baseClip = WavUtility.ToAudioClip(File.ReadAllBytes(uneditedClipPath), clip.name);
            }

            int sampleCount = baseClip.samples;
            int channelCount = baseClip.channels;
            float[] originalSamples = new float[sampleCount * channelCount];
            baseClip.GetData(originalSamples, 0);
            int sampleRate = baseClip.frequency;

            string key = GetEditorPrefKeyFromClip(clip);
            float trimStart = EditorPrefs.GetFloat($"{key}_TrimStart", 0);
            float trimEnd = EditorPrefs.GetFloat($"{key}_TrimEnd", 1);
            float fadeInDuration = EditorPrefs.GetFloat($"{key}_FadeIn", 0);
            float fadeOutDuration = EditorPrefs.GetFloat($"{key}_FadeOut", 0);

            float volume = EditorPrefs.GetFloat($"{key}_Volume", 1);
            bool normalize = EditorPrefs.GetInt($"{key}_Normalize", 0) == 1;
            AnimationCurve fadeInCurve = LoadCurve($"{key}_FadeInCurve", AnimationCurve.Linear(0, 0, 1, 1));
            AnimationCurve fadeOutCurve = LoadCurve($"{key}_FadeOutCurve", AnimationCurve.Linear(0, 0, 1, 1));

            float[] modifiedSamples = ApplyTrim(originalSamples, trimStart, trimEnd);
            ApplyFade(modifiedSamples, modifiedSamples.Length, sampleRate, fadeInDuration, fadeOutDuration, fadeInCurve, fadeOutCurve);
            if (normalize) Normalize(modifiedSamples);
            AdjustVolume(modifiedSamples, volume);

            AudioClip modifiedClip = AudioClip.Create(clip.name, modifiedSamples.Length / channelCount, clip.channels, sampleRate, false);
            modifiedClip.SetData(modifiedSamples, 0);
            byte[] wavData = WavUtility.FromAudioClip(modifiedClip);
            
            WriteAllBytesSafe(AssetDatabase.GetAssetPath(clip), wavData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
        }

        public static void WriteAllBytesSafe(string path, byte[] data)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                    }
                }

                File.WriteAllBytes(path, data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioClipEditor] Failed to write to {path}: {e.Message}");
            }
        }

        public static void SaveCurve(AnimationCurve curve, string key)
        {
            CurveData curveData = new CurveData
            {
                keyframes = curve.keys.Select(k => new KeyframeData(k)).ToArray()
            };

            string json = JsonUtility.ToJson(curveData);
            EditorPrefs.SetString(key, json);
        }

        public static AnimationCurve LoadCurve(string key, AnimationCurve defaultCurve)
        {
            if (!EditorPrefs.HasKey(key)) return defaultCurve;

            string json = EditorPrefs.GetString(key);
            CurveData curveData = JsonUtility.FromJson<CurveData>(json);

            AnimationCurve curve = new AnimationCurve(curveData.keyframes.Select(k => k.ToKeyframe()).ToArray());
            return curve;
        }

        public static float[] ApplyTrim(float[] samples, float trimStart, float trimEnd)
        {
            int startSample = Mathf.FloorToInt(trimStart * samples.Length);
            int endSample = Mathf.FloorToInt(trimEnd * samples.Length);

            int newLength = endSample - startSample;
            float[] modifiedSamples = new float[newLength];
            Array.Copy(samples, startSample, modifiedSamples, 0, newLength);
            return modifiedSamples;
        }

        public static void Normalize(float[] samples)
        {
            float max = samples.Max(Mathf.Abs);
            if (max > 0)
            {
                float multiplier = 1f / max;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= multiplier;
                }
            }
        }

        public static void AdjustVolume(float[] samples, float volume)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= volume;
                samples[i] = Mathf.Clamp(samples[i], -1f, 1f); // Prevent overflow
            }
        }

        public static void ApplyFade(float[] samples, int length, int sampleRate, float fadeInDuration, float fadeOutDuration, AnimationCurve fadeInCurve, AnimationCurve fadeOutCurve, int channels = 1)
        {
            // Calculate the trimmed clip length in seconds (considering channels)
            float clipLengthInSeconds = (float)length / (sampleRate * channels);
            
            // Convert durations to normalized values (0-1) relative to the length of the processed clip
            float normalizedFadeIn = fadeInDuration / clipLengthInSeconds;
            float normalizedFadeOut = fadeOutDuration / clipLengthInSeconds;
            
            // Calculate sample counts based on the normalized values (in sample frames, considering all channels)
            int fadeInSampleFrames = Mathf.CeilToInt(normalizedFadeIn * (length / channels));
            int fadeOutSampleFrames = Mathf.CeilToInt(normalizedFadeOut * (length / channels));
            
            // Ensure we don't go out of bounds
            fadeInSampleFrames = Mathf.Min(fadeInSampleFrames, length / channels);
            fadeOutSampleFrames = Mathf.Min(fadeOutSampleFrames, length / channels);
            
            // Apply fade-in effect (multiply each sample in all channels)
            for (int frame = 0; frame < fadeInSampleFrames; frame++)
            {
                float t = (float)frame / fadeInSampleFrames;
                float multiplier = fadeInCurve.Evaluate(t);
                
                // Apply to all channels at this frame
                for (int channel = 0; channel < channels; channel++)
                {
                    int sampleIndex = frame * channels + channel;
                    if (sampleIndex < length)
                    {
                        samples[sampleIndex] *= multiplier;
                    }
                }
            }
            
            // Apply fade-out effect (multiply each sample in all channels)
            for (int frame = 0; frame < fadeOutSampleFrames; frame++)
            {
                float t = (float)frame / fadeOutSampleFrames;
                float multiplier = fadeOutCurve.Evaluate(t);
                
                // Apply to all channels at this frame, starting from the end
                for (int channel = 0; channel < channels; channel++)
                {
                    int sampleIndex = (length / channels - frame - 1) * channels + channel;
                    if (sampleIndex >= 0 && sampleIndex < length)
                    {
                        samples[sampleIndex] *= multiplier;
                    }
                }
            }
        }

        public static void SaveUneditedClip(AudioClip clip)
        {
            string uneditedClipPath = GetUneditedClipPath(clip);
            if (File.Exists(uneditedClipPath)) return;

            byte[] wavData = WavUtility.FromAudioClip(clip);
            WriteAllBytesSafe(uneditedClipPath, wavData);
            AssetDatabase.ImportAsset(uneditedClipPath);
        }

        public static string GetUneditedClipPath(AudioClip clip)
        {
            string assetPath = AssetDatabase.GetAssetPath(clip);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            string[] guids = AssetDatabase.FindAssets("t:Script AudioClipEditorAnchor");
            if (guids.Length == 0)
            {
                Debug.LogError("No AudioClipEditorAnchor script found in the project! It is required to determine the folder for unedited clips.");
                return null;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]); // Get full script path
            string assetFolderPath = Path.GetDirectoryName(scriptPath); // Get its folder

            UneditedClipsFolderPath = Path.Combine(assetFolderPath, "UneditedAudioClips");
            
            if (!AssetDatabase.IsValidFolder(UneditedClipsFolderPath))
            {
                AssetDatabase.CreateFolder(assetFolderPath, "UneditedAudioClips");
            }

            string uneditedClipName = Path.Combine(UneditedClipsFolderPath, $"{guid}.wav");
            return uneditedClipName;
        }

        public static string GetEditorPrefKeyFromClip(AudioClip clip)
        {
            string assetPath = AssetDatabase.GetAssetPath(clip);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            string key = $"WaveformEditor_{guid}";
            return key;
        }

        public static AudioClip GetUneditedClip(AudioClip clip)
        {
            string uneditedClipPath = GetUneditedClipPath(clip);
            if (!File.Exists(uneditedClipPath)) return null;

            byte[] wavData = File.ReadAllBytes(uneditedClipPath);
            AudioClip uneditedClip = WavUtility.ToAudioClip(wavData, clip.name);
            return uneditedClip;
        }
    }

    [Serializable]
    public class CurveData
    {
        public KeyframeData[] keyframes;
    }

    [Serializable]
    public class KeyframeData
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;

        public KeyframeData(Keyframe key)
        {
            time = key.time;
            value = key.value;
            inTangent = key.inTangent;
            outTangent = key.outTangent;
        }

        public Keyframe ToKeyframe()
        {
            return new Keyframe(time, value, inTangent, outTangent);
        }
    }
}
