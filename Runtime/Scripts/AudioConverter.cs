using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Flippit
{
    
    public class AudioConverter
    {
        
        public static AudioClip ConvertToWav(byte[] audioData, string outputPath, int sampleRate, int bitsPerSample, int channels)
        {
            int numSamples = audioData.Length / (bitsPerSample / 8);
            float[] audioSamples = new float[numSamples];

            // Convertissez les données audio brutes en float[]
            for (int i = 0; i < numSamples; i++)
            {
                audioSamples[i] = BitConverter.ToInt16(audioData, i * (bitsPerSample / 8)) / 32768f;
            }
            AudioClip clip = AudioClip.Create("MyAudioClip", numSamples, channels, sampleRate, false);

            using FileStream fileStream = new(outputPath, FileMode.Create);
            using BinaryWriter writer = new(fileStream);

            writer.Write(new char[] { 'R', 'I', 'F', 'F' });
            writer.Write(audioData.Length + 36);

            writer.Write(new char[] { 'W', 'A', 'V', 'E' });
            writer.Write(new char[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)channels);
            writer.Write(sampleRate);
            writer.Write((ushort)bitsPerSample);
            writer.Write((ushort)(bitsPerSample / 8 * channels));
            writer.Write((ushort)bitsPerSample);
            writer.Write(new char[] { 'd', 'a', 't', 'a' });
            writer.Write(audioData.Length);
            writer.Write(audioData);

            clip.SetData(audioSamples, 0);

            return clip;
        }
    }
}
