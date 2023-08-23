using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;


namespace Flippit
{
	public static class SaveWav
	{

		const int HEADER_SIZE = 44;

		public static byte[] Save(string filename, AudioClip clip)
		{
			if (!filename.ToLower().EndsWith(".wav"))
			{
				filename += ".wav";
			}

			var filepath = Path.Combine(Application.persistentDataPath, filename);

			// Make sure directory exists if user is saving to sub dir.
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using var memoryStream = CreateEmpty();
            ConvertAndWrite(memoryStream, clip);
            WriteHeader(memoryStream, clip);
            return memoryStream.GetBuffer();
        }

        [Obsolete]
        public static AudioClip TrimSilence(AudioClip clip, float min)
		{
			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

			return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);// to update
        }

        [Obsolete]
        public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
		{
			return TrimSilence(samples, min, channels, hz, false, false);// to update
        }

        [Obsolete]
        public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream)
		{
			int i;

			for (i = 0; i < samples.Count; i++)
			{
				if (Mathf.Abs(samples[i]) > min)
				{
					break;
				}
			}

			samples.RemoveRange(0, i);

			for (i = samples.Count - 1; i > 0; i--)
			{
				if (Mathf.Abs(samples[i]) > min)
				{
					break;
				}
			}

			samples.RemoveRange(i, samples.Count - i);

			var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);// to update

			clip.SetData(samples.ToArray(), 0);

			return clip;
		}

		static MemoryStream CreateEmpty()
		{
			var memoryStream = new MemoryStream();
			byte emptyByte = new();

			for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
			{
				memoryStream.WriteByte(emptyByte);
			}

			return memoryStream;
		}

		static void ConvertAndWrite(MemoryStream memoryStream, AudioClip clip)
		{

			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
			//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

			byte[] bytesData = new byte[samples.Length * 2];
			//bytesData array is twice the size of
			//dataSource array because a float converted in Int16 is 2 bytes.

			int rescaleFactor = 32767; //to convert float to Int16

			for (int i = 0; i < samples.Length; i++)
			{
				intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}

			memoryStream.Write(bytesData, 0, bytesData.Length);
		}

		static void WriteHeader(MemoryStream memoryStream, AudioClip clip)
		{

			var hz = clip.frequency;
			var channels = clip.channels;
			var samples = clip.samples;

			memoryStream.Seek(0, SeekOrigin.Begin);

			byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			memoryStream.Write(riff, 0, 4);

			byte[] chunkSize = BitConverter.GetBytes(memoryStream.Length - 8);
			memoryStream.Write(chunkSize, 0, 4);

			byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			memoryStream.Write(wave, 0, 4);

			byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			memoryStream.Write(fmt, 0, 4);

			byte[] subChunk1 = BitConverter.GetBytes(16);
			memoryStream.Write(subChunk1, 0, 4);

            ushort one = 1;

			byte[] audioFormat = BitConverter.GetBytes(one);
			memoryStream.Write(audioFormat, 0, 2);

			byte[] numChannels = BitConverter.GetBytes(channels);
			memoryStream.Write(numChannels, 0, 2);

			byte[] sampleRate = BitConverter.GetBytes(hz);
			memoryStream.Write(sampleRate, 0, 4);

			byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
			memoryStream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
			memoryStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

			ushort bps = 16;
			byte[] bitsPerSample = BitConverter.GetBytes(bps);
			memoryStream.Write(bitsPerSample, 0, 2);

			byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
			memoryStream.Write(datastring, 0, 4);

			byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
			memoryStream.Write(subChunk2, 0, 4);

		}
	}
}
