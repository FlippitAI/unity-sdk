using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using UnityEngine;
using UnityEngine.Networking;


namespace Flippit
{
    public class TTS : MonoBehaviour
    {
        public AudioSource audioSource;

        public async Task SpeechMe(string text,string VoiceId)
        {
            var credentials = new BasicAWSCredentials("AKIASXOKCHRGP2BBORMZ","8bsQ3czQ87tBmwL0JJgAGxIhoh7tnhR9r+Hcbph+");
            var client = new AmazonPollyClient(credentials, RegionEndpoint.EUWest1);

            var request = new SynthesizeSpeechRequest()
            {
                Text = text,
                Engine = Engine.Neural,
                VoiceId = VoiceId,
                OutputFormat = OutputFormat.Mp3
            };

            var response = await client.SynthesizeSpeechAsync(request);

            WriteIntoFile(response.AudioStream);

            // using var www = UnityWebRequestMultimedia.GetAudioClip($"file://{Application.persistentDataPath}/audio.mp3", AudioType.MPEG);
            // var op = www.SendWebRequest();
            // while (!op.isDone) await Task.Yield();

            // var clip = DownloadHandlerAudioClip.GetContent(www);
            // audioSource.clip = clip;
            // audioSource.Play();
            // await Task.Delay((int)(clip.length * 1000));
        }


        private void WriteIntoFile(Stream stream)
        {
            using var filestream = new FileStream(path: $"{Application.persistentDataPath}/audio.mp3", FileMode.Create);
            byte[] buffer = new byte[8 * 1024];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, offset: 0, count: buffer.Length)) > 0)
            {
                filestream.Write(buffer, offset: 0, count: bytesRead);
            }
        }

    }
}