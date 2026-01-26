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
            float playbackSpeed = EditorPrefs.GetFloat($"{key}_Speed", 1);
            bool preservePitch = EditorPrefs.GetInt($"{key}_PreservePitch", 0) == 1;
            AnimationCurve fadeInCurve = LoadCurve($"{key}_FadeInCurve", AnimationCurve.Linear(0, 0, 1, 1));
            AnimationCurve fadeOutCurve = LoadCurve($"{key}_FadeOutCurve", AnimationCurve.Linear(0, 0, 1, 1));

            float[] modifiedSamples = ApplyTrim(originalSamples, trimStart, trimEnd);
            ApplyFade(modifiedSamples, modifiedSamples.Length, sampleRate, fadeInDuration, fadeOutDuration, fadeInCurve, fadeOutCurve);
            if (normalize) Normalize(modifiedSamples);
            AdjustVolume(modifiedSamples, volume);
            if (!Mathf.Approximately(playbackSpeed, 1f))
            {
                if (preservePitch)
                {
                    modifiedSamples = ApplySpeedPreservePitch(modifiedSamples, playbackSpeed, channelCount, sampleRate);
                }
                else
                {
                    modifiedSamples = ApplySpeed(modifiedSamples, playbackSpeed, channelCount);
                }
            }

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

        public static float[] ApplySpeed(float[] samples, float speed, int channels)
        {
            if (speed <= 0f) return samples;
            if (channels <= 0) channels = 1;

            int originalFrames = samples.Length / channels;
            if (originalFrames <= 0) return samples;

            int newFrames = Mathf.Max(1, Mathf.FloorToInt(originalFrames / speed));
            float[] result = new float[newFrames * channels];

            for (int frame = 0; frame < newFrames; frame++)
            {
                float srcFrame = frame * speed;
                int srcFrameIndex = Mathf.Min((int)srcFrame, originalFrames - 1);
                int srcBaseIndex = srcFrameIndex * channels;
                int dstBaseIndex = frame * channels;

                for (int c = 0; c < channels; c++)
                {
                    int srcIndex = srcBaseIndex + c;
                    int dstIndex = dstBaseIndex + c;
                    if (srcIndex < samples.Length && dstIndex < result.Length)
                    {
                        result[dstIndex] = samples[srcIndex];
                    }
                }
            }

            return result;
        }

        public static float[] ApplySpeedPreservePitch(float[] samples, float speed, int channels, int sampleRate)
        {
            if (speed <= 0f) return samples;
            if (channels <= 0) channels = 1;

            int originalFrames = samples.Length / channels;
            if (originalFrames <= 0) return samples;

            float alpha = 1f / speed;
            int grain = Mathf.Min(2048, originalFrames);
            if (grain < 2) return samples;

            int analysisHop = Mathf.Max(1, grain / 2);
            int estimatedFrames = Mathf.Max(grain, (int)(originalFrames * alpha) + grain);

            float[] outputInterleaved = new float[estimatedFrames * channels];
            int maxOutFrames = 0;

            for (int ch = 0; ch < channels; ch++)
            {
                float[] outChannel = new float[estimatedFrames];
                float[] weight = new float[estimatedFrames];

                int inPos = 0;
                int outPos = 0;

                while (inPos + grain < originalFrames && outPos + grain < estimatedFrames)
                {
                    for (int i = 0; i < grain; i++)
                    {
                        float w = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (grain - 1));
                        int inIndex = (inPos + i) * channels + ch;
                        int outIndex = outPos + i;
                        if (inIndex < samples.Length && outIndex < outChannel.Length)
                        {
                            outChannel[outIndex] += samples[inIndex] * w;
                            weight[outIndex] += w;
                        }
                    }

                    inPos += analysisHop;
                    int synthesisHop = Mathf.Max(1, (int)(analysisHop * alpha));
                    outPos += synthesisHop;
                }

                int outFrames = 0;
                for (int i = 0; i < estimatedFrames; i++)
                {
                    if (weight[i] > 0f)
                    {
                        outChannel[i] /= weight[i];
                        outChannel[i] = Mathf.Clamp(outChannel[i], -1f, 1f);
                        outFrames = i + 1;
                    }
                }

                if (outFrames > maxOutFrames)
                {
                    maxOutFrames = outFrames;
                }

                for (int f = 0; f < outFrames; f++)
                {
                    int dstIndex = f * channels + ch;
                    if (dstIndex < outputInterleaved.Length)
                    {
                        outputInterleaved[dstIndex] = outChannel[f];
                    }
                }
            }

            int finalFrames = maxOutFrames;
            if (finalFrames <= 0 || finalFrames > estimatedFrames)
            {
                finalFrames = Mathf.Min(originalFrames, estimatedFrames);
            }

            int finalLength = finalFrames * channels;
            if (finalLength > outputInterleaved.Length)
            {
                finalLength = outputInterleaved.Length;
            }

            float[] result = new float[finalLength];
            Array.Copy(outputInterleaved, result, finalLength);

            return result;
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
