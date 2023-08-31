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
        private ApiKeyManager apiKeyManager;
        private int currentVisemeIndex = 0;

        private List<Viseme> visemesList;
        private SkinnedMeshRenderer skinnedMeshRenderer;

        private bool isAudioPlaying = false;

        private static readonly Dictionary<string, string> phonemeToVisemeMapping = new Dictionary<string, string>()
        {
            {"sil", "viseme_sil"},
            {"p", "viseme_PP"},
            {"t", "viseme_DD"},
            {"S", "viseme_CH"},
            {"T", "viseme_TH"},
            {"f", "viseme_FF"},
            {"k", "viseme_kk"},
            {"r", "viseme_RR"},
            {"s", "viseme_SS"},
            {"@", "viseme_aa"},
            {"a", "viseme_aa"},
            {"e", "viseme_E"},
            {"E", "viseme_E"},
            {"i", "viseme_I"},
            {"o", "viseme_O"},
            {"O", "viseme_O"},
            {"u", "viseme_U"}
        };

        void Start()
        {
            //Get the mesh renderer to perform the viseme operations
            skinnedMeshRenderer = GetComponent<IACharacter>().Avatar.transform.Find("Renderer_Avatar").GetComponent<SkinnedMeshRenderer>();            
        }


        public async Task SpeechMe(string text, string VoiceId, List<Viseme> visemes)
        {
            visemesList = visemes;
            apiKeyManager = Resources.Load<ApiKeyManager>("Apikeys");
            var credentials = new BasicAWSCredentials(apiKeyManager.AWSKey,apiKeyManager.AWSSecret);
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

            using var www = UnityWebRequestMultimedia.GetAudioClip($"file://{Application.persistentDataPath}/audio.mp3", AudioType.MPEG);
            var op = www.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            var clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            isAudioPlaying = true;
            audioSource.Play();
            await Task.Delay((int)(clip.length * 1000));
            isAudioPlaying = false;
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

        void Update()
        {
            if (isAudioPlaying && currentVisemeIndex < visemesList.Count)
            {
                float currentTime = audioSource.time * 1000;

                // Check if the audio playback time is within the range of the current viseme data
                if (currentTime >= visemesList[currentVisemeIndex].start &&
                    currentTime <= visemesList[currentVisemeIndex].end)
                {
                    // Call the setViseme function with the appropriate values

                    setViseme(phonemeToVisemeMapping[visemesList[currentVisemeIndex].value], 1);
                }
                else
                {
                    // Call the setViseme function with the appropriate values to deactivate the viseme
                    setViseme(phonemeToVisemeMapping[visemesList[currentVisemeIndex].value], 0);
                }

                // Move to the next viseme data if the audio has passed its end time
                if (currentTime > visemesList[currentVisemeIndex].end)
                {
                    currentVisemeIndex++;
                }
            }
            else if (!isAudioPlaying && currentVisemeIndex != 0)
            {
                currentVisemeIndex = 0;
            }
        }

        // Your setViseme function implementation
        void setViseme(string viseme, float value)
        {
            int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(viseme);
            if (blendShapeIndex >= 0)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, value);
            }
            else
            {
                Debug.LogWarning($"Blend shape '{viseme}' does not exist in the mesh.");
            }
        }

    }
}






