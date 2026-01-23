using System.IO;
using UnityEngine;

namespace AudioClipEditor
{
    public static class WavUtility
    {
        public static byte[] FromAudioClip(AudioClip clip)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                WriteWavHeader(writer, clip);
                WriteWavData(writer, clip);
                return stream.ToArray();
            }
        }

        private static void WriteWavHeader(BinaryWriter writer, AudioClip clip)
        {
            int sampleRate = clip.frequency;
            int channels = clip.channels;
            int bitsPerSample = 16;
            int dataSize = clip.samples * channels * (bitsPerSample / 8);

            // RIFF header
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + dataSize); // File size minus first 8 bytes
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            // Format chunk
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size (16 for PCM)
            writer.Write((short)1); // AudioFormat (1 for PCM)
            writer.Write((short)channels); // NumChannels
            writer.Write(sampleRate); // SampleRate
            writer.Write(sampleRate * channels * (bitsPerSample / 8)); // ByteRate
            writer.Write((short)(channels * (bitsPerSample / 8))); // BlockAlign
            writer.Write((short)bitsPerSample); // BitsPerSample

            // Data chunk
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(dataSize); // Subchunk2Size
        }

        private static void WriteWavData(BinaryWriter writer, AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            foreach (float sample in samples)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(intSample);
            }
        }

        public static void SaveToFile(AudioClip clip, string path)
        {
            byte[] wavData = FromAudioClip(clip);
            File.WriteAllBytes(path, wavData);
            Debug.Log("Saved WAV file to: " + path);
        }

        public static AudioClip ToAudioClip(byte[] data, string audioClipName)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read the WAV header
                reader.ReadBytes(4); // "RIFF"
                int fileSize = reader.ReadInt32();
                reader.ReadBytes(4); // "WAVE"

                // Read the format chunk
                reader.ReadBytes(4); // "fmt "
                int subchunk1Size = reader.ReadInt32();
                short audioFormat = reader.ReadInt16();
                short numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();

                // Read the data chunk
                reader.ReadBytes(4); // "data"
                int dataSize = reader.ReadInt32();

                float[] samples = new float[dataSize / (bitsPerSample / 8)];
                for (int i = 0; i < samples.Length; i++)
                {
                    short intSample = reader.ReadInt16();
                    samples[i] = intSample / (float)short.MaxValue;
                }

                AudioClip clip = AudioClip.Create(audioClipName, samples.Length / numChannels, numChannels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
        }
    }
}
